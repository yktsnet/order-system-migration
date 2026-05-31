"""
Agent ユニットテスト

カバー範囲:
  - validate_sql       : 純粋ロジック（LLM不要）
  - route_classify     : 純粋ロジック
  - route_validate     : 純粋ロジック
  - route_execute      : 純粋ロジック
  - classify_intent    : LLMモック
  - generate_sql       : LLMモック
  - execute_sql        : db.execute_queryモック
  - format_response    : LLMモック + db.log_agentモック

実行:
  cd src/Agent && pip install -r requirements-dev.txt
  pytest tests/ -v
"""

import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import pytest
from unittest.mock import MagicMock, patch

from agent import (
    AgentState,
    classify_intent,
    generate_sql,
    validate_sql,
    execute_sql,
    format_response,
    route_classify,
    route_validate,
    route_execute,
)


# ---------- helpers ----------


def base_state(**kwargs) -> AgentState:
    state: AgentState = {
        "question": "先月の受注件数は？",
        "sql": "",
        "result": [],
        "answer": "",
        "error": "",
        "retry_count": 0,
    }
    state.update(kwargs)
    return state


def make_llm_mock(text: str):
    """LLMインスタンスのモック。invoke() が text を返す。"""
    mock_llm = MagicMock()
    mock_resp = MagicMock()
    mock_resp.content = text
    mock_llm.invoke.return_value = mock_resp
    return mock_llm


# ---------- validate_sql ----------


class TestValidateSql:
    def test_valid_select(self):
        result = validate_sql(base_state(sql="SELECT * FROM Orders"))
        assert result["error"] == ""

    def test_valid_select_lowercase(self):
        result = validate_sql(base_state(sql="select orderno from orders"))
        assert result["error"] == ""

    def test_insert_rejected(self):
        result = validate_sql(base_state(sql="INSERT INTO Orders VALUES (1)"))
        assert "INSERT" in result["error"]

    def test_update_rejected(self):
        result = validate_sql(base_state(sql="UPDATE Orders SET qty = 1"))
        assert "UPDATE" in result["error"]

    def test_delete_rejected(self):
        result = validate_sql(base_state(sql="DELETE FROM Orders"))
        assert "DELETE" in result["error"]

    def test_drop_rejected(self):
        result = validate_sql(base_state(sql="DROP TABLE Orders"))
        assert "DROP" in result["error"]

    def test_truncate_rejected(self):
        result = validate_sql(base_state(sql="TRUNCATE TABLE Orders"))
        assert "TRUNCATE" in result["error"]

    def test_non_select_start_rejected(self):
        # WITH句始まりでDELETEを含むケース
        result = validate_sql(
            base_state(sql="WITH cte AS (SELECT 1) DELETE FROM Orders")
        )
        assert result["error"] != ""

    def test_select_with_subquery_allowed(self):
        sql = "SELECT * FROM Orders WHERE orderNo IN (SELECT orderNo FROM Orders WHERE qty > 10)"
        result = validate_sql(base_state(sql=sql))
        assert result["error"] == ""


# ---------- routing ----------


class TestRouteClassify:
    def test_out_of_scope_goes_to_end(self):
        assert route_classify(base_state(error="out_of_scope")) == "end"

    def test_empty_error_goes_to_generate_sql(self):
        assert route_classify(base_state(error="")) == "generate_sql"

    def test_other_error_goes_to_generate_sql(self):
        # out_of_scope 以外のエラーは generate_sql へ（classify 段階では別エラーは起きないが念のため）
        assert route_classify(base_state(error="some_other_error")) == "generate_sql"


class TestRouteValidate:
    def test_has_error_goes_to_handle_error(self):
        assert route_validate(base_state(error="危険な操作: DELETE")) == "handle_error"

    def test_no_error_goes_to_execute_sql(self):
        assert route_validate(base_state(error="")) == "execute_sql"


class TestRouteExecute:
    def test_success_goes_to_format_response(self):
        assert (
            route_execute(base_state(error="", result=[{"count": 42}]))
            == "format_response"
        )

    def test_first_retry_goes_to_generate_sql(self):
        assert (
            route_execute(base_state(error="column not found", retry_count=1))
            == "generate_sql"
        )

    def test_second_retry_goes_to_generate_sql(self):
        # retry_count=1 < 2 → まだリトライ可
        assert (
            route_execute(base_state(error="relation not found", retry_count=1))
            == "generate_sql"
        )

    def test_retry_limit_goes_to_handle_error(self):
        # retry_count=2 → 上限到達
        assert (
            route_execute(base_state(error="column not found", retry_count=2))
            == "handle_error"
        )

    def test_no_error_empty_result_goes_to_format_response(self):
        # result=[] でも error="" なら format_response（空応答はそこで処理）
        assert route_execute(base_state(error="", result=[])) == "format_response"


# ---------- classify_intent ----------


