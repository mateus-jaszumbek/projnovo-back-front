import { useEffect, useState } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import {
  Banknote,
  BarChart3,
  Building2,
  Cake,
  Columns3,
  CreditCard,
  FileText,
  LayoutDashboard,
  LifeBuoy,
  Package,
  Tags,
  Truck,
  Smartphone,
  UserCog,
  UsersRound,
  Wallet,
  Wrench,
} from "lucide-react";

import { useAuth } from "../auth/AuthContext";
import { AppHeader } from "../components/app/AppHeader";
import { AppSidebar, type AppNavGroup } from "../components/app/AppSidebar";
import { SiteFooter } from "../components/app/SiteFooter";
import { COMPANY_UPDATED_EVENT, apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";

import { DashboardPage } from "../pages/DashboardPage";
import {
  AparelhosPage,
  CategoriasPecaPage,
  ClientesPage,
  FornecedoresPage,
  PecasPage,
  ServicosPage,
  TecnicosPage,
} from "../pages/CadastroPages";
import { VendasPage } from "../pages/VendasPage";
import { OrdensServicoPage } from "../pages/OrdensServicoPage";
import { CaixaPage } from "../pages/CaixaPage";
import { FinanceiroPage } from "../pages/FinanceiroPage";
import { ComprasEstoquePage } from "../pages/ComprasEstoquePage";
import { FiscalPage } from "../pages/FiscalPage";
import { UsuariosPage } from "../pages/UsuariosPage";
import { RelatoriosPage } from "../pages/RelatoriosPage";
import { KanbanPage } from "../pages/KanbanPage";
import { ContatoClientesPage } from "../pages/ContatoClientesPage";
import { SupportPage } from "../pages/SupportPage";
import { EmpresaPage } from "../pages/EmpresaPage";
import { ConfiguracaoPagamentoPage } from "../pages/ConfiguracaoPagamentoPage";

const navGroups: AppNavGroup[] = [
  {
    label: "Visao geral",
    items: [
      { to: "/", label: "Painel", icon: LayoutDashboard, minAccess: 1 },
      { to: "/empresa", label: "Empresa", icon: Building2, minAccess: 1 },
    ],
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
      { to: "/categorias-peca", label: "Categorias de pecas", icon: Tags, minAccess: 3 },
      { to: "/servicos", label: "Servicos", icon: FileText, minAccess: 2 },
      { to: "/tecnicos", label: "Tecnicos", icon: UserCog, minAccess: 2 },
    ],
  },
  {
    label: "Gestao",
    items: [
      { to: "/kanban", label: "Kanban", icon: Columns3, minAccess: 2 },
      { to: "/contato-clientes", label: "Contato com clientes", icon: Cake, minAccess: 3 },
      { to: "/compras-estoque", label: "Compras", icon: Package, minAccess: 3 },
      { to: "/caixa", label: "Caixa", icon: Wallet, minAccess: 4 },
      { to: "/financeiro", label: "Financeiro", icon: Banknote, minAccess: 4 },
      { to: "/relatorios", label: "Relatorios", icon: BarChart3, minAccess: 3 },
      { to: "/fiscal", label: "Fiscal", icon: FileText, minAccess: 5 },
      { to: "/pagamento", label: "Pagamento", icon: CreditCard, minAccess: 5 },
      { to: "/usuarios", label: "Usuarios", icon: UserCog, minAccess: 5 },
    ],
  },
  {
    label: "Ajuda",
    items: [{ to: "/suporte", label: "Suporte", icon: LifeBuoy, minAccess: 1 }],
  },
];

export function Shell() {
  const { session, sair } = useAuth();
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const [company, setCompany] = useState<ApiRecord | null>(null);
  const companyLogoUrl =
    typeof company?.logoUrl === "string" && company.logoUrl.trim()
      ? company.logoUrl
      : null;
  const companyName =
    typeof company?.nomeFantasia === "string" && company.nomeFantasia.trim()
      ? company.nomeFantasia
      : session?.empresaNomeFantasia;

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

  useEffect(() => {
    let active = true;

    async function loadCompanyLogo() {
      if (!session) {
        if (active) setCompany(null);
        return;
      }

      try {
        const empresa = await apiRequest<ApiRecord>("/empresas/minha");
        if (active) setCompany(empresa);
      } catch {
        if (active) setCompany(null);
      }
    }

    void loadCompanyLogo();
    const handleCompanyUpdated = () => {
      void loadCompanyLogo();
    };

    window.addEventListener(COMPANY_UPDATED_EVENT, handleCompanyUpdated);

    return () => {
      active = false;
      window.removeEventListener(COMPANY_UPDATED_EVENT, handleCompanyUpdated);
    };
  }, [session?.empresaId]);

  if (currentNavItem && !canAccess(currentNavItem.minAccess)) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="min-h-screen text-slate-900">
      <div className="flex min-h-screen">
        <AppSidebar
          open={open}
          onClose={() => setOpen(false)}
          companyName={companyName}
          companyLogoUrl={companyLogoUrl}
          userName={session?.nome}
          userRole={session?.perfil}
          navGroups={visibleNavGroups}
          onLogout={sair}
        />

        <div className="flex min-w-0 flex-1 flex-col">
          <AppHeader
            onOpenMenu={() => setOpen(true)}
            companyName={companyName}
            companyLogoUrl={companyLogoUrl}
          />

          <main
            className={[
              "min-w-0 flex-1",
              isKanban ? "overflow-hidden p-3 lg:p-4" : "overflow-y-auto px-4 py-5 lg:px-6 lg:py-6",
            ].join(" ")}
          >
            <Routes>
              <Route index element={<DashboardPage />} />
              <Route path="empresa" element={<EmpresaPage />} />
              <Route path="clientes" element={<ClientesPage />} />
              <Route path="aparelhos" element={<AparelhosPage />} />
              <Route path="fornecedores" element={<FornecedoresPage />} />
              <Route path="ordens-servico" element={<OrdensServicoPage />} />
              <Route path="vendas" element={<VendasPage />} />
              <Route path="vendas/nova" element={<VendasPage />} />
              <Route path="pecas" element={<PecasPage />} />
              <Route path="categorias-peca" element={<CategoriasPecaPage />} />
              <Route path="servicos" element={<ServicosPage />} />
              <Route path="kanban" element={<KanbanPage />} />
              <Route path="contato-clientes" element={<ContatoClientesPage />} />
              <Route path="compras-estoque" element={<ComprasEstoquePage />} />
              <Route path="caixa" element={<CaixaPage />} />
              <Route path="financeiro" element={<FinanceiroPage />} />
              <Route path="relatorios" element={<RelatoriosPage />} />
              <Route path="fiscal" element={<FiscalPage />} />
              <Route path="pagamento" element={<ConfiguracaoPagamentoPage />} />
              <Route path="tecnicos" element={<TecnicosPage />} />
              <Route path="usuarios" element={<UsuariosPage />} />
              <Route path="suporte" element={<SupportPage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>

          <SiteFooter company={company} companyName={companyName} />
        </div>
      </div>
    </div>
  );
}
