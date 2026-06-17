import { useState, useEffect } from "react"
import { PlusCircle, History, Sparkles } from "lucide-react"
import { Header } from "./components/Header"
import { Footer } from "./components/Footer"
import { OrderForm } from "./components/OrderForm"
import { OrderHistory } from "./components/OrderHistory"
import { ChatPanel } from "./components/ChatPanel"
import type { Category, OrderHistory as OrderHistoryType } from "./types"

function App() {
  const API_BASE = "";

  const [activeTab, setActiveTab] = useState<"form" | "history" | "chat">("form");
  const [categories, setCategories] = useState<Category[]>([]);
  const [history, setHistory] = useState<OrderHistoryType[]>([]);
  const [loading, setLoading] = useState(false);

  // カテゴリ一覧の取得
  useEffect(() => {
    fetch(`${API_BASE}/categories`)
      .then(res => res.json())
      .then(data => {
        setCategories(data);
      });
  }, []);

  // フィルター用パラメータからクエリ文字列を構築する関数
  const buildFilterQuery = (filters: {
    customerName?: string;
    itemName?: string;
    categoryId?: number;
    from?: string;
    to?: string;
  }) => {
    const params = new URLSearchParams();
    if (filters.customerName) params.set("customerName", filters.customerName);
    if (filters.itemName) params.set("itemName", filters.itemName);
    if (filters.categoryId && filters.categoryId > 0) params.set("categoryId", String(filters.categoryId));
    if (filters.from) params.set("from", filters.from);
    if (filters.to) params.set("to", filters.to);
    return params.toString();
  };

  // 履歴取得処理
  const fetchHistory = async (filters: {
    customerName?: string;
    itemName?: string;
    categoryId?: number;
    from?: string;
    to?: string;
  } = {}) => {
    setLoading(true);
    try {
      const query = buildFilterQuery(filters);
      const url = `${API_BASE}/orders${query ? `?${query}` : ""}`;
      const res = await fetch(url, { cache: "no-store" });
      if (res.ok) {
        const data = await res.json();
        setHistory(data);
      }
    } catch (err) {
      console.error("履歴取得失敗:", err);
    } finally {
      setLoading(false);
    }
  };

  // CSVダウンロード
  const handleExportCsv = (filters: {
    customerName?: string;
    itemName?: string;
    categoryId?: number;
    from?: string;
    to?: string;
  }) => {
    const query = buildFilterQuery(filters);
    window.location.href = `${API_BASE}/orders/export${query ? `?${query}` : ""}`;
  };

  // 初回表示時および履歴タブ表示時に履歴を取得
  useEffect(() => {
    if (activeTab === "history") {
      fetchHistory();
    }
  }, [activeTab]);

  // 新規受注登録
  const handleSave = async (order: {
    orderNo: string;
    customerName: string;
    categoryId: number;
    itemName: string;
    price: number;
    qty: number;
  }): Promise<boolean> => {
    setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/orders`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(order),
      });
      if (res.ok) {
        alert("正常に登録されました（在庫が減算されました）");
        return true;
      } else {
        alert("登録に失敗しました");
        return false;
      }
    } catch {
      alert("通信エラーが発生しました");
      return false;
    } finally {
      setLoading(false);
    }
  };

  // 受注削除
  const handleDelete = async (targetOrderNo: string) => {
    if (!confirm(`受注番号: ${targetOrderNo} を取り消しますか？\n削除すると在庫が自動的に復元されます。`)) return;
    try {
      const res = await fetch(`${API_BASE}/orders/${targetOrderNo}`, { method: "DELETE" });
      if (res.ok) {
        fetchHistory();
      } else {
        alert("削除に失敗しました");
      }
    } catch {
      alert("通信エラーが発生しました");
    }
  };

  const tabClass = (tab: typeof activeTab) =>
    `px-4 py-2 text-xs font-bold flex items-center gap-2 rounded-t-md transition-all flex-shrink-0 ${
      activeTab === tab
        ? "bg-slate-50 border-t border-l border-r text-sky-600"
        : "text-slate-400 hover:text-slate-600"
    }`;

  return (
    <div className="flex flex-col min-h-screen w-full bg-slate-50 font-sans text-slate-900">
      <Header />

      {/* タブ */}
      <div className="bg-white border-b px-4 flex gap-1 pt-2 overflow-x-auto whitespace-nowrap scrollbar-none">
        <button onClick={() => setActiveTab("form")} className={tabClass("form")}>
          <PlusCircle className="w-3.5 h-3.5" /> 受注登録
        </button>
        <button onClick={() => setActiveTab("history")} className={tabClass("history")}>
          <History className="w-3.5 h-3.5" /> 注文履歴・取消
        </button>
        <button onClick={() => setActiveTab("chat")} className={tabClass("chat")}>
          <Sparkles className="w-3.5 h-3.5" /> データ分析
        </button>
      </div>

      <main className="flex-1 p-4 sm:p-6 overflow-auto">
        <div className="max-w-6xl mx-auto">
          {/* 受注登録タブ */}
          <div className={activeTab === "form" ? "block" : "hidden"}>
            <OrderForm categories={categories} loading={loading} onSave={handleSave} />
          </div>

          {/* 注文履歴タブ */}
          <div className={activeTab === "history" ? "block" : "hidden"}>
            <OrderHistory
              categories={categories}
              history={history}
              loading={loading}
              onFetch={fetchHistory}
              onDelete={handleDelete}
              onExportCsv={handleExportCsv}
            />
          </div>

          {/* データ分析タブ */}
          <div className={activeTab === "chat" ? "block" : "hidden"}>
            <ChatPanel />
          </div>
        </div>
      </main>

      <Footer />
    </div>
  )
}

export default App
