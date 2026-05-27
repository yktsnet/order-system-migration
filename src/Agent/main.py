from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from agent import AgentState, agent_graph

app = FastAPI(title="Agent API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["POST", "GET"],
    allow_headers=["*"],
)


class ChatRequest(BaseModel):
    message: str


class ChatResponse(BaseModel):
    answer: str
    sql: str | None = None
    data: list = []


@app.post("/chat", response_model=ChatResponse)
async def chat(req: ChatRequest):
    initial: AgentState = {
        "question": req.message,
        "sql": "",
        "result": [],
        "answer": "",
        "error": "",
        "retry_count": 0,
    }
    final = agent_graph.invoke(initial)
    return ChatResponse(
        answer=final["answer"],
        sql=final.get("sql") or None,
        data=final.get("result", []),
    )


@app.get("/health")
def health():
    return {"status": "ok"}
