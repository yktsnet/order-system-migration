import os
import re
from typing import TypedDict

from langchain_core.messages import HumanMessage
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.graph import END, START, StateGraph

import db
from schema_prompt import SCHEMA


# ---------- State ----------
class AgentState(TypedDict):
    question: str
    sql: str
    result: list
    answer: str
    error: str
    retry_count: int


# ---------- LLM ----------
def _llm() -> ChatGoogleGenerativeAI:
    return ChatGoogleGenerativeAI(
        model="gemini-2.5-flash",
        google_api_key=os.environ["GEMINI_API_KEY"],
        temperature=0,
    )


# ---------- Nodes ----------
def classify_intent(state: AgentState) -> AgentState:
    prompt = (
        f"次の質問がデータベースへの問い合わせ（受注データの検索・集計・ランキング等）を"
        f"必要とするかどうか判定してください。\n"
        f"質問: {state['question']}\n"
        f"「YES」か「NO」のみ回答してください。"
    )
    resp = _llm().invoke([HumanMessage(content=prompt)])
    if "NO" in resp.content.upper():
        return {
            **state,
            "answer": "受注データに関する質問をどうぞ。例:「先月の売上は？」「得意先ランキングは？」",
            "error": "out_of_scope",
        }
    return state


def generate_sql(state: AgentState) -> AgentState:
    retry_hint = ""
    if state.get("error") and state.get("retry_count", 0) > 0:
        retry_hint = f"\n\n前回のSQLでエラーが発生しました:\n{state['error']}\n上記を修正したSQLを生成してください。"

    prompt = (
        f"以下のスキーマを参照してSQLを生成してください。\n\n"
        f"{SCHEMA}\n"
        f"質問: {state['question']}{retry_hint}\n\n"
        f"SELECT文のみを出力してください。説明不要。コードブロック(```)も不要。"
    )
    resp = _llm().invoke([HumanMessage(content=prompt)])
    raw = resp.content.strip()
    # コードブロック除去
    sql = re.sub(r"```(?:sql)?", "", raw).replace("```", "").strip()
    return {**state, "sql": sql, "error": ""}


def validate_sql(state: AgentState) -> AgentState:
    sql_upper = state["sql"].upper()
    forbidden = ["INSERT", "UPDATE", "DELETE", "DROP", "TRUNCATE", "ALTER", "CREATE"]
    for word in forbidden:
        if re.search(r"\b" + word + r"\b", sql_upper):
            return {**state, "error": f"危険な操作が含まれています: {word}"}
    if not sql_upper.lstrip().startswith("SELECT"):
        return {**state, "error": "SELECT文以外は実行できません"}
    return state


def execute_sql(state: AgentState) -> AgentState:
    try:
        result = db.execute_query(state["sql"])
        return {**state, "result": result, "error": ""}
    except Exception as e:
        return {
            **state,
            "error": str(e),
            "retry_count": state.get("retry_count", 0) + 1,
        }


def format_response(state: AgentState) -> AgentState:
    if not state["result"]:
        db.log_agent(state["question"], state["sql"], True, state.get("retry_count", 0), None)
        return {**state, "answer": "該当するデータが見つかりませんでした。"}

    prompt = (
        f"以下のデータを日本語で分かりやすく説明してください。\n"
        f"質問: {state['question']}\n"
        f"データ: {state['result'][:50]}\n"
        f"簡潔に、重要な情報を中心に答えてください。"
    )
    resp = _llm().invoke([HumanMessage(content=prompt)])
    db.log_agent(state["question"], state["sql"], True, state.get("retry_count", 0), None)
    return {**state, "answer": resp.content}


def handle_error(state: AgentState) -> AgentState:
    db.log_agent(
        state["question"],
        state.get("sql") or None,
        False,
        state.get("retry_count", 0),
        state["error"],
    )
    return {**state, "answer": f"処理中にエラーが発生しました: {state['error']}"}


# ---------- Routing ----------
def route_classify(state: AgentState) -> str:
    return "end" if state.get("error") == "out_of_scope" else "generate_sql"


def route_validate(state: AgentState) -> str:
    return "handle_error" if state.get("error") else "execute_sql"


def route_execute(state: AgentState) -> str:
    if not state.get("error"):
        return "format_response"
    if state.get("retry_count", 0) < 2:
        return "generate_sql"  # retry
    return "handle_error"


# ---------- Graph ----------
def _build_graph():
    g = StateGraph(AgentState)

    g.add_node("classify_intent", classify_intent)
    g.add_node("generate_sql", generate_sql)
    g.add_node("validate_sql", validate_sql)
    g.add_node("execute_sql", execute_sql)
    g.add_node("format_response", format_response)
    g.add_node("handle_error", handle_error)

    g.add_edge(START, "classify_intent")
    g.add_conditional_edges(
        "classify_intent",
        route_classify,
        {"end": END, "generate_sql": "generate_sql"},
    )
    g.add_edge("generate_sql", "validate_sql")
    g.add_conditional_edges(
        "validate_sql",
        route_validate,
        {"handle_error": "handle_error", "execute_sql": "execute_sql"},
    )
    g.add_conditional_edges(
        "execute_sql",
        route_execute,
        {
            "format_response": "format_response",
            "generate_sql": "generate_sql",
            "handle_error": "handle_error",
        },
    )
    g.add_edge("format_response", END)
    g.add_edge("handle_error", END)

    return g.compile()


agent_graph = _build_graph()
