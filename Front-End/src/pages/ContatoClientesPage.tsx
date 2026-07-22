import { useMemo, useState } from "react";
import { Cake, CalendarDays, MessageCircle, RefreshCw, UserX } from "lucide-react";

import { DataTable, Notice } from "../components/Ui";
import type { ColumnConfig } from "../components/Ui";
import { PageHeader } from "../components/app/PageHeader";
import { PageSection } from "../components/app/PageSection";
import { StatCard } from "../components/app/StartCard";
import { useList } from "../hooks/useApi";
import type { ApiRecord } from "../lib/api";
import { formatDate } from "../components/uiHelpers";

type Tab = "hoje" | "mes" | "inativos";

const tabOptions: { value: Tab; label: string }[] = [
  { value: "hoje", label: "Aniversariantes de hoje" },
  { value: "mes", label: "Aniversariantes do mês" },
  { value: "inativos", label: "Sem contato recente" },
];

const mensagemPadraoPorTab: Record<Tab, string> = {
  hoje: "Olá! A equipe da loja deseja um feliz aniversário! 🎉",
  mes: "Olá! Passando pra desejar um feliz aniversário nesse mês especial! 🎉",
  inativos:
    "Olá! Faz um tempo que você não aparece por aqui. Precisa de alguma manutenção no seu aparelho?",
};

function buttonClass(variant: "primary" | "secondary" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60";
}

function whatsappLink(telefone: unknown, mensagem: string) {
  const digits = String(telefone ?? "").replace(/\D/g, "");
  if (!digits) return null;

  const numero = digits.length <= 11 ? `55${digits}` : digits;
  return `https://wa.me/${numero}?text=${encodeURIComponent(mensagem)}`;
}

