import { useMemo, useState } from "react";
import {
  AlertTriangle,
  ArrowDownCircle,
  ArrowUpCircle,
  Lock,
  RefreshCw,
  Unlock,
  Wallet,
} from "lucide-react";

import { DataTable, Notice } from "../components/Ui";
import type { ColumnConfig, FieldConfig } from "../components/Ui";
import { PageHeader } from "../components/app/PageHeader";
import { PageSection } from "../components/app/PageSection";
import { StatCard } from "../components/app/StartCard";
import { useList } from "../hooks/useApi";
import { apiRequest } from "../lib/api";
import {
  errorMessage,
  formatCurrency,
  formatDate,
  formatFieldInput,
  parseMoney,
} from "../components/uiHelpers";

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 disabled:cursor-not-allowed disabled:bg-slate-100";

const textareaClass =
  "min-h-[90px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 resize-y";

const MONEY_FIELD: FieldConfig = { name: "valor", label: "Valor", type: "currency" };

function maskMoney(value: unknown) {
  return formatFieldInput(MONEY_FIELD, value);
}

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function toNumber(value: unknown) {
  const number = Number(value ?? 0);
  return Number.isFinite(number) ? number : 0;
}

function buttonClass(variant: "primary" | "secondary" | "danger" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";
  }

  if (variant === "danger") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100 disabled:cursor-not-allowed disabled:opacity-60";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60";
}

function statusBadgeClass(status: string) {
  return status === "ABERTO"
    ? "border-emerald-200 bg-emerald-50 text-emerald-700"
    : "border-slate-200 bg-slate-100 text-slate-700";
}

function tipoBadgeClass(tipo: string) {
  return tipo === "ENTRADA"
    ? "border-emerald-200 bg-emerald-50 text-emerald-700"
    : "border-rose-200 bg-rose-50 text-rose-700";
}

