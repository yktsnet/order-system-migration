import { useState, useEffect } from "react"
import { Save } from "lucide-react"
import type { Category } from "../types"

interface OrderFormProps {
  categories: Category[];
  loading: boolean;
  onSave: (order: {
    orderNo: string;
    customerName: string;
    categoryId: number;
    itemName: string;
    price: number;
    qty: number;
  }) => Promise<boolean>;
}

export function OrderForm({ categories, loading, onSave }: OrderFormProps) {
  const [orderNo, setOrderNo] = useState("");
  const [customer, setCustomer] = useState("");
  const [selectedCat, setSelectedCat] = useState<number>(1);
  const [itemName, setItemName] = useState("");
  const [price, setPrice] = useState(0);
  const [qty, setQty] = useState(1);

  // 初回および登録成功後に受注番号を生成
  const generateOrderNo = () => {
    setOrderNo(`ORD-${new Date().getTime().toString().slice(-6)}`);
  };

  useEffect(() => {
    generateOrderNo();
  }, []);

  // カテゴリ一覧が読み込まれたら、初期選択を設定
  useEffect(() => {
    if (categories.length > 0 && !selectedCat) {
      setSelectedCat(categories[0].id);
    }
  }, [categories]);

  const subTotal = price * qty;
  const tax = Math.floor(subTotal * 0.1);
  const total = subTotal + tax;

  const handleSubmit = async () => {
    if (!customer.trim() || !itemName.trim() || price <= 0) {
      alert("得意先名、商品名、正しい単価を入力してください。");
      return;
    }
    const success = await onSave({
      orderNo,
      customerName: customer,
      categoryId: selectedCat,
      itemName,
      price,
      qty,
    });

    if (success) {
      setCustomer("");
      setItemName("");
      setPrice(0);
      setQty(1);
      generateOrderNo();
    }
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-12 gap-6 animate-in fade-in duration-300">
      <div className="col-span-1 md:col-span-8 bg-white border rounded-lg shadow-sm p-4 sm:p-6 space-y-6">
        <div className="flex items-center justify-between border-b pb-4 mb-2">
          <h2 className="text-sm font-bold text-slate-600">新規受注入力</h2>
          <span className="text-[10px] bg-sky-100 text-sky-700 px-2 py-0.5 rounded-full font-bold">READY</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="col-span-1 sm:col-span-2">
            <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">受注番号</label>
            <input
              type="text"
              value={orderNo}
              readOnly
              className="w-full h-10 px-3 border border-slate-200 rounded bg-slate-50 font-mono text-sm"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">得意先名</label>
            <input
              type="text"
              value={customer}
              onChange={e => setCustomer(e.target.value)}
              className="w-full h-10 px-3 border border-slate-300 rounded text-sm focus:border-sky-600 outline-none"
              placeholder="例：株式会社サンプル"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">カテゴリ</label>
            <select
              value={selectedCat}
              onChange={e => setSelectedCat(Number(e.target.value))}
              className="w-full h-10 px-3 border border-slate-300 rounded text-sm bg-white focus:border-sky-600 outline-none"
            >
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <div className="col-span-1 sm:col-span-2">
            <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">商品名称</label>
            <input
              type="text"
              value={itemName}
              onChange={e => setItemName(e.target.value)}
              className="w-full h-10 px-3 border border-slate-300 rounded text-sm focus:border-sky-600 outline-none"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">単価 (円)</label>
            <input
              type="number"
              value={price}
              onChange={e => setPrice(Number(e.target.value))}
              className="w-full h-10 px-3 border border-slate-300 rounded font-bold text-sm"
            />
          </div>
          <div>
            <label className="block text-[11px] font-bold text-slate-400 uppercase mb-1">数量</label>
            <input
              type="number"
              value={qty}
              onChange={e => setQty(Number(e.target.value))}
              className="w-full h-10 px-3 border border-slate-300 rounded font-bold text-sm"
            />
          </div>
        </div>
      </div>
      <div className="col-span-1 md:col-span-4 space-y-4">
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
            <div className={`text-3xl sm:text-4xl font-mono font-bold ${total > 1000000 ? "text-red-400" : "text-sky-400"} break-all`}>
              {total.toLocaleString()}
            </div>
          </div>
        </div>
        <button
          onClick={handleSubmit}
          disabled={loading}
          className="w-full h-12 bg-sky-600 hover:bg-sky-500 text-white rounded font-bold shadow-md flex items-center justify-center gap-2 active:scale-95 disabled:opacity-50 transition-all"
        >
          <Save className="w-5 h-5" /> {loading ? "保存中..." : "受注を確定する"}
        </button>
      </div>
    </div>
  )
}
