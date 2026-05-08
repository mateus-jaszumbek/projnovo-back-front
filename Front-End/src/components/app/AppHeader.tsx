import { Menu } from "lucide-react";
import { apiResourceUrl } from "../../lib/api";

type AppHeaderProps = {
  onOpenMenu: () => void;
  companyName?: string;
  companyLogoUrl?: string | null;
  appName?: string;
};

function getInitials(value?: string) {
  const parts = String(value ?? "")
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2);

  if (parts.length === 0) return "SA";
  return parts.map((part) => part[0]?.toUpperCase() ?? "").join("");
}

export function AppHeader({
  onOpenMenu,
  companyName,
  companyLogoUrl,
  appName = "Serviços App",
}: AppHeaderProps) {
  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b border-emerald-100/80 bg-white/95 px-4 backdrop-blur lg:hidden">
      <button
        type="button"
        aria-label="Abrir menu"
        onClick={onOpenMenu}
        className="inline-flex h-9 w-9 items-center justify-center rounded-lg text-slate-600 transition hover:bg-slate-100 hover:text-slate-900"
      >
        <Menu size={22} />
      </button>

      <span className="min-w-0 flex-1 truncate text-sm font-semibold text-slate-900">
        {companyName ?? appName}
      </span>

      <div className="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-md border border-emerald-200/70 bg-white shadow-sm">
        {companyLogoUrl ? (
          <img
            src={apiResourceUrl(companyLogoUrl)}
            alt=""
            className="h-full w-full object-contain p-0.5"
          />
        ) : (
          <span className="text-xs font-bold text-slate-900">
            {getInitials(companyName ?? appName)}
          </span>
        )}
      </div>
    </header>
  );
}
