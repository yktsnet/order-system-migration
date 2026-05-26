import { useState, useEffect } from "react"
import { 
  Package2, Database, Cloud, Save, Trash2, 
  History, PlusCircle, RefreshCw
} from "lucide-react"

interface Category { id: number; name: string; }
interface OrderHistory {
  orderNo: string;
  orderDate: string;
  customerName: string;
  itemName: string;
  price: number;
  qty: number;
  totalAmount: number;
  categoryName: string;
}

function App() {
  const API_BASE = "";

  const [activeTab, setActiveTab] = useState<'form' | 'history'>('form');
  const [categories, setCategories] = useState<Category[]>([]);
  const [history, setHistory] = useState<OrderHistory[]>([]);
  const [loading, setLoading] = useState(false);

  const [orderNo, setOrderNo] = useState(`ORD-${new Date().getTime().toString().slice(-6)}`);
  const [customer, setCustomer] = useState("");
  const [selectedCat, setSelectedCat] = useState<number>(1);
  const [itemName, setItemName] = useState("");
  const [price, setPrice] = useState(0);
  const [qty, setQty] = useState(1);

  const subTotal = price * qty;
  const tax = Math.floor(subTotal * 0.1);
  const total = subTotal + tax;

  useEffect(() => {
    fetch(`${API_BASE}/categories`)
      .then(res => res.json())
      .then(data => {
        setCategories(data);
        if(data.length > 0) setSelectedCat(data[0].id);
      });
  }, []);

  const fetchHistory = async () => {
    setLoading(true);
    try {
      // ★ 修正箇所：ブラウザのキャッシュを無視し、必ずDBの最新状態を取得する
      const res = await fetch(`${API_BASE}/orders`, { cache: 'no-store' });
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

  useEffect(() => {
    if (activeTab === 'history') fetchHistory();
  }, [activeTab]);

  const handleSave = async () => {
    if (!customer || !itemName || price <= 0) {
      alert("得意先名、商品名、正しい単価を入力してください。");
      return;
    }
    setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/orders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderNo, customerName: customer, categoryId: selectedCat, itemName, price, qty })
      });
      if (res.ok) {
        alert("正常に登録されました（在庫が減算されました）");
        setCustomer(""); setItemName(""); setPrice(0); setQty(1);
        setOrderNo(`ORD-${new Date().getTime().toString().slice(-6)}`);
      }
    } catch (err) {
      alert("通信エラーが発生しました");
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (targetOrderNo: string) => {
    if (!confirm(`受注番号: ${targetOrderNo} を取り消しますか？\n削除すると在庫が自動的に復元されます。`)) return;
    try {
      const res = await fetch(`${API_BASE}/orders/${targetOrderNo}`, { method: 'DELETE' });
      if (res.ok) {
        // 削除成功時、キャッシュを無視するfetchHistoryが呼ばれ、画面から完全に消える
        fetchHistory();
      } else {
        alert("削除に失敗しました");
      }
    } catch (err) {
      alert("通信エラーが発生しました");
    }
  };

  return (
    <div className="flex flex-col h-screen w-full bg-slate-50 font-sans text-slate-900">
      <header className="flex items-center justify-between h-10 px-4 border-b bg-white shrink-0 shadow-sm">
        <div className="flex items-center gap-2">
          <Package2 className="w-4 h-4 text-sky-600" />
          <span className="font-bold text-slate-700 uppercase tracking-tight">受注管理システム</span>
        </div>
        <div className="flex items-center gap-4 text-[11px] text-slate-500">
          <div className="flex items-center gap-1"><Database className="w-3 h-3 text-green-500"/> DB: Online</div>
          <div className="flex items-center gap-1"><Cloud className="w-3 h-3 text-green-500"/> S3: Connected</div>
        </div>
      </header>

      <div className="bg-white border-b px-4 flex gap-1 pt-2">
        <button 
          onClick={() => setActiveTab('form')}
          className={`px-4 py-2 text-xs font-bold flex items-center gap-2 rounded-t-md transition-all ${activeTab === 'form' ? 'bg-slate-50 border-t border-l border-r text-sky-600' : 'text-slate-400 hover:text-slate-600'}`}
        >
          <PlusCircle className="w-3.5 h-3.5" /> 受注登録
        </button>
        <button 
          onClick={() => setActiveTab('history')}
          className={`px-4 py-2 text-xs font-bold flex items-center gap-2 rounded-t-md transition-all ${activeTab === 'history' ? 'bg-slate-50 border-t border-l border-r text-sky-600' : 'text-slate-400 hover:text-slate-600'}`}
        >
          <History className="w-3.5 h-3.5" /> 注文履歴・取消
        </button>
      </div>

      <main className="flex-1 p-6 overflow-auto">
        <div className="max-w-6xl mx-auto">
          {activeTab === 'form' ? (
            <div className="grid grid-cols-12 gap-6 animate-in fade-in duration-300">
              <div className="col-span-8 bg-white border rounded-lg shadow-sm p-6 space-y-6">
                <div className="flex items-center justify-between border-b pb-4 mb-2">
                  <h2 className="text-sm font-bold text-slate-600">新規受注入力</h2>
                  <span className="text-[10px] bg-sky-100 text-sky-700 px-2 py-0.5 rounded-full font-bold">READY</span>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="col-span-2">
                    <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">受注番号</label>
                    <input type="text" value={orderNo} readOnly className="w-full h-10 px-3 border border-slate-200 rounded bg-slate-50 font-mono text-sm" />
                  </div>
                  <div>
                    <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">得意先名</label>
                    <input type="text" value={customer} onChange={e => setCustomer(e.target.value)} className="w-full h-10 px-3 border border-slate-300 rounded text-sm focus:border-sky-600 outline-none" placeholder="例：株式会社サンプル" />
                  </div>
                  <div>
                    <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">カテゴリ</label>
                    <select value={selectedCat} onChange={e => setSelectedCat(Number(e.target.value))} className="w-full h-10 px-3 border border-slate-300 rounded text-sm bg-white focus:border-sky-600 outline-none">
                      {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                    </select>
                  </div>
                  <div className="col-span-2">
                    <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">商品名称</label>
                    <input type="text" value={itemName} onChange={e => setItemName(e.target.value)} className="w-full h-10 px-3 border border-slate-300 rounded text-sm focus:border-sky-600 outline-none" />
                  </div>
                  <div>
                    <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">単価 (円)</label>
                    <input type="number" value={price} onChange={e => setPrice(Number(e.target.value))} className="w-full h-10 px-3 border border-slate-300 rounded font-bold text-sm" />
                  </div>
                  <div>
                    <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">数量</label>
                    <input type="number" value={qty} onChange={e => setQty(Number(e.target.value))} className="w-full h-10 px-3 border border-slate-300 rounded font-bold text-sm" />
                  </div>
                </div>
              </div>
              <div className="col-span-4 space-y-4">
                <div className="bg-slate-800 rounded-lg p-6 text-white shadow-lg space-y-4">
                  <div className="text-right border-b border-slate-700 pb-2">
                    <div className="text-[10px] text-slate-400 font-bold uppercase">SUBTOTAL</div>
                    <div className="text-xl font-mono">{subTotal.toLocaleString()} <span className="text-xs">JPY</span></div>
                  </div>
                  <div className="text-right border-b border-slate-700 pb-2">
                    <div className="text-[10px] text-slate-400 font-bold uppercase">TAX (10%)</div>
                    <div className="text-xl font-mono">{tax.toLocaleString()} <span className="text-xs">JPY</span></div>
                  </div>
                  <div className="text-right pt-2">
                    <div className="text-[10px] text-sky-400 font-bold uppercase">TOTAL</div>
                    <div className={`text-4xl font-mono font-bold ${total > 1000000 ? 'text-red-400' : 'text-sky-400'}`}>
                      {total.toLocaleString()}
                    </div>
                  </div>
                </div>
                <button onClick={handleSave} disabled={loading} className="w-full h-12 bg-sky-600 hover:bg-sky-500 text-white rounded font-bold shadow-md flex items-center justify-center gap-2 active:scale-95 disabled:opacity-50 transition-all">
                  <Save className="w-5 h-5" /> {loading ? "保存中..." : "受注を確定する"}
                </button>
              </div>
            </div>
          ) : (
            <div className="bg-white border rounded-lg shadow-sm overflow-hidden animate-in slide-in-from-bottom-2 duration-300">
              <div className="px-6 py-4 border-b flex justify-between items-center bg-slate-50/50">
                <h2 className="text-sm font-bold text-slate-600 flex items-center gap-2">
                  <History className="w-4 h-4" /> 最近の受注履歴
                </h2>
                <button onClick={fetchHistory} className="text-slate-400 hover:text-sky-600 transition-colors">
                  <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                </button>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 text-slate-500 font-bold uppercase tracking-wider border-b">
                    <tr>
                      <th className="px-6 py-3">受注日時</th>
                      <th className="px-6 py-3">番号</th>
                      <th className="px-6 py-3">得意先</th>
                      <th className="px-6 py-3">商品 / カテゴリ</th>
                      <th className="px-6 py-3 text-right">金額</th>
                      <th className="px-6 py-3 text-center">操作</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {history.length === 0 ? (
                      <tr><td colSpan={6} className="px-6 py-10 text-center text-slate-400 italic">履歴はありません</td></tr>
                    ) : (
                      history.map(row => (
                        <tr key={row.orderNo} className="hover:bg-slate-50 transition-colors">
                          <td className="px-6 py-4 text-slate-500 whitespace-nowrap">{new Date(row.orderDate).toLocaleString('ja-JP')}</td>
                          <td className="px-6 py-4 font-mono font-bold text-slate-700">{row.orderNo}</td>
                          <td className="px-6 py-4 font-bold">{row.customerName}</td>
                          <td className="px-6 py-4">
                            <div className="font-medium text-slate-800">{row.itemName}</div>
                            <div className="text-[10px] text-slate-400">{row.categoryName}</div>
                          </td>
                          <td className="px-6 py-4 text-right font-mono font-bold text-slate-900">¥{row.totalAmount?.toLocaleString() ?? "0"}</td>
                          <td className="px-6 py-4 text-center">
                            <button 
                              onClick={() => handleDelete(row.orderNo)}
                              className="p-2 text-slate-300 hover:text-red-500 hover:bg-red-50 rounded-full transition-all"
                              title="受注取消"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </main>

      <footer className="h-8 px-4 border-t bg-white text-[10px] flex items-center text-slate-400 justify-between">
        <div className="flex gap-4">
          <span>Target: .NET 8 Web API + React</span>
          <span>Database: PostgreSQL</span>
        </div>
        <span>© 2026 Modernization Demo Lab</span>
      </footer>
    </div>
  )
}

export default App
