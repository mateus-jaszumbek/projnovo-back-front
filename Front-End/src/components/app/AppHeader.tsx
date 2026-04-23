import { Menu } from "lucide-react";

type AppHeaderProps = {
  companyName?: string;
  email?: string;
  onOpenSidebar?: () => void;
  rightContent?: React.ReactNode;
};

export function AppHeader({
  companyName,
  email,
  onOpenSidebar,
  rightContent,
}: AppHeaderProps) {
  return (
    <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/80 backdrop-blur">
      <div className="flex items-center justify-between gap-3 px-4 py-3 lg:px-6">
        <div className="flex min-w-0 items-center gap-3">
          <button
            className="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-700 shadow-sm transition hover:bg-slate-50 lg:hidden"
            type="button"
            onClick={onOpenSidebar}
            aria-label="Abrir menu"
          >
            <Menu size={20} />
          </button>

          <div className="min-w-0">
            <strong className="block truncate text-sm font-semibold text-slate-900">
              {companyName ?? "Loja"}
            </strong>
            <span className="block truncate text-xs text-slate-500">
              {email ?? "Sem e-mail"}
            </span>
          </div>
        </div>

        {rightContent ?? (
          <div className="hidden items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-500 shadow-sm md:flex">
            <span className="inline-block h-2 w-2 rounded-full bg-emerald-500" />
            Ambiente ativo
          </div>
        )}
      </div>
    </header>
  );
}