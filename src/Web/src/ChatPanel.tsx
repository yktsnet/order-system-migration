import { useState, useRef, useEffect } from "react"
import { Send, Loader2, ChevronDown, ChevronUp, Bot, User } from "lucide-react"

interface ChatMessage {
  role: "user" | "assistant"
  content: string
  sql?: string
  data?: Record<string, unknown>[]
}

const SUGGESTED = [
  "先月の得意先ランキングトップ3は？",
  "カテゴリ別の売上合計を教えて",
  "在庫が50以下の商品は？",
  "今月の受注件数は？",
]

const AGENT_BASE = import.meta.env.VITE_AGENT_BASE ?? ""

export function ChatPanel() {
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      role: "assistant",
      content:
        "受注データについて自然言語で質問できます。\n例: 「先月のカテゴリ別売上は？」「得意先ランキングトップ3は？」",
    },
  ])
  const [input, setInput] = useState("")
  const [loading, setLoading] = useState(false)
  const [expandedSql, setExpandedSql] = useState<number | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" })
  }, [messages])

  const sendMessage = async (text?: string) => {
    const msg = (text ?? input).trim()
    if (!msg || loading) return

    setInput("")
    setMessages((prev) => [...prev, { role: "user", content: msg }])
    setLoading(true)

    try {
      const res = await fetch(`${AGENT_BASE}/chat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: msg }),
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const data = await res.json()
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          content: data.answer,
          sql: data.sql,
          data: data.data,
        },
      ])
    } catch {
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          content: "エラーが発生しました。Agentサービスに接続できませんでした。",
        },
      ])
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="bg-white border rounded-lg shadow-sm flex flex-col animate-in fade-in duration-300 h-[70vh] min-h-[400px] md:h-[calc(100vh-200px)]">

      {/* Header */}
      <div className="px-6 py-4 border-b bg-slate-50/50 flex items-center gap-2 shrink-0">
        <Bot className="w-4 h-4 text-sky-600" />
        <h2 className="text-sm font-bold text-slate-600">データ分析 (AI Agent)</h2>
        <span className="ml-auto text-[10px] bg-sky-100 text-sky-700 px-2 py-0.5 rounded-full font-bold">
          LangGraph + Gemini
        </span>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4 min-h-0">
        {messages.map((msg, i) => (
          <div key={i} className={`flex gap-3 ${msg.role === "user" ? "flex-row-reverse" : ""}`}>
            <div
              className={`w-7 h-7 rounded-full flex items-center justify-center shrink-0 ${
                msg.role === "user" ? "bg-sky-600" : "bg-slate-200"
              }`}
            >
              {msg.role === "user" ? (
                <User className="w-3.5 h-3.5 text-white" />
              ) : (
                <Bot className="w-3.5 h-3.5 text-slate-600" />
              )}
            </div>
            <div
              className={`max-w-[75%] space-y-2 flex flex-col ${
                msg.role === "user" ? "items-end" : "items-start"
              }`}
            >
              <div
                className={`px-4 py-3 rounded-lg text-sm leading-relaxed whitespace-pre-wrap ${
                  msg.role === "user"
                    ? "bg-sky-600 text-white rounded-tr-none"
                    : "bg-slate-100 text-slate-800 rounded-tl-none"
                }`}
              >
                {msg.content}
              </div>

              {/* SQL折りたたみ */}
              {msg.sql && (
                <div className="w-full">
                  <button
                    onClick={() => setExpandedSql(expandedSql === i ? null : i)}
                    className="flex items-center gap-1 text-[10px] text-slate-400 hover:text-slate-600 transition-colors"
                  >
                    {expandedSql === i ? (
                      <ChevronUp className="w-3 h-3" />
                    ) : (
                      <ChevronDown className="w-3 h-3" />
                    )}
                    生成SQL {expandedSql === i ? "非表示" : "表示"}
                  </button>
                  {expandedSql === i && (
                    <pre className="mt-1 p-3 bg-slate-800 text-green-400 text-[11px] rounded overflow-x-auto font-mono leading-relaxed">
                      {msg.sql}
                    </pre>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}

        {/* ローディング */}
        {loading && (
          <div className="flex gap-3">
            <div className="w-7 h-7 rounded-full bg-slate-200 flex items-center justify-center">
              <Bot className="w-3.5 h-3.5 text-slate-600" />
            </div>
            <div className="px-4 py-3 rounded-lg bg-slate-100 rounded-tl-none">
              <Loader2 className="w-4 h-4 animate-spin text-slate-400" />
            </div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <div className="px-6 py-4 border-t bg-white shrink-0">
        {/* サジェスト */}
        <div className="mb-2 flex gap-2 overflow-x-auto whitespace-nowrap scrollbar-none pb-1">
          {SUGGESTED.map((q) => (
            <button
              key={q}
              onClick={() => sendMessage(q)}
              disabled={loading}
              className="text-[10px] px-2 py-1 bg-slate-100 hover:bg-slate-200 disabled:opacity-50 text-slate-600 rounded transition-colors flex-shrink-0"
            >
              {q}
            </button>
          ))}
        </div>
        <div className="flex gap-3">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && !e.shiftKey && sendMessage()}
            placeholder="例: 先月の得意先ランキングトップ3は？"
            disabled={loading}
            className="flex-1 h-10 px-4 border border-slate-300 rounded-lg text-sm focus:border-sky-600 outline-none disabled:opacity-50"
          />
          <button
            onClick={() => sendMessage()}
            disabled={loading || !input.trim()}
            className="h-10 w-10 bg-sky-600 hover:bg-sky-500 disabled:opacity-50 text-white rounded-lg flex items-center justify-center transition-colors"
          >
            <Send className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  )
}
