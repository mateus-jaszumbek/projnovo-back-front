import {
  AlertTriangle,
  Banknote,
  FileText,
  Package,
  Smartphone,
  UsersRound,
  Wrench,
  ArrowUpRight,
  CircleAlert,
  ClipboardList,
} from "lucide-react";

import { PageHeader } from "../components/app/PageHeader";
import { PageSection } from "../components/app/PageSection";
import { StatCard } from "../components/app/StartCard";
import { formatCurrency } from "../components/uiHelpers";
import { useList } from "../hooks/useApi";
import { apiBaseUrl } from "../lib/api";

function sumBy(rows: Record<string, unknown>[], key: string) {
  return rows.reduce((total, row) => total + Number(row[key] ?? 0), 0);
}

function InfoItem({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
      <span className="mt-0.5 inline-flex h-5 w-5 items-center justify-center rounded-full bg-slate-200 text-[10px] font-bold text-slate-700">
        •
      </span>
      <span>{children}</span>
    </div>
  );
}

export function DashboardPage() {
  const clientes = useList("/clientes");
  const pecas = useList("/pecas");
  const vendas = useList("/vendas");
  const ordens = useList("/ordens-servico");
  const documentos = useList("/documentos-fiscais");

  const receita = sumBy(vendas.data, "valorTotal");

  const estoqueBaixo = pecas.data.filter(
    (peca) => Number(peca.estoqueAtual ?? 0) <= Number(peca.estoqueMinimo ?? 0),
  ).length;

  const ordensAbertas = ordens.data.filter((item) =>
    ["ABERTA", "APROVADA", "EM_EXECUCAO", "PRONTA"].includes(String(item.status ?? "")),
  ).length;

  const ordensEmAndamento = ordens.data.filter((item) =>
    ["APROVADA", "EM_EXECUCAO"].includes(String(item.status ?? "")),
  ).length;

  const notasPendentes = documentos.data.filter((item) =>
    ["PENDENTE_ENVIO", "RASCUNHO", "REJEITADO"].includes(String(item.status ?? "")),
  ).length;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Painel"
        title="Resumo da loja"
        description="Acompanhe operação, estoque, caixa e documentos fiscais antes de abrir o atendimento."
        actions={
          <a
            className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50 hover:text-slate-900"
            href={`${apiBaseUrl().replace("/api", "")}/swagger`}
            target="_blank"
            rel="noreferrer"
          >
            Swagger API
            <ArrowUpRight size={16} />
          </a>
        }
      />


      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard
          title="Clientes"
          value={clientes.data.length}
          description="Base cadastrada"
          icon={UsersRound}
        />

        <StatCard
          title="Ordens abertas"
          value={ordensAbertas}
          description="Atendimento em aberto"
          icon={Smartphone}
          tone="warning"
        />

        <StatCard
          title="Receita registrada"
          value={formatCurrency(receita)}
          description="Total lançado"
          icon={Banknote}
          tone="success"
        />

        <StatCard
          title="Estoque baixo"
          value={estoqueBaixo}
          description="Itens em atenção"
          icon={Package}
          tone={estoqueBaixo > 0 ? "danger" : "default"}
        />

        <StatCard
          title="Notas pendentes"
          value={notasPendentes}
          description="Fiscal com pendências"
          icon={FileText}
          tone={notasPendentes > 0 ? "warning" : "default"}
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <PageSection
          title="Visão rápida da operação"
          description="Os principais pontos para decidir o que precisa de atenção primeiro."
        >
          <div className="grid gap-3 md:grid-cols-2">
            <InfoItem>Ordens abertas: {ordensAbertas}</InfoItem>
            <InfoItem>Peças com estoque baixo: {estoqueBaixo}</InfoItem>
            <InfoItem>Clientes cadastrados: {clientes.data.length}</InfoItem>
            <InfoItem>Documentos fiscais pendentes: {notasPendentes}</InfoItem>
            <InfoItem>Total de peças cadastradas: {pecas.data.length}</InfoItem>
            <InfoItem>Total de vendas lançadas: {vendas.data.length}</InfoItem>
          </div>
        </PageSection>

        <PageSection
          title="Checklist fiscal"
          description="Pontos críticos antes da emissão real."
        >
          <div className="space-y-3">
            <InfoItem>Configuração fiscal em homologação validada</InfoItem>
            <InfoItem>Regras NCM, CFOP, CST e CSOSN por produto</InfoItem>
            <InfoItem>Clientes com CPF/CNPJ e endereço fiscal</InfoItem>
            <InfoItem>Venda finalizada antes da emissão NF-e ou NFC-e</InfoItem>
            <InfoItem>Cancelamento com motivo obrigatório</InfoItem>
          </div>
        </PageSection>
      </div>

      <PageSection
        title="Alertas operacionais"
        description="Use este bloco como prioridade do dia."
      >
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <StatCard
            title="Peças críticas"
            value={estoqueBaixo}
            description="Ação imediata"
            icon={AlertTriangle}
            tone={estoqueBaixo > 0 ? "danger" : "default"}
          />

          <StatCard
            title="Ordens em andamento"
            value={ordensEmAndamento}
            description="Execução do time"
            icon={Wrench}
            tone="warning"
          />

          <StatCard
            title="Notas emitidas"
            value={documentos.data.length}
            description="Total fiscal"
            icon={ClipboardList}
          />

          <StatCard
            title="Vendas lançadas"
            value={vendas.data.length}
            description="Movimento comercial"
            icon={Banknote}
            tone="success"
          />

          <StatCard
            title="Base de clientes"
            value={clientes.data.length}
            description="Relacionamento ativo"
            icon={CircleAlert}
          />
        </div>
      </PageSection>
    </div>
  );
}
