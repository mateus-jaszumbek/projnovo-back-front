import { useEffect, useState } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import {
  Banknote,
  BarChart3,
  Columns3,
  FileText,
  LayoutDashboard,
  Package,
  Truck,
  Smartphone,
  UserCog,
  UsersRound,
  Wrench,
} from "lucide-react";

import { useAuth } from "../auth/AuthContext";
import { AppSidebar, type AppNavGroup } from "../components/app/AppSidebar";
import { AppHeader } from "../components/app/AppHeader";

import { DashboardPage } from "../pages/DashboardPage";
import {
  AparelhosPage,
  ClientesPage,
  FornecedoresPage,
  PecasPage,
  ServicosPage,
  TecnicosPage,
} from "../pages/CadastroPages";
import { VendasPage } from "../pages/VendasPage";
import { OrdensServicoPage } from "../pages/OrdensServicoPage";
import { FinanceiroPage } from "../pages/FinanceiroPage";
import { ComprasEstoquePage } from "../pages/ComprasEstoquePage";
import { FiscalPage } from "../pages/FiscalPage";
import { UsuariosPage } from "../pages/UsuariosPage";
import { RelatoriosPage } from "../pages/RelatoriosPage";
import { ModulosPage } from "../pages/ModulosPage";
import { KanbanPage } from "../pages/KanbanPage";

const navGroups: AppNavGroup[] = [
  {
    label: "Visao geral",
    items: [{ to: "/", label: "Painel", icon: LayoutDashboard, minAccess: 1 }],
  },
  {
    label: "Atendimento",
    items: [
      { to: "/ordens-servico", label: "Ordens", icon: Wrench, minAccess: 1 },
      { to: "/vendas", label: "Vendas", icon: Banknote, minAccess: 2 },
    ],
  },
  {
    label: "Cadastros",
    items: [
      { to: "/clientes", label: "Clientes", icon: UsersRound, minAccess: 1 },
      { to: "/aparelhos", label: "Aparelhos", icon: Smartphone, minAccess: 1 },
      { to: "/fornecedores", label: "Fornecedores", icon: Truck, minAccess: 3 },
      { to: "/pecas", label: "Pecas", icon: Package, minAccess: 3 },
      { to: "/servicos", label: "Servicos", icon: FileText, minAccess: 2 },
      { to: "/tecnicos", label: "Tecnicos", icon: UserCog, minAccess: 2 },
    ],
  },
  {
    label: "Gestao",
    items: [
      { to: "/modulos", label: "Modulos", icon: FileText, minAccess: 4 },
      { to: "/kanban", label: "Kanban", icon: Columns3, minAccess: 2 },
      { to: "/compras-estoque", label: "Compras", icon: Package, minAccess: 3 },
      { to: "/financeiro", label: "Financeiro", icon: Banknote, minAccess: 4 },
      { to: "/relatorios", label: "Relatorios", icon: BarChart3, minAccess: 3 },
      { to: "/fiscal", label: "Fiscal", icon: FileText, minAccess: 5 },
      { to: "/usuarios", label: "Usuarios", icon: UserCog, minAccess: 5 },
    ],
  },
];

export function Shell() {
  const { session, sair } = useAuth();
  const location = useLocation();
  const [open, setOpen] = useState(false);

  const isKanban = location.pathname.startsWith("/kanban");
  const userLevel = session?.isSuperAdmin ? 5 : session?.nivelAcesso ?? 1;
  const userRole = String(session?.perfil ?? "").toLowerCase();
  const isCompanyAdmin = Boolean(
    session?.isSuperAdmin ||
      ["owner", "admin", "administrador", "super-admin", "superadmin"].includes(userRole),
  );
  const canAccess = (minAccess = 1) => isCompanyAdmin || userLevel >= minAccess;
  const visibleNavGroups = navGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => canAccess(item.minAccess)),
    }))
    .filter((group) => group.items.length > 0);
  const currentNavItem = navGroups
    .flatMap((group) => group.items)
    .find((item) => (item.to === "/" ? location.pathname === "/" : location.pathname.startsWith(item.to)));

  useEffect(() => {
    setOpen(false);
  }, [location.pathname]);

  if (currentNavItem && !canAccess(currentNavItem.minAccess)) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="min-h-screen bg-slate-100 text-slate-900">
      <div className="flex min-h-screen">
        <AppSidebar
          open={open}
          onClose={() => setOpen(false)}
          companyName={session?.empresaNomeFantasia}
          userName={session?.nome}
          userRole={session?.perfil}
          navGroups={visibleNavGroups}
          onLogout={sair}
        />

        <div className="flex min-w-0 flex-1 flex-col">
          <AppHeader
            companyName={session?.empresaNomeFantasia}
            email={session?.email}
            onOpenSidebar={() => setOpen(true)}
          />

          <main
            className={[
              "min-w-0 flex-1",
              isKanban ? "overflow-hidden p-3 lg:p-4" : "overflow-y-auto p-4 lg:p-6",
            ].join(" ")}
          >
            <Routes>
              <Route index element={<DashboardPage />} />
              <Route path="clientes" element={<ClientesPage />} />
              <Route path="aparelhos" element={<AparelhosPage />} />
              <Route path="fornecedores" element={<FornecedoresPage />} />
              <Route path="ordens-servico" element={<OrdensServicoPage />} />
              <Route path="vendas" element={<VendasPage />} />
              <Route path="pecas" element={<PecasPage />} />
              <Route path="servicos" element={<ServicosPage />} />
              <Route path="modulos" element={<ModulosPage />} />
              <Route path="kanban" element={<KanbanPage />} />
              <Route path="compras-estoque" element={<ComprasEstoquePage />} />
              <Route path="financeiro" element={<FinanceiroPage />} />
              <Route path="relatorios" element={<RelatoriosPage />} />
              <Route path="fiscal" element={<FiscalPage />} />
              <Route path="tecnicos" element={<TecnicosPage />} />
              <Route path="usuarios" element={<UsuariosPage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
        </div>
      </div>
    </div>
  );
}
