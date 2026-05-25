import { Package2, Database, Cloud } from "lucide-react"

function App() {
  return (
    // 画面全体を占有するコンテナ
    <div className="flex flex-col h-screen w-full bg-slate-100 font-sans">
      
      {/* ヘッダー：横いっぱいに広げる */}
      <header className="flex items-center justify-between h-10 px-4 border-b bg-white shrink-0 shadow-sm">
        <div className="flex items-center gap-2">
          <Package2 className="w-4 h-4 text-blue-600" />
          <span className="font-bold text-slate-700">受注管理システム - .NET Modernization Lab</span>
        </div>
        <div className="flex items-center gap-4 text-[11px] text-slate-500">
          <div className="flex items-center gap-1"><Database className="w-3 h-3"/> API: Online</div>
          <div className="flex items-center gap-1"><Cloud className="w-3 h-3"/> S3: Connected</div>
        </div>
      </header>

      {/* メイン：残りのスペースをすべて使う */}
      <main className="flex-1 p-4 overflow-auto">
        <div className="w-full max-w-6xl mx-auto">
          {/* フォームの土台となる「白いカード」 */}
          <div className="bg-white border border-slate-200 rounded shadow-sm min-h-[400px] flex flex-col">
            <div className="px-4 py-2 border-b bg-slate-50/50">
              <h2 className="text-xs font-semibold text-slate-500 uppercase tracking-wider">受注登録フォーム</h2>
            </div>
            <div className="flex-1 p-8 flex items-center justify-center border-2 border-dashed border-slate-100 m-4 rounded">
              <p className="text-slate-400 italic">Phase 3-B: ここに WinForms の項目を配置します</p>
            </div>
          </div>
        </div>
      </main>

      {/* フッター：固定高さ */}
      <footer className="h-6 px-4 border-t bg-white text-[10px] flex items-center text-slate-400 justify-between">
        <span>Target: .NET 8 Web API + React (Tailwind v4)</span>
        <span>© 2026 Modernization Lab</span>
      </footer>
    </div>
  )
}

export default App
