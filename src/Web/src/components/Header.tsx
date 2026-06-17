import { Package2, Database, Cloud } from "lucide-react"

export function Header() {
  return (
    <header className="flex items-center justify-between h-10 px-4 border-b bg-white shrink-0 shadow-sm flex-wrap gap-2">
      <div className="flex items-center gap-2">
        <Package2 className="w-4 h-4 text-sky-600" />
        <span className="font-bold text-slate-700 uppercase tracking-tight">受注管理システム</span>
      </div>
      <div className="hidden sm:flex items-center gap-4 text-[11px] text-slate-500">
        <div className="flex items-center gap-1">
          <Database className="w-3 h-3 text-green-500" /> DB: Online
        </div>
        <div className="flex items-center gap-1">
          <Cloud className="w-3 h-3 text-green-500" /> S3: Connected
        </div>
      </div>
    </header>
  )
}