export function CaixaPage() {
  const [reload, setReload] = useState(0);
  const caixas = useList("/caixas-diarios", reload);

  const [valorAbertura, setValorAbertura] = useState(() => maskMoney("0"));
  const [observacoesAbertura, setObservacoesAbertura] = useState("");
  const [abrindo, setAbrindo] = useState(false);

  const [tipoLancamento, setTipoLancamento] = useState<"SAIDA" | "ENTRADA">("SAIDA");
  const [valorLancamento, setValorLancamento] = useState(() => maskMoney("0"));
  const [observacaoLancamento, setObservacaoLancamento] = useState("");
  const [lancando, setLancando] = useState(false);

  const [valorFechamentoInformado, setValorFechamentoInformado] = useState(() => maskMoney("0"));
  const [observacoesFechamento, setObservacoesFechamento] = useState("");
  const [fechando, setFechando] = useState(false);
  const [reabrindo, setReabrindo] = useState(false);

  const [caixaSelecionadoId, setCaixaSelecionadoId] = useState("");

  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");

  const caixaAberto = useMemo(
    () => caixas.data.find((c) => String(c.status ?? "") === "ABERTO"),
    [caixas.data],
  );

  const caixaEhDeHoje = Boolean(caixaAberto) && String(caixaAberto?.dataCaixa ?? "") === todayIso();

  const caixaFechadoDeHoje = useMemo(
    () =>
      caixas.data.find(
        (c) => String(c.dataCaixa ?? "") === todayIso() && String(c.status ?? "") === "FECHADO",
      ),
    [caixas.data],
  );

  const caixaVisualizadoId = caixaSelecionadoId || String(caixaAberto?.id ?? "");
  const caixaVisualizado = caixas.data.find((c) => String(c.id ?? "") === caixaVisualizadoId);

  const lancamentos = useList(
    caixaVisualizadoId ? `/caixa-lancamentos/caixa/${caixaVisualizadoId}` : "",
    reload,
  );

  const diferencaPreview = caixaAberto
    ? parseMoney(valorFechamentoInformado) - toNumber(caixaAberto.valorFechamentoSistema)
    : 0;

  function refresh() {
    setReload((key) => key + 1);
  }

  async function abrirCaixa() {
    setNotice("");
    setFailure("");
    setAbrindo(true);

    try {
      await apiRequest("/caixas-diarios", {
        method: "POST",
        body: {
          dataCaixa: todayIso(),
          valorAbertura: parseMoney(valorAbertura),
          observacoes: observacoesAbertura.trim() || null,
        },
      });

      setNotice("Caixa aberto com sucesso.");
      setValorAbertura(maskMoney("0"));
      setObservacoesAbertura("");
      setCaixaSelecionadoId("");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setAbrindo(false);
    }
  }

  async function reabrirCaixa(id: string) {
    setNotice("");
    setFailure("");
    setReabrindo(true);

    try {
      await apiRequest(`/caixas-diarios/${id}/reabrir`, { method: "PATCH" });

      setNotice("Caixa reaberto com sucesso.");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setReabrindo(false);
    }
  }

  async function registrarLancamento() {
    if (!caixaAberto) return;

    setNotice("");
    setFailure("");

    const valor = parseMoney(valorLancamento);
    if (valor <= 0) {
      setFailure("Informe um valor maior que zero.");
      return;
    }

    setLancando(true);

    try {
      await apiRequest("/caixa-lancamentos", {
        method: "POST",
        body: {
          caixaDiarioId: caixaAberto.id,
          tipo: tipoLancamento,
          origemTipo: tipoLancamento === "SAIDA" ? "SANGRIA" : "SUPRIMENTO",
          valor,
          observacao: observacaoLancamento.trim() || null,
        },
      });

      setNotice(
        tipoLancamento === "SAIDA" ? "Sangria registrada com sucesso." : "Suprimento registrado com sucesso.",
      );
      setValorLancamento(maskMoney("0"));
      setObservacaoLancamento("");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLancando(false);
    }
  }

  async function fecharCaixa() {
    if (!caixaAberto) return;

    if (
      !window.confirm(
        "Fechar o caixa é uma ação definitiva para o dia. Confira o valor contado antes de continuar. Deseja fechar mesmo assim?",
      )
    ) {
      return;
    }

    setNotice("");
    setFailure("");
    setFechando(true);

    try {
      await apiRequest(`/caixas-diarios/${caixaAberto.id}/fechar`, {
        method: "PATCH",
        body: {
          valorFechamentoInformado: parseMoney(valorFechamentoInformado),
          observacoes: observacoesFechamento.trim() || null,
        },
      });

      setNotice("Caixa fechado com sucesso.");
      setValorFechamentoInformado(maskMoney("0"));
      setObservacoesFechamento("");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setFechando(false);
    }
  }

  const historicoColumns: ColumnConfig[] = [
    { key: "dataCaixa", label: "Data", render: (row) => formatDate(row.dataCaixa) },
    { key: "valorAbertura", label: "Abertura", render: (row) => formatCurrency(row.valorAbertura) },
    {
      key: "valorFechamentoSistema",
      label: "Sistema",
      render: (row) => formatCurrency(row.valorFechamentoSistema),
    },
    {
      key: "valorFechamentoInformado",
      label: "Informado",
      render: (row) =>
        row.valorFechamentoInformado === null || row.valorFechamentoInformado === undefined
          ? "-"
          : formatCurrency(row.valorFechamentoInformado),
    },
    {
      key: "diferenca",
      label: "Diferença",
      render: (row) => {
        if (row.diferenca === null || row.diferenca === undefined) return "-";
        const valor = toNumber(row.diferenca);
        return (
          <span className={valor < 0 ? "text-rose-600" : valor > 0 ? "text-amber-600" : "text-emerald-600"}>
            {formatCurrency(valor)}
          </span>
        );
      },
    },
    {
      key: "status",
      label: "Status",
      render: (row) => (
        <span
          className={[
            "inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
            statusBadgeClass(String(row.status ?? "")),
          ].join(" ")}
        >
          {String(row.status ?? "-")}
        </span>
      ),
    },
  ];

  const lancamentosColumns: ColumnConfig[] = [
    {
      key: "tipo",
      label: "Tipo",
      render: (row) => (
        <span
          className={[
            "inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
            tipoBadgeClass(String(row.tipo ?? "")),
          ].join(" ")}
        >
          {String(row.tipo ?? "-")}
        </span>
      ),
    },
    { key: "origemTipo", label: "Origem", render: (row) => String(row.origemTipo ?? "-") },
    { key: "formaPagamento", label: "Forma", render: (row) => String(row.formaPagamento ?? "-") },
    { key: "valor", label: "Valor", render: (row) => formatCurrency(row.valor) },
    { key: "observacao", label: "Observação", render: (row) => String(row.observacao ?? "-") },
    { key: "createdAt", label: "Quando", render: (row) => formatDate(row.createdAt) },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Financeiro"
        title="Caixa"
        description="Abra o caixa do dia, registre sangrias e suprimentos, e feche conferindo o valor contado."
        actions={
          <button type="button" className={buttonClass()} onClick={refresh}>
            <RefreshCw size={16} />
            Atualizar
          </button>
        }
      />

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure || caixas.error ? <Notice type="error">{failure || caixas.error}</Notice> : null}

      {caixaAberto && !caixaEhDeHoje ? (
        <Notice type="error">
          O caixa em aberto é do dia {formatDate(caixaAberto.dataCaixa)}, não de hoje. Feche-o antes de
          abrir o caixa de hoje.
        </Notice>
      ) : null}

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard
          title="Status"
          value={caixaAberto ? "Aberto" : "Fechado"}
          description={
            caixaAberto ? `Desde ${formatDate(caixaAberto.dataAbertura)}` : "Nenhum caixa aberto agora"
          }
          icon={caixaAberto ? Unlock : Lock}
          tone={caixaAberto ? "success" : "warning"}
        />
        <StatCard
          title="Valor em caixa"
          value={formatCurrency(caixaAberto?.valorFechamentoSistema ?? 0)}
          description="Calculado pelo sistema"
          icon={Wallet}
        />
        <StatCard
          title="Caixas registrados"
          value={caixas.data.length}
          description="Histórico completo"
          icon={ArrowUpCircle}
        />
      </div>

      {!caixaAberto && caixaFechadoDeHoje ? (
        <PageSection
          title="Reabrir caixa de hoje"
          description="O caixa de hoje já foi fechado. Reabra-o para continuar registrando vendas e lançamentos no mesmo dia."
        >
          <div className="flex justify-end">
            <button
              type="button"
              className={buttonClass("primary")}
              disabled={reabrindo}
              onClick={() => void reabrirCaixa(String(caixaFechadoDeHoje.id ?? ""))}
            >
              <Unlock size={16} />
              {reabrindo ? "Reabrindo..." : "Reabrir caixa de hoje"}
            </button>
          </div>
        </PageSection>
      ) : null}

      {!caixaAberto && !caixaFechadoDeHoje ? (
        <PageSection title="Abrir caixa" description="Informe o valor inicial (troco) para começar o dia.">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Valor de abertura</span>
              <input
                className={inputClass}
                type="text"
                inputMode="numeric"
                value={valorAbertura}
                onChange={(event) => setValorAbertura(maskMoney(event.target.value))}
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Observações</span>
              <input
                className={inputClass}
                value={observacoesAbertura}
                maxLength={1000}
                onChange={(event) => setObservacoesAbertura(event.target.value)}
                placeholder="Opcional"
              />
            </label>
          </div>

          <div className="mt-5 flex justify-end">
            <button
              type="button"
              className={buttonClass("primary")}
              disabled={abrindo}
              onClick={() => void abrirCaixa()}
            >
              {abrindo ? "Abrindo..." : "Abrir caixa"}
            </button>
          </div>
        </PageSection>
      ) : (
        <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
          <PageSection
            title="Sangria e suprimento"
            description="Registre retiradas e reforços de troco no caixa aberto."
          >
            <div className="mb-4 flex gap-2">
              <button
                type="button"
                className={[
                  "inline-flex flex-1 items-center justify-center gap-2 rounded-2xl border px-4 py-2.5 text-sm font-semibold transition",
                  tipoLancamento === "SAIDA"
                    ? "border-rose-300 bg-rose-50 text-rose-700"
                    : "border-slate-200 bg-white text-slate-600 hover:bg-slate-50",
                ].join(" ")}
                onClick={() => setTipoLancamento("SAIDA")}
              >
                <ArrowDownCircle size={16} />
                Sangria
              </button>

              <button
                type="button"
                className={[
                  "inline-flex flex-1 items-center justify-center gap-2 rounded-2xl border px-4 py-2.5 text-sm font-semibold transition",
                  tipoLancamento === "ENTRADA"
                    ? "border-emerald-300 bg-emerald-50 text-emerald-700"
                    : "border-slate-200 bg-white text-slate-600 hover:bg-slate-50",
                ].join(" ")}
                onClick={() => setTipoLancamento("ENTRADA")}
              >
                <ArrowUpCircle size={16} />
                Suprimento
              </button>
            </div>

            <div className="grid gap-4">
              <label className="block">
                <span className="mb-2 block text-sm font-medium text-slate-700">Valor</span>
                <input
                  className={inputClass}
                  type="text"
                  inputMode="numeric"
                  value={valorLancamento}
                  onChange={(event) => setValorLancamento(maskMoney(event.target.value))}
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium text-slate-700">Observação</span>
                <textarea
                  className={textareaClass}
                  value={observacaoLancamento}
                  maxLength={1000}
                  onChange={(event) => setObservacaoLancamento(event.target.value)}
                  placeholder={
                    tipoLancamento === "SAIDA"
                      ? "Ex.: retirada para depósito bancário"
                      : "Ex.: reforço de troco"
                  }
                />
              </label>
            </div>

            <div className="mt-5 flex justify-end">
              <button
                type="button"
                className={buttonClass(tipoLancamento === "SAIDA" ? "danger" : "primary")}
                disabled={lancando}
                onClick={() => void registrarLancamento()}
              >
                {lancando ? "Registrando..." : tipoLancamento === "SAIDA" ? "Registrar sangria" : "Registrar suprimento"}
              </button>
            </div>

            <div className="mt-8 border-t border-slate-100 pt-6">
              <h3 className="text-sm font-semibold text-slate-900">Fechar caixa</h3>
              <p className="mt-1 text-sm text-slate-500">
                Conte o dinheiro físico e informe o valor apurado. A diferença é calculada automaticamente.
              </p>

              <div className="mt-4 grid gap-4 md:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Valor contado</span>
                  <input
                    className={inputClass}
                    type="text"
                    inputMode="numeric"
                    value={valorFechamentoInformado}
                    onChange={(event) => setValorFechamentoInformado(maskMoney(event.target.value))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Observações do fechamento</span>
                  <input
                    className={inputClass}
                    value={observacoesFechamento}
                    maxLength={1000}
                    onChange={(event) => setObservacoesFechamento(event.target.value)}
                    placeholder="Opcional"
                  />
                </label>
              </div>

              <div className="mt-3 flex items-center gap-2 text-sm">
                <span className="text-slate-500">Diferença prevista:</span>
                <strong
                  className={
                    diferencaPreview < 0
                      ? "text-rose-600"
                      : diferencaPreview > 0
                        ? "text-amber-600"
                        : "text-emerald-600"
                  }
                >
                  {formatCurrency(diferencaPreview)}
                </strong>
                {diferencaPreview !== 0 ? (
                  <span className="inline-flex items-center gap-1 text-xs text-slate-500">
                    <AlertTriangle size={14} />
                    {diferencaPreview < 0 ? "faltando" : "sobrando"}
                  </span>
                ) : null}
              </div>

              <div className="mt-4 flex justify-end">
                <button
                  type="button"
                  className={buttonClass("danger")}
                  disabled={fechando}
                  onClick={() => void fecharCaixa()}
                >
                  <Lock size={16} />
                  {fechando ? "Fechando..." : "Fechar caixa"}
                </button>
              </div>
            </div>
          </PageSection>

          <PageSection
            title={`Lançamentos ${caixaVisualizado ? `- ${formatDate(caixaVisualizado.dataCaixa)}` : ""}`}
            description="Entradas e saídas registradas neste caixa, incluindo vendas à vista."
          >
            <DataTable
              columns={lancamentosColumns}
              rows={lancamentos.data}
              loading={lancamentos.loading}
              emptyText="Nenhum lançamento neste caixa."
            />
          </PageSection>
        </div>
      )}

      <PageSection
        title="Histórico de caixas"
        description="Consulte a abertura, fechamento e diferença de outros dias."
      >
        <DataTable
          columns={historicoColumns}
          rows={caixas.data}
          loading={caixas.loading}
          emptyText="Nenhum caixa registrado ainda."
          actions={(row) => (
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
              onClick={() => setCaixaSelecionadoId(String(row.id ?? ""))}
            >
              Ver lançamentos
            </button>
          )}
        />
      </PageSection>
    </div>
  );
}
