import { useMemo, useState } from "react";
import type { ReactNode } from "react";
import {
  BarChart3,
  CalendarRange,
  ChevronDown,
  ChevronUp,
  Download,
  FileText,
  Filter,
  Package,
  RotateCcw,
  Users,
  Wallet,
  Wrench,
} from "lucide-react";

import { DataTable, Notice, PageFrame } from "../components/Ui";
import type { ApiRecord } from "../lib/api";
import { formatCurrency, formatDate } from "../components/uiHelpers";
import { useList } from "../hooks/useApi";

type ReportType =
  | "geral"
  | "vendas"
  | "ordens"
  | "financeiro"
  | "estoque"
  | "fiscal"
  | "clientes";

const reportOptions: { value: ReportType; label: string }[] = [
  { value: "geral", label: "Geral" },
  { value: "vendas", label: "Vendas" },
  { value: "ordens", label: "Ordens de serviço" },
  { value: "financeiro", label: "Financeiro" },
  { value: "estoque", label: "Peças e estoque" },
  { value: "fiscal", label: "Notas fiscais" },
  { value: "clientes", label: "Clientes" },
];

const statusOptions = [
  "ABERTA",
  "APROVADA",
  "EM_EXECUCAO",
  "PRONTA",
  "ENTREGUE",
  "FINALIZADA",
  "CANCELADA",
  "PENDENTE",
  "PAGO",
  "EMITIDA",
  "REJEITADA",
];

const pagamentoOptions = [
  "DINHEIRO",
  "PIX",
  "CARTAO_CREDITO",
  "CARTAO_DEBITO",
  "BOLETO",
];

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60";

function text(value: unknown) {
  return String(value ?? "").toLowerCase();
}

function money(value: unknown) {
  const amount = Number(value ?? 0);
  return Number.isFinite(amount) ? amount : 0;
}

function onlyDate(value: unknown) {
  const raw = String(value ?? "");
  return raw ? raw.slice(0, 10) : "";
}

function inDateRange(row: ApiRecord, keys: string[], start: string, end: string) {
  if (!start && !end) return true;
  const value = keys.map((key) => onlyDate(row[key])).find(Boolean);
  if (!value) return false;
  if (start && value < start) return false;
  if (end && value > end) return false;
  return true;
}

function valueInRange(value: unknown, min: string, max: string) {
  const total = money(value);
  if (min && total < Number(min)) return false;
  if (max && total > Number(max)) return false;
  return true;
}

function includesAny(row: ApiRecord, keys: string[], query: string) {
  if (!query.trim()) return true;
  const needle = query.trim().toLowerCase();
  return keys.some((key) => text(row[key]).includes(needle));
}

function sum(rows: ApiRecord[], key: string) {
  return rows.reduce((total, row) => total + money(row[key]), 0);
}

function average(rows: ApiRecord[], key: string) {
  return rows.length ? sum(rows, key) / rows.length : 0;
}

function percent(part: number, total: number) {
  return total ? `${Math.round((part / total) * 100)}%` : "0%";
}

