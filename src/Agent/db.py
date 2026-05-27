import os
import decimal
import datetime
import psycopg2
import psycopg2.extras


def get_connection():
    return psycopg2.connect(os.environ["DATABASE_URL"])


def _serialize(row: dict) -> dict:
    out = {}
    for k, v in row.items():
        if isinstance(v, decimal.Decimal):
            out[k] = float(v)
        elif isinstance(v, (datetime.datetime, datetime.date)):
            out[k] = v.isoformat()
        else:
            out[k] = v
    return out


def execute_query(sql: str) -> list[dict]:
    conn = get_connection()
    try:
        with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
            cur.execute(sql)
            rows = cur.fetchall()
            return [_serialize(dict(row)) for row in rows]
    finally:
        conn.close()


def log_agent(
    question: str,
    sql: str | None,
    success: bool,
    retry_count: int,
    error: str | None,
) -> None:
    try:
        conn = get_connection()
        with conn.cursor() as cur:
            cur.execute(
                """
                INSERT INTO agentlog (question, generated_sql, success, retry_count, error_message)
                VALUES (%s, %s, %s, %s, %s)
                """,
                (question, sql, success, retry_count, error),
            )
        conn.commit()
        conn.close()
    except Exception as e:
        print(f"[WARN] AgentLog 書き込み失敗: {e}")
