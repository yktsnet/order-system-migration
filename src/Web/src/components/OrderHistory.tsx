import { useState } from "react"
import { History, RefreshCw, Download, Trash2 } from "lucide-react"
import type { Category, OrderHistory as HistoryType } from "../types"

interface OrderHistoryProps {
  categories: Category[];
  history: HistoryType[];
  loading: boolean;
  onFetch: (filters: {
    customerName?: string;
    itemName?: string;
    categoryId?: number;
    from?: string;
    to?: string;
  }) => Promise<void>;
  onDelete: (orderNo: string) => Promise<void>;
  onExportCsv: (filters: {
    customerName?: string;
    itemName?: string;
    categoryId?: number;
    from?: string;
    to?: string;
  }) => void;
}

export function OrderHistory({
  categories,
  history,
  loading,
  onFetch,
  onDelete,
  onExportCsv,
}: OrderHistoryProps) {
  // 履歴フィルタのローカル状態
  const [filterCustomer, setFilterCustomer] = useState("");
  const [filterItem, setFilterItem] = useState("");
  const [filterCategoryId, setFilterCategoryId] = useState(0);
  const [filterFrom, setFilterFrom] = useState("");
  const [filterTo, setFilterTo] = useState("");

  const getFilterParams = () => {
    return {
      customerName: filterCustomer || undefined,
      itemName: filterItem || undefined,
      categoryId: filterCategoryId > 0 ? filterCategoryId : undefined,
      from: filterFrom || undefined,
      to: filterTo || undefined,
    };
  };

  const handleSearch = () => {
    onFetch(getFilterParams());
  };

  const handleCsvClick = () => {
    onExportCsv(getFilterParams());
  };

  const handleDeleteClick = (orderNo: string) => {
    onDelete(orderNo);
  };

  return (
    <div className="bg-white border rounded-lg shadow-sm overflow-hidden animate-in slide-in-from-bottom-2 duration-300">
      <div className="px-6 py-4 border-b flex justify-between items-center bg-slate-50/50">
        <h2 className="text-sm font-bold text-slate-600 flex items-center gap-2">
          <History className="w-4 h-4" /> 最近の受注履歴
        </h2>
        <button
          onClick={handleSearch}
          className="text-slate-400 hover:text-sky-600 transition-colors"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
        </button>
      </div>
      <div className="px-4 py-3 border-b bg-white flex flex-wrap gap-3 items-end">
        <div className="w-full sm:w-auto">
          <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">得意先名</label>
          <input
            type="text"
            value={filterCustomer}
            onChange={e => setFilterCustomer(e.target.value)}
            onKeyDown={e => e.key === "Enter" && handleSearch()}
            placeholder="部分一致"
            className="h-8 px-2 border border-slate-300 rounded text-xs focus:border-sky-600 outline-none w-full sm:w-36"
          />
        </div>
        <div className="w-full sm:w-auto">
          <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">商品名</label>
          <input
            type="text"
            value={filterItem}
            onChange={e => setFilterItem(e.target.value)}
            onKeyDown={e => e.key === "Enter" && handleSearch()}
            placeholder="部分一致"
            className="h-8 px-2 border border-slate-300 rounded text-xs focus:border-sky-600 outline-none w-full sm:w-36"
          />
        </div>
        <div className="w-full sm:w-auto">
          <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">カテゴリ</label>
          <select
            value={filterCategoryId}
            onChange={e => setFilterCategoryId(Number(e.target.value))}
            className="h-8 px-2 border border-slate-300 rounded text-xs bg-white focus:border-sky-600 outline-none w-full sm:w-28"
          >
            <option value={0}>全て</option>
            {categories.map(c => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>
        <div className="w-full sm:w-auto">
          <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">開始日</label>
          <input
            type="date"
            value={filterFrom}
            onChange={e => setFilterFrom(e.target.value)}
            className="h-8 px-2 border border-slate-300 rounded text-xs focus:border-sky-600 outline-none w-full"
          />
        </div>
        <div className="w-full sm:w-auto">
          <label className="block text-[10px] font-bold text-slate-400 uppercase mb-1">終了日</label>
          <input
            type="date"
            value={filterTo}
            onChange={e => setFilterTo(e.target.value)}
            className="h-8 px-2 border border-slate-300 rounded text-xs focus:border-sky-600 outline-none w-full"
          />
        </div>
        <button
          onClick={handleSearch}
          disabled={loading}
          className="h-8 px-4 bg-sky-600 hover:bg-sky-500 disabled:opacity-50 text-white text-xs font-bold rounded transition-colors w-full sm:w-auto"
        >
          検索
        </button>
        <button
          onClick={handleCsvClick}
          className="h-8 px-4 bg-slate-600 hover:bg-slate-500 text-white text-xs font-bold rounded transition-colors flex items-center justify-center gap-1 w-full sm:w-auto"
        >
          <Download className="w-3 h-3" /> CSV
        </button>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs min-w-[700px]">
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
              <tr>
                <td colSpan={6} className="px-6 py-10 text-center text-slate-400 italic">
                  履歴はありません
                </td>
              </tr>
            ) : (
              history.map(row => (
                <tr key={row.orderNo} className="hover:bg-slate-50 transition-colors">
                  <td className="px-6 py-4 text-slate-500 whitespace-nowrap">
                    {new Date(row.orderDate).toLocaleString("ja-JP")}
                  </td>
                  <td className="px-6 py-4 font-mono font-bold text-slate-700">{row.orderNo}</td>
                  <td className="px-6 py-4 font-bold">{row.customerName}</td>
                  <td className="px-6 py-4">
                    <div className="font-medium text-slate-800">{row.itemName}</div>
                    <div className="text-[10px] text-slate-400">{row.categoryName}</div>
                  </td>
                  <td className="px-6 py-4 text-right font-mono font-bold text-slate-900">
                    ¥{row.totalAmount?.toLocaleString() ?? "0"}
                  </td>
                  <td className="px-6 py-4 text-center">
                    <button
                      onClick={() => handleDeleteClick(row.orderNo)}
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
  )
}