export function ContatoClientesPage() {
  const [tab, setTab] = useState<Tab>("hoje");
  const [reload, setReload] = useState(0);
  const [mensagem, setMensagem] = useState(mensagemPadraoPorTab.hoje);
  const [failure, setFailure] = useState("");
  const [historicoClienteId, setHistoricoClienteId] = useState("");
  const [historicoNome, setHistoricoNome] = useState("");

  const aniversariantes = useList("/gestao/aniversariantes", reload);
  const inativos = useList("/gestao/clientes-inativos?mesesMin=6&mesesMax=12", reload);
  const ordens = useList(historicoClienteId ? "/ordens-servico" : "", 0);

  const hoje = new Date();
  const diaHoje = hoje.getDate();
  const mesHoje = hoje.getMonth() + 1;

  const aniversariantesHoje = useMemo(
    () => aniversariantes.data.filter((c) => Number(c.dia) === diaHoje && Number(c.mes) === mesHoje),
    [aniversariantes.data, diaHoje, mesHoje],
  );

  const ordensDoCliente = useMemo(
    () => ordens.data.filter((os) => String(os.clienteId ?? "") === historicoClienteId),
    [ordens.data, historicoClienteId],
  );

  function selecionarTab(value: Tab) {
    setTab(value);
    setMensagem(mensagemPadraoPorTab[value]);
    setFailure("");
  }

  function abrirWhatsapp(row: ApiRecord) {
    const link = whatsappLink(row.telefone, mensagem);
    if (!link) {
      setFailure(`${String(row.nome ?? "Este cliente")} não tem telefone cadastrado.`);
      return;
    }

    setFailure("");
    window.open(link, "_blank", "noopener,noreferrer");
  }

  function abrirHistorico(row: ApiRecord) {
    setHistoricoClienteId(String(row.clienteId ?? ""));
    setHistoricoNome(String(row.nome ?? ""));
  }

  const dadosAtuais = tab === "hoje" ? aniversariantesHoje : tab === "mes" ? aniversariantes.data : inativos.data;
  const carregando = tab === "inativos" ? inativos.loading : aniversariantes.loading;

  const colunasAniversariante: ColumnConfig[] = [
    { key: "nome", label: "Nome" },
    { key: "telefone", label: "Telefone" },
    { key: "email", label: "E-mail", render: (row) => String(row.email ?? "-") },
    { key: "dia", label: "Dia", render: (row) => String(row.dia ?? "-") },
  ];

  const colunasInativos: ColumnConfig[] = [
    { key: "nome", label: "Nome" },
    { key: "telefone", label: "Telefone" },
    { key: "email", label: "E-mail", render: (row) => String(row.email ?? "-") },
    { key: "ultimaVisita", label: "Última visita", render: (row) => formatDate(row.ultimaVisita) },
    {
      key: "diasSemContato",
      label: "Dias sem contato",
      render: (row) => `${String(row.diasSemContato ?? "-")} dias`,
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Relacionamento"
        title="Contato com clientes"
        description="Aniversariantes e clientes que sumiram - use antes que eles esqueçam da loja."
        actions={
          <button type="button" className={buttonClass()} onClick={() => setReload((key) => key + 1)}>
            <RefreshCw size={16} />
            Atualizar
          </button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard
          title="Aniversariantes hoje"
          value={aniversariantesHoje.length}
          description="Ligue ou mande mensagem"
          icon={Cake}
          tone={aniversariantesHoje.length > 0 ? "success" : "default"}
        />
        <StatCard
          title="Aniversariantes no mês"
          value={aniversariantes.data.length}
          description="Todo o mês atual"
          icon={CalendarDays}
        />
        <StatCard
          title="Sem contato recente"
          value={inativos.data.length}
          description="Entre 6 e 12 meses sem voltar"
          icon={UserX}
          tone={inativos.data.length > 0 ? "warning" : "default"}
        />
      </div>

      {failure ? <Notice type="error">{failure}</Notice> : null}

      <div className="flex flex-wrap gap-2">
        {tabOptions.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => selecionarTab(option.value)}
            className={[
              "inline-flex items-center justify-center rounded-2xl border px-4 py-2.5 text-sm font-medium transition",
              tab === option.value
                ? "border-slate-900 bg-slate-900 text-white"
                : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50",
            ].join(" ")}
          >
            {option.label}
          </button>
        ))}
      </div>

      <PageSection
        title="Mensagem padrão"
        description="Editável antes de abrir o WhatsApp de cada cliente da lista abaixo."
      >
        <textarea
          className="min-h-[80px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
          value={mensagem}
          maxLength={500}
          onChange={(event) => setMensagem(event.target.value)}
        />
      </PageSection>

      <PageSection
        title={tabOptions.find((option) => option.value === tab)?.label ?? ""}
        description={
          tab === "inativos"
            ? "Clientes cuja última OS ou venda foi há mais de 6 meses (e menos de 12) - risco de terem ido pra concorrência."
            : "Clientes com data de aniversário cadastrada."
        }
      >
        <DataTable
          columns={tab === "inativos" ? colunasInativos : colunasAniversariante}
          rows={dadosAtuais}
          loading={carregando}
          emptyText={
            tab === "hoje"
              ? "Nenhum aniversariante hoje."
              : tab === "mes"
                ? "Nenhum aniversariante neste mês."
                : "Nenhum cliente inativo nesse período."
          }
          actions={(row) => (
            <div className="flex items-center gap-2">
              <button
                type="button"
                className="inline-flex items-center gap-1 text-emerald-700 hover:text-emerald-900"
                onClick={() => abrirWhatsapp(row)}
              >
                <MessageCircle size={14} />
                WhatsApp
              </button>
              {tab === "inativos" ? (
                <button
                  type="button"
                  className="text-slate-600 hover:text-slate-900"
                  onClick={() => abrirHistorico(row)}
                >
                  Ver OS's
                </button>
              ) : null}
            </div>
          )}
        />
      </PageSection>

      {historicoClienteId ? (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/80 p-4 backdrop-blur-sm">
          <div className="w-full max-w-2xl rounded-[28px] border border-slate-200 bg-white shadow-2xl">
            <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
              <div>
                <h3 className="text-lg font-bold tracking-tight text-slate-900">
                  Histórico de {historicoNome}
                </h3>
                <p className="mt-1 text-sm text-slate-500">Ordens de serviço que geraram este alerta.</p>
              </div>
              <button
                type="button"
                className={buttonClass()}
                onClick={() => setHistoricoClienteId("")}
              >
                Fechar
              </button>
            </div>

            <div className="max-h-[60vh] overflow-y-auto px-6 py-5">
              <DataTable
                columns={[
                  { key: "numeroOs", label: "OS" },
                  { key: "status", label: "Status" },
                  { key: "dataEntrada", label: "Entrada", render: (row) => formatDate(row.dataEntrada) },
                  { key: "dataEntrega", label: "Entrega", render: (row) => formatDate(row.dataEntrega) },
                  { key: "defeitoRelatado", label: "Defeito" },
                ]}
                rows={ordensDoCliente}
                loading={ordens.loading}
                emptyText="Nenhuma OS encontrada para este cliente."
              />
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
