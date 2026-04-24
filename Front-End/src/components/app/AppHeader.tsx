import { Menu } from "lucide-react";

import { apiResourceUrl } from "../../lib/api";

type AppHeaderProps = {
  companyName?: string;
  companyLogoUrl?: string | null;
  email?: string;
  onOpenSidebar?: () => void;
  rightContent?: React.ReactNode;
};

function getInitials(value?: string) {
  const parts = String(value ?? "")
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2);

  if (parts.length === 0) return "LG";
  return parts.map((part) => part[0]?.toUpperCase() ?? "").join("");
}

export function AppHeader({
  companyName,
  companyLogoUrl,
  email,
  onOpenSidebar,
  rightContent,
}: AppHeaderProps) {
  return (
    <header className="sticky top-0 z-30 border-b border-white/40 bg-white/55 backdrop-blur-xl">
      <div className="flex items-center justify-between gap-3 px-4 py-4 lg:px-6">
        <div className="flex min-w-0 items-center gap-3">
          <button
            className="inline-flex h-11 w-11 items-center justify-center rounded-lg border border-emerald-200/70 bg-white/90 text-slate-700 shadow-sm transition hover:border-emerald-300 hover:bg-white lg:hidden"
            type="button"
            onClick={onOpenSidebar}
            aria-label="Abrir menu"
          >
            <Menu size={20} />
          </button>

          <div className="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-emerald-200/70 bg-white shadow-sm">
            {companyLogoUrl ? (
              <img
                src={apiResourceUrl(companyLogoUrl)}
                alt={companyName ? `Logo de ${companyName}` : "Logo da empresa"}
                className="h-full w-full object-contain p-1.5"
              />
            ) : (
              <span className="text-sm font-semibold text-slate-700">
                {getInitials(companyName)}
              </span>
            )}
          </div>

          <div className="min-w-0">
            <strong className="block truncate text-sm font-semibold text-slate-950">
              {companyName ?? "Loja"}
            </strong>
            <span className="block truncate text-xs text-slate-500">
              {email ?? "Sem e-mail"}
            </span>
          </div>
        </div>

        {rightContent ?? (
          <div className="hidden items-center gap-2 rounded-md border border-emerald-200/70 bg-white/90 px-3 py-2 text-xs font-medium text-slate-600 shadow-sm md:flex">
            <span className="inline-block h-2 w-2 rounded-full bg-teal-500" />
            Operacao em andamento
          </div>
        )}
      </div>
    </header>
  );
}
