import { NavLink } from "react-router-dom";
import { LogOut, X, type LucideIcon } from "lucide-react";

import { apiResourceUrl } from "../../lib/api";

export type AppNavItem = {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
  minAccess?: number;
};

export type AppNavGroup = {
  label: string;
  items: AppNavItem[];
};

type AppSidebarProps = {
  open: boolean;
  onClose: () => void;
  companyName?: string;
  companyLogoUrl?: string | null;
  appName?: string;
  userName?: string;
  userRole?: string;
  navGroups: AppNavGroup[];
  onLogout: () => void;
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

function navLinkClass(isActive: boolean) {
  return [
    "group flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-all",
    isActive
      ? "bg-[linear-gradient(135deg,rgba(13,148,136,0.96),rgba(5,150,105,0.88))] text-white shadow-[0_14px_28px_rgba(13,148,136,0.22)]"
      : "text-slate-600 hover:bg-white/85 hover:text-slate-950",
  ].join(" ");
}

export function AppSidebar({
  open,
  onClose,
  companyName,
  companyLogoUrl,
  appName = "Serviços App",
  userName,
  userRole,
  navGroups,
  onLogout,
}: AppSidebarProps) {
  return (
    <>
      {open ? (
        <button
          type="button"
          aria-label="Fechar menu"
          className="fixed inset-0 z-40 bg-slate-950/40 backdrop-blur-[1px] lg:hidden"
          onClick={onClose}
        />
      ) : null}

      <aside
        className={[
          "fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-white/60 bg-[linear-gradient(180deg,rgba(255,255,255,0.96),rgba(240,252,250,0.96))] shadow-[0_24px_60px_rgba(15,23,42,0.1)] backdrop-blur-xl transition-transform duration-300",
          open ? "translate-x-0" : "-translate-x-full",
          "lg:static lg:z-0 lg:translate-x-0 lg:shadow-none",
        ].join(" ")}
      >
        <div className="flex items-center justify-between border-b border-emerald-100/80 px-5 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-emerald-200/70 bg-white shadow-sm">
              {companyLogoUrl ? (
                <img
                  src={apiResourceUrl(companyLogoUrl)}
                  alt={companyName ? `Logo de ${companyName}` : "Logo da empresa"}
                  className="h-full w-full object-contain p-1.5"
                />
              ) : (
                <span className="text-sm font-bold text-slate-900">
                  {getInitials(companyName ?? appName)}
                </span>
              )}
            </div>

            <div className="min-w-0">
              <strong className="block truncate text-sm font-semibold text-slate-950">
                {appName}
              </strong>
              <small className="block truncate text-xs text-slate-500">
                {companyName ?? "Minha loja"}
              </small>
            </div>
          </div>

          <button
            className="inline-flex h-10 w-10 items-center justify-center rounded-lg text-slate-500 transition hover:bg-white hover:text-slate-900 lg:hidden"
            type="button"
            onClick={onClose}
            aria-label="Fechar menu"
          >
            <X size={20} />
          </button>
        </div>

        <nav className="flex-1 space-y-6 overflow-y-auto px-4 py-5" aria-label="Menu principal">
          {navGroups.map((group) => (
            <div key={group.label}>
              <span className="mb-2 block px-3 text-[11px] font-semibold uppercase tracking-[0.14em] text-teal-700/70">
                {group.label}
              </span>

              <div className="space-y-1">
                {group.items.map((item) => {
                  const Icon = item.icon;

                  return (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      end={item.end ?? item.to === "/"}
                      className={({ isActive }) => navLinkClass(isActive)}
                      onClick={onClose}
                    >
                      <Icon
                        size={18}
                        className="shrink-0 transition group-hover:scale-[1.03]"
                      />
                      <span className="truncate">{item.label}</span>
                    </NavLink>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        <div className="border-t border-emerald-100/80 p-4">
          <div className="rounded-lg border border-white/70 bg-white/80 p-3 shadow-sm backdrop-blur">
            <div className="mb-3 min-w-0">
              <strong className="block truncate text-sm text-slate-950">
                {userName ?? "Usuário"}
              </strong>
              <small className="block truncate text-xs text-slate-500">
                {userRole ?? "Perfil"}
              </small>
            </div>

            <button
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-emerald-200/70 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:border-emerald-300 hover:bg-emerald-50/70 hover:text-slate-950"
              type="button"
              onClick={onLogout}
            >
              <LogOut size={16} />
              Sair
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}
