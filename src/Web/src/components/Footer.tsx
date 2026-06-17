export function Footer() {
  return (
    <footer className="py-3 px-4 border-t bg-white text-[10px] flex flex-col sm:flex-row items-center text-slate-400 justify-between gap-2 shrink-0">
      <div className="flex flex-wrap justify-center gap-4 text-center sm:text-left">
        <span>Target: .NET 8 Web API + React</span>
        <span>Database: PostgreSQL</span>
        <span>Agent: FastAPI + LangGraph</span>
      </div>
      <span>© 2026 Modernization Demo Lab</span>
    </footer>
  )
}