class TestClassifyIntent:
    def test_yes_response_passes_through(self):
        with patch("agent._llm", return_value=make_llm_mock("YES")):
            result = classify_intent(base_state(question="先月の受注件数は？"))
        assert result.get("error") != "out_of_scope"

    def test_no_response_sets_out_of_scope(self):
        with patch("agent._llm", return_value=make_llm_mock("NO")):
            result = classify_intent(base_state(question="今日の天気は？"))
        assert result["error"] == "out_of_scope"
        assert result["answer"] != ""

    def test_out_of_scope_answer_contains_example(self):
        with patch("agent._llm", return_value=make_llm_mock("NO")):
            result = classify_intent(base_state(question="おすすめのランチは？"))
        # ユーザー向けガイダンスが含まれること
        assert "受注" in result["answer"]


# ---------- generate_sql ----------


class TestGenerateSql:
    def test_sql_is_generated(self):
        sql_text = "SELECT COUNT(*) FROM Orders WHERE orderdate >= '2026-04-01'"
        with patch("agent._llm", return_value=make_llm_mock(sql_text)):
            result = generate_sql(base_state())
        assert "SELECT" in result["sql"].upper()
        assert result["error"] == ""

    def test_code_block_backticks_stripped(self):
        with patch(
            "agent._llm",
            return_value=make_llm_mock("```sql\nSELECT * FROM Orders\n```"),
        ):
            result = generate_sql(base_state())
        assert "```" not in result["sql"]

    def test_error_cleared_on_new_attempt(self):
        """generate_sql は error をリセットする"""
        with patch("agent._llm", return_value=make_llm_mock("SELECT 1")):
            result = generate_sql(base_state(error="前回のエラー", retry_count=1))
        assert result["error"] == ""

    def test_retry_hint_included_in_prompt(self):
        """リトライ時、前回エラーがプロンプトに含まれること"""
        mock_llm = make_llm_mock("SELECT * FROM Orders")
        with patch("agent._llm", return_value=mock_llm):
            generate_sql(
                base_state(error='column "hoge" does not exist', retry_count=1)
            )
        messages = mock_llm.invoke.call_args[0][0]
        prompt_text = messages[0].content
        assert 'column "hoge" does not exist' in prompt_text

    def test_no_retry_hint_on_first_attempt(self):
        """初回（retry_count=0）はリトライヒントなし"""
        mock_llm = make_llm_mock("SELECT 1")
        with patch("agent._llm", return_value=mock_llm):
            generate_sql(base_state(retry_count=0))
        messages = mock_llm.invoke.call_args[0][0]
        prompt_text = messages[0].content
        assert "前回のSQLでエラーが発生しました" not in prompt_text


# ---------- execute_sql ----------


class TestExecuteSql:
    def test_success_returns_result(self):
        mock_result = [{"count": 42}]
        with patch("agent.db.execute_query", return_value=mock_result):
            result = execute_sql(base_state(sql="SELECT COUNT(*) FROM Orders"))
        assert result["result"] == mock_result
        assert result["error"] == ""

    def test_db_error_sets_error_message(self):
        with patch(
            "agent.db.execute_query",
            side_effect=Exception('relation "orders" does not exist'),
        ):
            result = execute_sql(base_state(sql="SELECT * FROM orders", retry_count=0))
        assert "relation" in result["error"]

    def test_db_error_increments_retry_count(self):
        with patch("agent.db.execute_query", side_effect=Exception("syntax error")):
            result = execute_sql(base_state(retry_count=0))
        assert result["retry_count"] == 1

    def test_db_error_accumulates_retry_count(self):
        with patch("agent.db.execute_query", side_effect=Exception("syntax error")):
            result = execute_sql(base_state(retry_count=1))
        assert result["retry_count"] == 2


# ---------- format_response ----------


class TestFormatResponse:
    def test_formats_non_empty_result(self):
        answer_text = "先月の受注件数は42件です。"
        with patch("agent._llm", return_value=make_llm_mock(answer_text)), patch(
            "agent.db.log_agent"
        ):
            result = format_response(
                base_state(
                    sql="SELECT COUNT(*) FROM Orders",
                    result=[{"count": 42}],
                )
            )
        assert result["answer"] == answer_text

    def test_empty_result_returns_not_found(self):
        with patch("agent.db.log_agent"):
            result = format_response(base_state(sql="SELECT * FROM Orders", result=[]))
        assert "見つかりませんでした" in result["answer"]

    def test_empty_result_does_not_call_llm(self):
        """空結果時はLLMを呼ばない"""
        with patch("agent._llm") as mock_llm_factory, patch("agent.db.log_agent"):
            format_response(base_state(result=[]))
        mock_llm_factory.assert_not_called()

    def test_log_agent_called_on_success(self):
        with patch("agent._llm", return_value=make_llm_mock("42件です。")), patch(
            "agent.db.log_agent"
        ) as mock_log:
            format_response(
                base_state(
                    sql="SELECT COUNT(*) FROM Orders",
                    result=[{"count": 42}],
                )
            )
        mock_log.assert_called_once()

    def test_log_agent_called_on_empty_result(self):
        with patch("agent.db.log_agent") as mock_log:
            format_response(base_state(result=[]))
        mock_log.assert_called_once()