function csvCell(value: unknown) {
  const raw = String(value ?? "").replace(/"/g, '""');
  return `"${raw}"`;
}

function downloadCsv(
  filename: string,
  headers: string[],
  rows: ApiRecord[],
  values: ((row: ApiRecord) => unknown)[],
) {
  const content = [
    headers.map(csvCell).join(";"),
    ...rows.map((row) => values.map((getValue) => csvCell(getValue(row))).join(";")),
  ].join("\n");

  const blob = new Blob([`\uFEFF${content}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

function MetricCard({
  icon,
  title,
  value,
  helper,
}: {
  icon: ReactNode;
  title: string;
  value: string;
  helper: string;
}) {
  return (
    <article className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
          {icon}
        </div>

        <div className="min-w-0">
          <span className="block text-sm text-slate-500">{title}</span>
          <strong className="mt-1 block text-2xl font-bold tracking-tight text-slate-900">
            {value}
          </strong>
          <small className="mt-1 block text-xs text-slate-400">{helper}</small>
        </div>
      </div>
    </article>
  );
}

function QuickInfo({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
      <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
        {label}
      </span>
      <strong className="mt-1 block text-base text-slate-900">{value}</strong>
    </article>
  );
}

function FilterField({
  label,
  children,
  span = "normal",
}: {
  label: string;
  children: ReactNode;
  span?: "normal" | "full";
}) {
  return (
    <label className={span === "full" ? "md:col-span-2 xl:col-span-4" : ""}>
      <span className="mb-2 block text-sm font-medium text-slate-700">{label}</span>
      {children}
    </label>
  );
}

function ReportSection({
  title,
  description,
  onExport,
  children,
  headerRight,
}: {
  title: string;
  description: string;
  onExport: () => void;
  children: ReactNode;
  headerRight?: ReactNode;
}) {
  return (
    <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-5 flex flex-col gap-4 border-b border-slate-100 pb-5 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight text-slate-900">{title}</h2>
          <p className="mt-1 text-sm text-slate-500">{description}</p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {headerRight}
          <button
            className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            type="button"
            onClick={onExport}
          >
            <Download size={16} />
            Exportar CSV
          </button>
        </div>
      </div>

      {children}
    </section>
  );
}

export function RelatoriosPage() {
  const clientes = useList("/clientes");
  const aparelhos = useList("/aparelhos");
  const tecnicos = useList("/tecnicos");
  const pecas = useList("/pecas");
  const vendas = useList("/vendas");
  const ordens = useList("/ordens-servico");
  const receber = useList("/contas-receber");
  const pagar = useList("/contas-pagar");
  const documentos = useList("/documentos-fiscais");

  const [reportType, setReportType] = useState<ReportType>("geral");
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);

  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [clienteId, setClienteId] = useState("");
  const [tipoCliente, setTipoCliente] = useState("");
  const [tecnicoId, setTecnicoId] = useState("");
  const [status, setStatus] = useState("");
  const [pagamento, setPagamento] = useState("");
  const [valorMin, setValorMin] = useState("");
  const [valorMax, setValorMax] = useState("");
  const [aparelhoBusca, setAparelhoBusca] = useState("");
  const [pecaBusca, setPecaBusca] = useState("");
  const [clienteBusca, setClienteBusca] = useState("");
  const [documentoTipo, setDocumentoTipo] = useState("");

  const rangeQuery = new URLSearchParams();
  if (startDate) rangeQuery.set("inicio", startDate);
  if (endDate) rangeQuery.set("fim", endDate);
  const query = rangeQuery.toString();

  const dre = useList(`/gestao/dre${query ? `?${query}` : ""}`);
  const comissoes = useList(`/gestao/comissoes${query ? `?${query}` : ""}`);
  const auditoria = useList(`/gestao/auditoria-financeira${query ? `?${query}` : ""}`);

  const clienteMap = useMemo(
    () => new Map(clientes.data.map((cliente) => [String(cliente.id ?? ""), cliente])),
    [clientes.data],
  );

  const tecnicoMap = useMemo(
    () => new Map(tecnicos.data.map((tecnico) => [String(tecnico.id ?? ""), tecnico])),
    [tecnicos.data],
  );

  const clienteNomeSelecionado = clienteId
    ? String(clienteMap.get(clienteId)?.nome ?? "")
    : "";

  const tecnicoNomeSelecionado = tecnicoId
    ? String(tecnicoMap.get(tecnicoId)?.nome ?? "")
    : "";

  const filteredClientes = useMemo(
    () =>
      clientes.data.filter((cliente) => {
        if (tipoCliente && String(cliente.tipoPessoa ?? "") !== tipoCliente) return false;

        if (
          !includesAny(
            cliente,
            ["nome", "cpfCnpj", "telefone", "email", "cidade", "uf"],
            clienteBusca,
          )
        ) {
          return false;
        }

        return true;
      }),
    [clientes.data, clienteBusca, tipoCliente],
  );

  const filteredPecas = useMemo(
    () =>
      pecas.data.filter((peca) => {
        if (
          !includesAny(
            peca,
            ["nome", "sku", "codigoInterno", "categoria", "marca", "modeloCompativel", "ncm"],
            pecaBusca,
          )
        ) {
          return false;
        }

        if (!valueInRange(peca.precoVenda, valorMin, valorMax)) return false;
        return true;
      }),
    [pecaBusca, pecas.data, valorMax, valorMin],
  );

  const filteredAparelhos = useMemo(
    () =>
      aparelhos.data.filter((aparelho) => {
        if (clienteId && String(aparelho.clienteId ?? "") !== clienteId) return false;

        if (
          !includesAny(
            aparelho,
            ["marca", "modelo", "imei", "serialNumber", "cor", "clienteNome"],
            aparelhoBusca,
          )
        ) {
          return false;
        }

        return true;
      }),
    [aparelhoBusca, aparelhos.data, clienteId],
  );

  const filteredVendas = useMemo(
    () =>
      vendas.data.filter((venda) => {
        if (!inDateRange(venda, ["dataVenda", "createdAt"], startDate, endDate)) return false;

        if (
          clienteId &&
          String(venda.clienteId ?? "") !== clienteId &&
          text(venda.clienteNome) !== text(clienteNomeSelecionado)
        ) {
          return false;
        }

        if (tipoCliente) {
          const cliente = clienteMap.get(String(venda.clienteId ?? ""));
          if (cliente && String(cliente.tipoPessoa ?? "") !== tipoCliente) return false;
        }

        if (status && String(venda.status ?? "") !== status) return false;
        if (pagamento && String(venda.formaPagamento ?? "") !== pagamento) return false;
        if (!valueInRange(venda.valorTotal, valorMin, valorMax)) return false;

        return true;
      }),
    [
      clienteId,
      clienteMap,
      clienteNomeSelecionado,
      endDate,
      pagamento,
      startDate,
      status,
      tipoCliente,
      valorMax,
      valorMin,
      vendas.data,
    ],
  );

  const filteredOrdens = useMemo(
    () =>
      ordens.data.filter((ordem) => {
        if (
          !inDateRange(ordem, ["dataEntrada", "dataPrevisao", "createdAt"], startDate, endDate)
        ) {
          return false;
        }

        if (
          clienteId &&
          String(ordem.clienteId ?? "") !== clienteId &&
          text(ordem.clienteNome) !== text(clienteNomeSelecionado)
        ) {
          return false;
        }

        if (
          tecnicoId &&
          String(ordem.tecnicoId ?? "") !== tecnicoId &&
          text(ordem.tecnicoNome) !== text(tecnicoNomeSelecionado)
        ) {
          return false;
        }

        if (status && String(ordem.status ?? "") !== status) return false;
        if (!includesAny(ordem, ["aparelhoDescricao", "marca", "modelo", "imei"], aparelhoBusca)) {
          return false;
        }
        if (!valueInRange(ordem.valorTotal, valorMin, valorMax)) return false;

        return true;
      }),
    [
      aparelhoBusca,
      clienteId,
      clienteNomeSelecionado,
      endDate,
      ordens.data,
      startDate,
      status,
      tecnicoId,
      tecnicoNomeSelecionado,
      valorMax,
      valorMin,
    ],
  );

  const filteredReceber = useMemo(
    () =>
      receber.data.filter((conta) => {
        if (
          !inDateRange(conta, ["dataVencimento", "dataEmissao", "createdAt"], startDate, endDate)
        ) {
          return false;
        }

        if (
          clienteId &&
          String(conta.clienteId ?? "") !== clienteId &&
          text(conta.clienteNome) !== text(clienteNomeSelecionado)
        ) {
          return false;
        }

        if (status && String(conta.status ?? "") !== status) return false;
        if (pagamento && String(conta.formaPagamento ?? "") !== pagamento) return false;
        if (!valueInRange(conta.valor, valorMin, valorMax)) return false;

        return true;
      }),
    [
      clienteId,
      clienteNomeSelecionado,
      endDate,
      pagamento,
      receber.data,
      startDate,
      status,
      valorMax,
      valorMin,
    ],
  );

  const filteredPagar = useMemo(
    () =>
      pagar.data.filter((conta) => {
        if (
          !inDateRange(conta, ["dataVencimento", "dataEmissao", "createdAt"], startDate, endDate)
        ) {
          return false;
        }

        if (status && String(conta.status ?? "") !== status) return false;
        if (!valueInRange(conta.valor, valorMin, valorMax)) return false;

        return true;
      }),
    [endDate, pagar.data, startDate, status, valorMax, valorMin],
  );

  const filteredDocumentos = useMemo(
    () =>
      documentos.data.filter((documento) => {
        if (!inDateRange(documento, ["dataEmissao", "createdAt"], startDate, endDate)) {
          return false;
        }

        if (
          documentoTipo &&
          String(documento.tipo ?? documento.tipoDocumentoFiscal ?? "") !== documentoTipo
        ) {
          return false;
        }

        if (status && String(documento.status ?? "") !== status) return false;
        if (!valueInRange(documento.valorTotal, valorMin, valorMax)) return false;

        return true;
      }),
    [documentoTipo, documentos.data, endDate, startDate, status, valorMax, valorMin],
  );

  const estoqueBaixo = filteredPecas.filter(
    (peca) => money(peca.estoqueAtual) <= money(peca.estoqueMinimo),
  );

  const hoje = new Date().toISOString().slice(0, 10);

  const contasVencidas = filteredReceber.filter(
    (conta) =>
      onlyDate(conta.dataVencimento) < hoje &&
      String(conta.status ?? "") !== "PAGO",
  );

  const erros = [
    clientes,
    aparelhos,
    tecnicos,
    pecas,
    vendas,
    ordens,
    receber,
    pagar,
    documentos,
    dre,
    comissoes,
    auditoria,
  ]
    .map((item) => item.error)
    .filter(Boolean);

  const loading = [
    clientes,
    aparelhos,
    tecnicos,
    pecas,
    vendas,
    ordens,
    receber,
    pagar,
    documentos,
  ].some((item) => item.loading);

  const financeiroSaldo =
    sum(filteredVendas, "valorTotal") +
    sum(filteredReceber, "valor") -
    sum(filteredPagar, "valor");

  const visibleSections = {
    vendas: reportType === "geral" || reportType === "vendas",
    ordens: reportType === "geral" || reportType === "ordens",
    financeiro: reportType === "geral" || reportType === "financeiro",
    estoque: reportType === "geral" || reportType === "estoque",
    fiscal: reportType === "geral" || reportType === "fiscal",
    clientes: reportType === "clientes",
  };

  const vendasRows = reportType === "geral" ? filteredVendas.slice(0, 8) : filteredVendas;
  const ordensRows = reportType === "geral" ? filteredOrdens.slice(0, 8) : filteredOrdens;
  const pecasRows = reportType === "geral" ? estoqueBaixo : filteredPecas;
  const documentosRows =
    reportType === "geral"
      ? filteredDocumentos.filter((doc) =>
          ["REJEITADA", "CANCELADA"].includes(String(doc.status ?? "")),
        )
      : filteredDocumentos;

  const showClienteFilter = ["geral", "vendas", "ordens", "financeiro", "clientes"].includes(
    reportType,
  );
  const showTecnicoFilter = ["geral", "ordens"].includes(reportType);
  const showPagamentoFilter = ["geral", "vendas", "financeiro"].includes(reportType);
  const showDocumentoFilter = ["geral", "fiscal"].includes(reportType);
  const showAparelhoFilter = ["geral", "ordens", "estoque"].includes(reportType);
  const showPecaFilter = ["geral", "estoque"].includes(reportType);
  const showClienteBusca = ["geral", "clientes"].includes(reportType);

  function clearFilters() {
    setStartDate("");
    setEndDate("");
    setClienteId("");
    setTipoCliente("");
    setTecnicoId("");
    setStatus("");
    setPagamento("");
    setValorMin("");
    setValorMax("");
    setAparelhoBusca("");
    setPecaBusca("");
    setClienteBusca("");
    setDocumentoTipo("");
  }

  return (
    <PageFrame
      eyebrow="Gestão"
      title="Relatórios"
      description="Relatórios mais limpos, com foco no que importa agora e filtros avançados só quando você precisar."
      actions={
        <div className="flex flex-wrap gap-2">
          <button
            className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            type="button"
            onClick={() => setShowAdvancedFilters((current) => !current)}
          >
            <Filter size={16} />
            {showAdvancedFilters ? "Ocultar filtros avançados" : "Mostrar filtros avançados"}
          </button>

          <button
            className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
            type="button"
            onClick={clearFilters}
          >
            <RotateCcw size={16} />
            Limpar filtros
          </button>
        </div>
      }
    >
      <div className="space-y-6">
        {erros.length ? <Notice type="error">{erros[0]}</Notice> : null}

        <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 flex flex-col gap-4 border-b border-slate-100 pb-5">
            <div className="flex items-start gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
                <BarChart3 size={20} />
              </div>

              <div>
                <h2 className="text-xl font-bold tracking-tight text-slate-900">
                  Tipo de relatório
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  Escolha uma visão mais específica para reduzir ruído visual.
                </p>
              </div>
            </div>

            <div className="flex flex-wrap gap-2">
              {reportOptions.map((option) => {
                const active = reportType === option.value;

                return (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => setReportType(option.value)}
                    className={[
                      "inline-flex items-center justify-center rounded-2xl border px-4 py-2.5 text-sm font-medium transition",
                      active
                        ? "border-slate-900 bg-slate-900 text-white"
                        : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50",
                    ].join(" ")}
                  >
                    {option.label}
                  </button>
                );
              })}
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <FilterField label="Data inicial">
              <div className="relative">
                <CalendarRange
                  size={16}
                  className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400"
                />
                <input
                  className={`${inputClass} pl-11`}
                  type="date"
                  value={startDate}
                  onChange={(event) => setStartDate(event.target.value)}
                />
              </div>
            </FilterField>

            <FilterField label="Data final">
              <div className="relative">
                <CalendarRange
                  size={16}
                  className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400"
                />
                <input
                  className={`${inputClass} pl-11`}
                  type="date"
                  value={endDate}
                  onChange={(event) => setEndDate(event.target.value)}
                />
              </div>
            </FilterField>

            <FilterField label="Status">
              <select
                className={inputClass}
                value={status}
                onChange={(event) => setStatus(event.target.value)}
              >
                <option value="">Todos</option>
                {statusOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </FilterField>

            <FilterField label="Faixa de valor">
              <div className="grid grid-cols-2 gap-3">
                <input
                  className={inputClass}
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="Mín."
                  value={valorMin}
                  onChange={(event) => setValorMin(event.target.value)}
                />
                <input
                  className={inputClass}
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="Máx."
                  value={valorMax}
                  onChange={(event) => setValorMax(event.target.value)}
                />
              </div>
            </FilterField>
          </div>

          <div className="mt-4 flex items-center justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
            <div>
              <strong className="block text-sm text-slate-900">Filtros avançados</strong>
              <span className="block text-xs text-slate-500">
                Mostre apenas os filtros relevantes ao tipo de relatório escolhido.
              </span>
            </div>

            <button
              type="button"
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
              onClick={() => setShowAdvancedFilters((current) => !current)}
            >
              {showAdvancedFilters ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
              {showAdvancedFilters ? "Recolher" : "Expandir"}
            </button>
          </div>

          {showAdvancedFilters ? (
            <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              {showClienteFilter ? (
                <FilterField label="Cliente">
                  <select
                    className={inputClass}
                    value={clienteId}
                    onChange={(event) => setClienteId(event.target.value)}
                  >
                    <option value="">Todos</option>
                    {clientes.data.map((cliente) => (
                      <option key={String(cliente.id)} value={String(cliente.id ?? "")}>
                        {String(cliente.nome ?? cliente.id ?? "")}
                      </option>
                    ))}
                  </select>
                </FilterField>
              ) : null}

              {showClienteFilter ? (
                <FilterField label="Tipo de cliente">
                  <select
                    className={inputClass}
                    value={tipoCliente}
                    onChange={(event) => setTipoCliente(event.target.value)}
                  >
                    <option value="">Todos</option>
                    <option value="FISICA">Pessoa física</option>
                    <option value="JURIDICA">Pessoa jurídica</option>
                  </select>
                </FilterField>
              ) : null}

              {showTecnicoFilter ? (
                <FilterField label="Técnico">
                  <select
                    className={inputClass}
                    value={tecnicoId}
                    onChange={(event) => setTecnicoId(event.target.value)}
                  >
                    <option value="">Todos</option>
                    {tecnicos.data.map((tecnico) => (
                      <option key={String(tecnico.id)} value={String(tecnico.id ?? "")}>
                        {String(tecnico.nome ?? tecnico.id ?? "")}
                      </option>
                    ))}
                  </select>
                </FilterField>
              ) : null}

              {showPagamentoFilter ? (
                <FilterField label="Pagamento">
                  <select
                    className={inputClass}
                    value={pagamento}
                    onChange={(event) => setPagamento(event.target.value)}
                  >
                    <option value="">Todos</option>
                    {pagamentoOptions.map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                </FilterField>
              ) : null}

              {showDocumentoFilter ? (
                <FilterField label="Tipo de documento">
                  <select
                    className={inputClass}
                    value={documentoTipo}
                    onChange={(event) => setDocumentoTipo(event.target.value)}
                  >
                    <option value="">Todos</option>
                    <option value="NFE">NF-e</option>
                    <option value="NFCE">NFC-e</option>
                    <option value="NFSE">NFS-e</option>
                  </select>
                </FilterField>
              ) : null}

              {showClienteBusca ? (
                <FilterField label="Busca de cliente">
                  <input
                    className={inputClass}
                    value={clienteBusca}
                    onChange={(event) => setClienteBusca(event.target.value)}
                    placeholder="Nome, CPF/CNPJ, telefone, cidade..."
                  />
                </FilterField>
              ) : null}

              {showAparelhoFilter ? (
                <FilterField label="Aparelho, IMEI ou modelo">
                  <input
                    className={inputClass}
                    value={aparelhoBusca}
                    onChange={(event) => setAparelhoBusca(event.target.value)}
                    placeholder="iPhone, Samsung, IMEI..."
                  />
                </FilterField>
              ) : null}

              {showPecaFilter ? (
                <FilterField label="Peça, SKU, NCM ou categoria">
                  <input
                    className={inputClass}
                    value={pecaBusca}
                    onChange={(event) => setPecaBusca(event.target.value)}
                    placeholder="Tela, bateria, SKU..."
                  />
                </FilterField>
              ) : null}
            </div>
          ) : null}
        </section>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <MetricCard
            icon={<Wallet size={20} />}
            title="Receita filtrada"
            value={formatCurrency(sum(filteredVendas, "valorTotal"))}
            helper="Total das vendas no filtro"
          />
          <MetricCard
            icon={<Wrench size={20} />}
            title="Ordens filtradas"
            value={String(filteredOrdens.length)}
            helper="Volume operacional"
          />
          <MetricCard
            icon={<Package size={20} />}
            title="Estoque baixo"
            value={String(estoqueBaixo.length)}
            helper="Itens no nível crítico"
          />
          <MetricCard
            icon={<FileText size={20} />}
            title="Saldo previsto"
            value={formatCurrency(financeiroSaldo)}
            helper="Vendas + receber - pagar"
          />
          <MetricCard
            icon={<Users size={20} />}
            title="Clientes filtrados"
            value={String(filteredClientes.length)}
            helper="Base no filtro atual"
          />
        </div>

        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          <QuickInfo
            label="Ticket médio de venda"
            value={formatCurrency(average(filteredVendas, "valorTotal"))}
          />
          <QuickInfo
            label="Ticket médio de OS"
            value={formatCurrency(average(filteredOrdens, "valorTotal"))}
          />
          <QuickInfo label="Contas vencidas" value={String(contasVencidas.length)} />
          <QuickInfo
            label="Notas rejeitadas/canceladas"
            value={String(
              filteredDocumentos.filter((doc) =>
                ["REJEITADA", "CANCELADA"].includes(String(doc.status ?? "")),
              ).length,
            )}
          />
          <QuickInfo
            label="Clientes PJ"
            value={percent(
              filteredClientes.filter(
                (cliente) => String(cliente.tipoPessoa ?? "") === "JURIDICA",
              ).length,
              filteredClientes.length,
            )}
          />
        </div>

        {visibleSections.vendas ? (
          <ReportSection
            title="Vendas"
            description={
              reportType === "geral"
                ? "Visão resumida das vendas filtradas."
                : "Conferência detalhada de faturamento, forma de pagamento e status."
            }
            onExport={() =>
              downloadCsv(
                "relatorio-vendas.csv",
                ["Venda", "Cliente", "Status", "Pagamento", "Total", "Data"],
                filteredVendas,
                [
                  (row) => row.numeroVenda,
                  (row) => row.clienteNome,
                  (row) => row.status,
                  (row) => row.formaPagamento,
                  (row) => row.valorTotal,
                  (row) => row.dataVenda,
                ],
              )
            }
            headerRight={
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-600">
                Mostrando <strong className="text-slate-900">{vendasRows.length}</strong>
                {reportType === "geral" ? ` de ${filteredVendas.length}` : ""}
              </div>
            }
          >
            <DataTable
              columns={[
                { key: "numeroVenda", label: "Venda" },
                { key: "clienteNome", label: "Cliente" },
                { key: "status", label: "Status" },
                { key: "formaPagamento", label: "Pagamento" },
                {
                  key: "valorTotal",
                  label: "Total",
                  render: (row) => formatCurrency(row.valorTotal),
                },
                {
                  key: "dataVenda",
                  label: "Data",
                  render: (row) => formatDate(row.dataVenda),
                },
              ]}
              rows={vendasRows}
              loading={loading}
              emptyText="Nenhuma venda encontrada com estes filtros."
            />
          </ReportSection>
        ) : null}

        {visibleSections.ordens ? (
          <ReportSection
            title="Ordens de serviço"
            description={
              reportType === "geral"
                ? "Resumo operacional das ordens no período."
                : "Acompanhe técnico, aparelho, status e valores das OS."
            }
            onExport={() =>
              downloadCsv(
                "relatorio-ordens.csv",
                ["OS", "Cliente", "Aparelho", "Técnico", "Status", "Total", "Entrada"],
                filteredOrdens,
                [
                  (row) => row.numeroOs,
                  (row) => row.clienteNome,
                  (row) => row.aparelhoDescricao,
                  (row) => row.tecnicoNome,
                  (row) => row.status,
                  (row) => row.valorTotal,
                  (row) => row.dataEntrada,
                ],
              )
            }
            headerRight={
              <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-600">
                Mostrando <strong className="text-slate-900">{ordensRows.length}</strong>
                {reportType === "geral" ? ` de ${filteredOrdens.length}` : ""}
              </div>
            }
          >
            <DataTable
              columns={[
                { key: "numeroOs", label: "OS" },
                { key: "clienteNome", label: "Cliente" },
                { key: "aparelhoDescricao", label: "Aparelho" },
                { key: "tecnicoNome", label: "Técnico" },
                { key: "status", label: "Status" },
                {
                  key: "valorTotal",
                  label: "Total",
                  render: (row) => formatCurrency(row.valorTotal),
                },
                {
                  key: "dataEntrada",
                  label: "Entrada",
                  render: (row) => formatDate(row.dataEntrada),
                },
              ]}
              rows={ordensRows}
              loading={loading}
              emptyText="Nenhuma OS encontrada com estes filtros."
            />
          </ReportSection>
        ) : null}

        {visibleSections.financeiro ? (
          <ReportSection
            title="Financeiro"
            description="Compare recebimentos, despesas e saldo previsto dentro do período."
            onExport={() =>
              downloadCsv(
                "relatorio-financeiro.csv",
                ["Tipo", "Descrição", "Pessoa", "Status", "Valor", "Vencimento"],
                [
                  ...filteredReceber.map((row) => ({ ...row, tipoRelatorio: "Receber" })),
                  ...filteredPagar.map((row) => ({ ...row, tipoRelatorio: "Pagar" })),
                ],
                [
                  (row) => row.tipoRelatorio,
                  (row) => row.descricao,
                  (row) => row.clienteNome ?? row.fornecedor,
                  (row) => row.status,
                  (row) => row.valor,
                  (row) => row.dataVencimento,
                ],
              )
            }
          >
            <div className="mb-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <QuickInfo
                label="A receber"
                value={formatCurrency(sum(filteredReceber, "valor"))}
              />
              <QuickInfo
                label="A pagar"
                value={formatCurrency(sum(filteredPagar, "valor"))}
              />
              <QuickInfo
                label="Saldo previsto"
                value={formatCurrency(financeiroSaldo)}
              />
              <QuickInfo
                label="Contas vencidas"
                value={String(contasVencidas.length)}
              />
            </div>

            <div className="grid gap-6 xl:grid-cols-2">
              <DataTable
                columns={[
                  { key: "descricao", label: "Receber" },
                  { key: "clienteNome", label: "Cliente" },
                  { key: "status", label: "Status" },
                  {
                    key: "valor",
                    label: "Valor",
                    render: (row) => formatCurrency(row.valor),
                  },
                  {
                    key: "dataVencimento",
                    label: "Vencimento",
                    render: (row) => formatDate(row.dataVencimento),
                  },
                ]}
                rows={filteredReceber}
                loading={loading}
                emptyText="Nenhuma conta a receber encontrada."
              />

              <DataTable
                columns={[
                  { key: "descricao", label: "Pagar" },
                  { key: "fornecedor", label: "Fornecedor" },
                  { key: "status", label: "Status" },
                  {
                    key: "valor",
                    label: "Valor",
                    render: (row) => formatCurrency(row.valor),
                  },
                  {
                    key: "dataVencimento",
                    label: "Vencimento",
                    render: (row) => formatDate(row.dataVencimento),
                  },
                ]}
                rows={filteredPagar}
                loading={loading}
                emptyText="Nenhuma conta a pagar encontrada."
              />
            </div>
          </ReportSection>
        ) : null}

        {visibleSections.financeiro ? (
          <ReportSection
            title="DRE gerencial"
            description="Receita, custos, despesas, lucro e margem do período."
            onExport={() =>
              downloadCsv(
                "relatorio-dre.csv",
                ["Receita", "Custos", "Despesas", "Lucro", "Margem"],
                dre.data,
                [
                  (row) => row.receitaBruta ?? row.receita,
                  (row) => row.custoMercadoria ?? row.custos,
                  (row) => row.despesas,
                  (row) => row.lucroLiquido ?? row.lucro,
                  (row) => row.margemLiquida ?? row.margem,
                ],
              )
            }
          >
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
              <QuickInfo
                label="Receita"
                value={formatCurrency(dre.data[0]?.receitaBruta ?? dre.data[0]?.receita ?? 0)}
              />
              <QuickInfo
                label="Custos"
                value={formatCurrency(
                  dre.data[0]?.custoMercadoria ?? dre.data[0]?.custos ?? 0,
                )}
              />
              <QuickInfo
                label="Despesas"
                value={formatCurrency(dre.data[0]?.despesas ?? 0)}
              />
              <QuickInfo
                label="Lucro"
                value={formatCurrency(dre.data[0]?.lucroLiquido ?? dre.data[0]?.lucro ?? 0)}
              />
              <QuickInfo
                label="Margem"
                value={String(dre.data[0]?.margemLiquida ?? dre.data[0]?.margem ?? "0%")}
              />
            </div>
          </ReportSection>
        ) : null}

        {visibleSections.financeiro ? (
          <ReportSection
            title="Comissões"
            description="Valores calculados para acompanhamento ou repasse."
            onExport={() =>
              downloadCsv(
                "relatorio-comissoes.csv",
                ["Pessoa", "Base", "Percentual", "Valor"],
                comissoes.data,
                [
                  (row) => row.nome ?? row.tecnicoNome ?? row.usuarioNome,
                  (row) => row.baseCalculo,
                  (row) => row.percentual,
                  (row) => row.valorComissao ?? row.valor,
                ],
              )
            }
          >
            <DataTable
              columns={[
                { key: "nome", label: "Pessoa" },
                {
                  key: "baseCalculo",
                  label: "Base",
                  render: (row) => formatCurrency(row.baseCalculo),
                },
                { key: "percentual", label: "%" },
                {
                  key: "valorComissao",
                  label: "Comissão",
                  render: (row) => formatCurrency(row.valorComissao ?? row.valor),
                },
              ]}
              rows={comissoes.data}
              loading={comissoes.loading}
              emptyText="Nenhuma comissão encontrada."
            />
          </ReportSection>
        ) : null}

        {visibleSections.financeiro ? (
          <ReportSection
            title="Auditoria financeira"
            description="Eventos e conferências das movimentações financeiras."
            onExport={() =>
              downloadCsv(
                "relatorio-auditoria-financeira.csv",
                ["Data", "Tipo", "Origem", "Descrição", "Valor"],
                auditoria.data,
                [
                  (row) => row.createdAt ?? row.data,
                  (row) => row.tipo,
                  (row) => row.origemTipo ?? row.origem,
                  (row) => row.descricao,
                  (row) => row.valor,
                ],
              )
            }
          >
            <DataTable
              columns={[
                {
                  key: "tipo",
                  label: "Tipo",
                  render: (row) => String(row.tipo ?? row.tipoDocumentoFiscal ?? ""),
                },
                { key: "origemTipo", label: "Origem" },
                { key: "descricao", label: "Descrição" },
                {
                  key: "valor",
                  label: "Valor",
                  render: (row) => formatCurrency(row.valor),
                },
              ]}
              rows={auditoria.data}
              loading={auditoria.loading}
              emptyText="Nenhum evento de auditoria encontrado."
            />
          </ReportSection>
        ) : null}

        {visibleSections.estoque ? (
          <ReportSection
            title={reportType === "geral" ? "Estoque crítico" : "Peças e estoque"}
            description={
              reportType === "geral"
                ? "Itens que merecem atenção imediata."
                : "Acompanhe preço, SKU, estoque atual e produtos em nível crítico."
            }
            onExport={() =>
              downloadCsv(
                "relatorio-pecas.csv",
                ["Peça", "SKU", "Categoria", "Estoque", "Mínimo", "Preço"],
                filteredPecas,
                [
                  (row) => row.nome,
                  (row) => row.sku,
                  (row) => row.categoria,
                  (row) => row.estoqueAtual,
                  (row) => row.estoqueMinimo,
                  (row) => row.precoVenda,
                ],
              )
            }
          >
            <DataTable
              columns={[
                { key: "nome", label: "Peça" },
                { key: "sku", label: "SKU" },
                { key: "categoria", label: "Categoria" },
                { key: "estoqueAtual", label: "Estoque" },
                { key: "estoqueMinimo", label: "Mínimo" },
                {
                  key: "precoVenda",
                  label: "Preço",
                  render: (row) => formatCurrency(row.precoVenda),
                },
              ]}
              rows={pecasRows}
              loading={loading}
              emptyText="Nenhuma peça encontrada."
            />
          </ReportSection>
        ) : null}

        {reportType === "estoque" ? (
          <ReportSection
            title="Aparelhos cadastrados"
            description="Cruze cliente, marca, modelo e identificadores do aparelho."
            onExport={() =>
              downloadCsv(
                "relatorio-aparelhos.csv",
                ["Cliente", "Marca", "Modelo", "IMEI", "Serial"],
                filteredAparelhos,
                [
                  (row) => row.clienteNome,
                  (row) => row.marca,
                  (row) => row.modelo,
                  (row) => row.imei,
                  (row) => row.serialNumber,
                ],
              )
            }
          >
            <DataTable
              columns={[
                { key: "clienteNome", label: "Cliente" },
                { key: "marca", label: "Marca" },
                { key: "modelo", label: "Modelo" },
                { key: "imei", label: "IMEI" },
                { key: "serialNumber", label: "Serial" },
              ]}
              rows={filteredAparelhos}
              loading={loading}
              emptyText="Nenhum aparelho encontrado."
            />
          </ReportSection>
        ) : null}

        {visibleSections.fiscal ? (
          <ReportSection
            title={reportType === "geral" ? "Fiscal em atenção" : "Notas fiscais"}
            description={
              reportType === "geral"
                ? "Destaque para documentos com problema ou cancelados."
                : "Acompanhe emissão, status, tipo de documento e valores."
            }
            onExport={() =>
              downloadCsv(
                "relatorio-fiscal.csv",
                ["Número", "Tipo", "Status", "Valor", "Data"],
                filteredDocumentos,
                [
                  (row) => row.numero,
                  (row) => row.tipo ?? row.tipoDocumentoFiscal,
                  (row) => row.status,
                  (row) => row.valorTotal,
                  (row) => row.dataEmissao,
                ],
              )
            }
          >
            <DataTable
              columns={[
                { key: "numero", label: "Número" },
                {
                  key: "tipo",
                  label: "Tipo",
                  render: (row) => String(row.tipo ?? row.tipoDocumentoFiscal ?? ""),
                },
                { key: "status", label: "Status" },
                {
                  key: "valorTotal",
                  label: "Valor",
                  render: (row) => formatCurrency(row.valorTotal),
                },
                {
                  key: "dataEmissao",
                  label: "Emissão",
                  render: (row) => formatDate(row.dataEmissao),
                },
              ]}
              rows={documentosRows}
              loading={loading}
              emptyText="Nenhum documento fiscal encontrado."
            />
          </ReportSection>
        ) : null}

        {visibleSections.clientes ? (
          <ReportSection
            title="Clientes"
            description="Veja o perfil da base, documentos, contatos e localização."
            onExport={() =>
              downloadCsv(
                "relatorio-clientes.csv",
                ["Nome", "Tipo", "CPF/CNPJ", "Telefone", "E-mail", "Cidade", "UF"],
                filteredClientes,
                [
                  (row) => row.nome,
                  (row) => row.tipoPessoa,
                  (row) => row.cpfCnpj,
                  (row) => row.telefone,
                  (row) => row.email,
                  (row) => row.cidade,
                  (row) => row.uf,
                ],
              )
            }
          >
            <DataTable
              columns={[
                { key: "nome", label: "Nome" },
                { key: "tipoPessoa", label: "Tipo" },
                { key: "cpfCnpj", label: "CPF/CNPJ" },
                { key: "telefone", label: "Telefone" },
                { key: "email", label: "E-mail" },
                { key: "cidade", label: "Cidade" },
                { key: "uf", label: "UF" },
              ]}
              rows={filteredClientes}
              loading={loading}
              emptyText="Nenhum cliente encontrado."
            />
          </ReportSection>
        ) : null}
      </div>
    </PageFrame>
  );
}