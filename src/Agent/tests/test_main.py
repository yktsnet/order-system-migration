"""
FastAPI エンドポイント（main.py）ユニットテスト

カバー範囲:
  - GET /health : 純粋ロジック
  - POST /chat  : agent_graph.invoke をモックし、リクエスト検証とレスポンス整形を検証

実行:
  cd src/Agent && pip install -r requirements-dev.txt
  pytest tests/ -v
"""

import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from unittest.mock import patch

from fastapi.testclient import TestClient

from main import app

client = TestClient(app)


# ---------- GET /health ----------
class TestHealth:
    def test_health_returns_ok(self):
        response = client.get("/health")
        assert response.status_code == 200
        assert response.json() == {"status": "ok"}


# ---------- POST /chat ----------
class TestChat:
    def _final_state(self, **overrides):
        state = {
            "question": "受注件数は？",
            "sql": "SELECT COUNT(*) FROM orders",
            "result": [{"count": 3}],
            "answer": "受注は3件です。",
            "error": "",
            "retry_count": 0,
        }
        state.update(overrides)
        return state

    @patch("main.agent_graph")
    def test_chat_returns_answer_sql_and_data(self, mock_graph):
        mock_graph.invoke.return_value = self._final_state()

        response = client.post("/chat", json={"message": "受注件数は？"})

        assert response.status_code == 200
        body = response.json()
        assert body["answer"] == "受注は3件です。"
        assert body["sql"] == "SELECT COUNT(*) FROM orders"
        assert body["data"] == [{"count": 3}]

    @patch("main.agent_graph")
    def test_chat_passes_message_as_initial_question(self, mock_graph):
        mock_graph.invoke.return_value = self._final_state()

        client.post("/chat", json={"message": "先月の売上は？"})

        initial = mock_graph.invoke.call_args.args[0]
        assert initial["question"] == "先月の売上は？"
        assert initial["retry_count"] == 0
        assert initial["error"] == ""

    @patch("main.agent_graph")
    def test_chat_empty_sql_becomes_null(self, mock_graph):
        # out_of_scope 等で SQL が生成されなかった場合、sql は null で返る
        mock_graph.invoke.return_value = self._final_state(sql="", result=[])

        response = client.post("/chat", json={"message": "こんにちは"})

        assert response.status_code == 200
        body = response.json()
        assert body["sql"] is None
        assert body["data"] == []

    def test_chat_missing_message_returns_422(self):
        response = client.post("/chat", json={})
        assert response.status_code == 422
