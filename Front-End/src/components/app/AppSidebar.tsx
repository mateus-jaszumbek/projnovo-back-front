import { NavLink } from "react-router-dom";
import { LogOut, X, type LucideIcon } from "lucide-react";

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
  appName?: string;
  userName?: string;
  userRole?: string;
  navGroups: AppNavGroup[];
  onLogout: () => void;
};

function navLinkClass(isActive: boolean) {
  return [
    "group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all",
    isActive
      ? "bg-slate-900 text-white shadow-sm"
      : "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
  ].join(" ");
}

export function AppSidebar({
  open,
  onClose,
  companyName,
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
          "fixed inset-y-0 left-0 z-50 flex w-72 flex-col border-r border-slate-200 bg-white/95 shadow-2xl backdrop-blur transition-transform duration-300",
          open ? "translate-x-0" : "-translate-x-full",
          "lg:static lg:z-0 lg:translate-x-0 lg:shadow-none",
        ].join(" ")}
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-900 text-sm font-bold text-white shadow-sm">
              SA
            </div>

            <div className="min-w-0">
              <strong className="block truncate text-sm font-semibold text-slate-900">
                {appName}
              </strong>
              <small className="block truncate text-xs text-slate-500">
                {companyName ?? "Minha loja"}
              </small>
            </div>
          </div>

          <button
            className="inline-flex h-10 w-10 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 lg:hidden"
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
              <span className="mb-2 block px-3 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-400">
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

        <div className="border-t border-slate-200 p-4">
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-3">
            <div className="mb-3 min-w-0">
              <strong className="block truncate text-sm text-slate-900">
                {userName ?? "Usuário"}
              </strong>
              <small className="block truncate text-xs text-slate-500">
                {userRole ?? "Perfil"}
              </small>
            </div>

            <button
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-100 hover:text-slate-900"
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
