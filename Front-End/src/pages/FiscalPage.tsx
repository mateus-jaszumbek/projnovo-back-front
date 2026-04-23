import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import {
  ClipboardList,
  FileText,
  Settings,
  Sparkles,
  Zap,
} from "lucide-react";

import { CrudPage } from "../components/CrudPage";
import { DataTable, FieldRenderer, Notice, PageFrame } from "../components/Ui";
import type { FieldConfig } from "../components/Ui";
import {
  defaultForm,
  errorMessage,
  formatCurrency,
  formatDate,
  formFromRecord,
  payloadFromForm,
  validateForm,
} from "../components/uiHelpers";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { useList, useOptions } from "../hooks/useApi";

const hoje = new Date().toISOString().slice(0, 10);

type FiscalTab = "config" | "regras" | "emissao" | "checklist";

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60";

function tabButtonClass(active: boolean) {
  return [
    "inline-flex items-center justify-center gap-2 rounded-2xl border px-4 py-2.5 text-sm font-medium transition",
    active
      ? "border-slate-900 bg-slate-900 text-white"
      : "border-slate-200 bg-white text-slate-700 hover:bg-slate-50",
  ].join(" ");
}

function panelTitle(title: string, description: string) {
  return (
    <div className="mb-5 border-b border-slate-100 pb-5">
      <h2 className="text-xl font-bold tracking-tight text-slate-900">{title}</h2>
      <p className="mt-1 text-sm text-slate-500">{description}</p>
    </div>
  );
}

export function FiscalPage() {
  const [tab, setTab] = useState<FiscalTab>("config");

  return (
    <PageFrame
      eyebrow="Fiscal"
      title="Notas e regras fiscais"
      description="Configure, valide regras e emita documentos fiscais fake de forma mais clara e organizada."
    >
      <div className="space-y-6">
        <Notice type="info">
          Emissão fake é usada para validar fluxo, numeração, regras e telas. Produção real exige certificado, credenciais e integração com SEFAZ/Prefeitura.
        </Notice>

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className={tabButtonClass(tab === "config")}
            onClick={() => setTab("config")}
          >
            <Settings size={16} />
            Configuração
          </button>

          <button
            type="button"
            className={tabButtonClass(tab === "regras")}
            onClick={() => setTab("regras")}
          >
            <ClipboardList size={16} />
            Regras fiscais
          </button>

          <button
            type="button"
            className={tabButtonClass(tab === "emissao")}
            onClick={() => setTab("emissao")}
          >
            <Zap size={16} />
            Emissão
          </button>

          <button
            type="button"
            className={tabButtonClass(tab === "checklist")}
            onClick={() => setTab("checklist")}
          >
            <FileText size={16} />
            Checklist
          </button>
        </div>

        <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          {tab === "config" ? <ConfiguracaoFiscalPanel /> : null}
          {tab === "regras" ? <RegrasFiscaisPanel /> : null}
          {tab === "emissao" ? <EmissaoFiscalPanel /> : null}
          {tab === "checklist" ? <ChecklistFiscalPanel /> : null}
        </section>
      </div>
    </PageFrame>
  );
}

function ChecklistFiscalPanel() {
  const itens = [
    "Credenciamento na SEFAZ/Prefeitura para cada tipo de nota",
    "Certificado digital A1/A3 válido e protegido",
    "CSC/token cadastrado para NFC-e",
    "Provedor fiscal real configurado antes de produção",
    "Regras por UF, NCM, CFOP, CST/CSOSN e origem",
    "Clientes com CPF/CNPJ e endereço fiscal quando exigido",
  ];

  return (
    <div className="space-y-6">
      {panelTitle(
        "Checklist para homologação e produção",
        "Antes de ativar produção real, valide tudo com contador, SEFAZ/Prefeitura e provedor fiscal.",
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {itens.map((item) => (
          <article
            key={item}
            className="rounded-2xl border border-slate-200 bg-slate-50 p-4"
          >
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-white text-slate-700">
                <Sparkles size={16} />
              </div>
              <strong className="text-sm font-medium leading-6 text-slate-800">{item}</strong>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}

function ConfiguracaoFiscalPanel() {
  const fields: FieldConfig[] = [
    {
      name: "ambiente",
      label: "Ambiente",
      type: "select",
      required: true,
      defaultValue: "Homologacao",
      options: [
        { value: "Homologacao", label: "Homologação" },
        { value: "Producao", label: "Produção" },
      ],
    },
    {
      name: "regimeTributario",
      label: "Regime tributário",
      type: "select",
      required: true,
      defaultValue: "SimplesNacional",
      options: [
        { value: "SimplesNacional", label: "Simples Nacional" },
        { value: "LucroPresumido", label: "Lucro Presumido" },
        { value: "LucroReal", label: "Lucro Real" },
      ],
    },
    {
      name: "serieNfce",
      label: "Série NFC-e",
      type: "number",
      min: 1,
      required: true,
      defaultValue: 1,
    },
    {
      name: "serieNfe",
      label: "Série NF-e",
      type: "number",
      min: 1,
      required: true,
      defaultValue: 1,
    },
    {
      name: "serieNfse",
      label: "Série NFS-e",
      type: "number",
      min: 1,
      required: true,
      defaultValue: 1,
    },
    {
      name: "proximoNumeroNfce",
      label: "Próx. NFC-e",
      type: "number",
      min: 1,
      required: true,
      defaultValue: 1,
    },
    {
      name: "proximoNumeroNfe",
      label: "Próx. NF-e",
      type: "number",
      min: 1,
      required: true,
      defaultValue: 1,
    },
    {
      name: "proximoNumeroNfse",
      label: "Próx. NFS-e",
      type: "number",
      min: 1,
      required: true,
      defaultValue: 1,
    },
    {
      name: "provedorFiscal",
      label: "Provedor fiscal",
      defaultValue: "Fake",
      maxLength: 100,
    },
    {
      name: "municipioCodigo",
      label: "Código do município",
      mask: "digits",
      maxLength: 7,
      helper: "Código IBGE com 7 dígitos.",
    },
    { name: "cnaePrincipal", label: "CNAE principal", mask: "digits", maxLength: 7 },
    { name: "itemListaServico", label: "Item da lista de serviço", maxLength: 20 },
    { name: "naturezaOperacaoPadrao", label: "Natureza da operação", maxLength: 200 },
    {
      name: "aliquotaIssPadrao",
      label: "ISS padrão (%)",
      type: "number",
      min: 0,
      max: 100,
      step: "0.01",
      nullable: true,
    },
    { name: "issRetidoPadrao", label: "ISS retido", type: "checkbox" },
    { name: "ativo", label: "Ativo", type: "checkbox", defaultValue: true },
  ];

  const [form, setForm] = useState<ApiRecord>(() => defaultForm(fields));
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const result = await apiRequest<ApiRecord>("/configuracao-fiscal");
        if (active) setForm(formFromRecord(fields, result));
      } catch {
        if (active) {
          setNotice("Salve a configuração fiscal inicial para começar em homologação.");
        }
      } finally {
        if (active) setLoading(false);
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, []);

  function update(name: string, value: unknown) {
    setForm((current) => ({ ...current, [name]: value }));
    setValidationErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
  }

  async function salvar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");
    setNotice("");

    const errors = validateForm(fields, form);
    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      setFailure("Corrija os campos fiscais antes de salvar.");
      return;
    }

    if (
      String(form.ambiente).toLowerCase() === "producao" &&
      (!form.provedorFiscal || String(form.provedorFiscal).toLowerCase() === "fake")
    ) {
      setValidationErrors({
        provedorFiscal: "Produção exige provedor fiscal real configurado.",
      });
      setFailure(
        "Use homologação enquanto o provedor fiscal real, certificado e credenciais não estiverem prontos.",
      );
      return;
    }

    try {
      const result = await apiRequest<ApiRecord>("/configuracao-fiscal", {
        method: "PUT",
        body: payloadFromForm(fields, form),
      });
      setForm(formFromRecord(fields, result));
      setValidationErrors({});
      setNotice("Configuração fiscal salva.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  return (
    <div className="space-y-5">
      {panelTitle(
        "Configuração fiscal",
        "Use homologação enquanto o certificado e o provedor real não estiverem prontos.",
      )}

      <form onSubmit={salvar} className="space-y-5">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {fields.map((field) => (
            <div
              key={field.name}
              className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}
            >
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
                <FieldRenderer
                  field={field}
                  value={form[field.name]}
                  error={validationErrors[field.name]}
                  onChange={update}
                />
              </div>
            </div>
          ))}
        </div>

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={loading}
            className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {loading ? "Carregando..." : "Salvar configuração"}
          </button>
        </div>
      </form>

      {notice ? <Notice type="info">{notice}</Notice> : null}
      {failure ? <Notice type="error">{failure}</Notice> : null}
    </div>
  );
}

function RegrasFiscaisPanel() {
  const fields: FieldConfig[] = [
    {
      name: "tipoDocumentoFiscal",
      label: "Tipo de nota",
      type: "select",
      required: true,
      defaultValue: "Nfce",
      options: [
        { value: "Nfce", label: "NFC-e" },
        { value: "Nfe", label: "NF-e" },
      ],
    },
    { name: "ufOrigem", label: "UF origem", placeholder: "SP", mask: "uf" },
    { name: "ufDestino", label: "UF destino", placeholder: "SP", mask: "uf" },
    {
      name: "regimeTributario",
      label: "Regime tributário",
      defaultValue: "SimplesNacional",
      maxLength: 50,
    },
    { name: "ncm", label: "NCM", mask: "ncm" },
    { name: "cfop", label: "CFOP", required: true, mask: "cfop" },
    { name: "cstCsosn", label: "CST/CSOSN", required: true, mask: "cstCsosn" },
    { name: "cest", label: "CEST", mask: "cest" },
    {
      name: "origemMercadoria",
      label: "Origem",
      required: true,
      defaultValue: "0",
      mask: "origem",
    },
    {
      name: "aliquotaIcms",
      label: "ICMS (%)",
      type: "number",
      min: 0,
      max: 100,
      step: "0.01",
      defaultValue: 0,
    },
    {
      name: "aliquotaPis",
      label: "PIS (%)",
      type: "number",
      min: 0,
      max: 100,
      step: "0.01",
      defaultValue: 0,
    },
    {
      name: "aliquotaCofins",
      label: "COFINS (%)",
      type: "number",
      min: 0,
      max: 100,
      step: "0.01",
      defaultValue: 0,
    },
    {
      name: "observacoes",
      label: "Observações",
      type: "textarea",
      span: "full",
      maxLength: 1000,
    },
    { name: "ativo", label: "Ativo", type: "checkbox", defaultValue: true },
  ];

  return (
    <div className="space-y-5">
      {panelTitle(
        "Regras fiscais de produtos",
        "Configure CFOP, CST/CSOSN, NCM e alíquotas usadas na NF-e/NFC-e.",
      )}

      <CrudPage
        embedded
        title="Regras fiscais"
        description="Cadastre regras por documento, UF e tributação."
        endpoint="/regras-fiscais-produtos"
        fields={fields}
        columns={[
          { key: "tipoDocumentoFiscal", label: "Nota" },
          { key: "ufDestino", label: "UF" },
          { key: "ncm", label: "NCM" },
          { key: "cfop", label: "CFOP" },
          { key: "cstCsosn", label: "CST/CSOSN" },
          { key: "aliquotaIcms", label: "ICMS" },
          {
            key: "ativo",
            label: "Status",
            render: (row) => (row.ativo ? "Ativa" : "Inativa"),
          },
        ]}
        submitLabel="Salvar regra"
        emptyText="Nenhuma regra fiscal criada."
      />
    </div>
  );
}

function EmissaoFiscalPanel() {
  const vendas = useOptions(
    "/vendas",
    (item) => `Venda ${item.numeroVenda ?? ""} - ${formatCurrency(item.valorTotal)}`,
  );
  const ordens = useOptions(
    "/ordens-servico",
    (item) => `OS ${item.numeroOs ?? ""} - ${item.clienteNome ?? ""}`,
  );

  const [reloadKey, setReloadKey] = useState(0);
  const [tipoDocumento, setTipoDocumento] = useState("nfce");
  const [form, setForm] = useState<ApiRecord>({
    vendaId: "",
    ordemServicoId: "",
    dataEmissao: hoje,
    observacoesNota: "",
    gerarContaReceber: false,
    validarTributacaoCompleta: true,
  });
  const [filtroTipo, setFiltroTipo] = useState("");
  const [filtroStatus, setFiltroStatus] = useState("");
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [emissaoErrors, setEmissaoErrors] = useState<Record<string, string>>({});

  const documentosPath = useMemo(() => {
    const params = new URLSearchParams();
    if (filtroTipo) params.set("tipoDocumento", filtroTipo);
    if (filtroStatus) params.set("status", filtroStatus);
    const query = params.toString();
    return query ? `/documentos-fiscais?${query}` : "/documentos-fiscais";
  }, [filtroStatus, filtroTipo]);

  const documentos = useList(documentosPath, reloadKey);

  const emissaoFields: FieldConfig[] = [
    tipoDocumento === "nfse"
      ? {
          name: "ordemServicoId",
          label: "Ordem de serviço",
          type: "select",
          required: true,
          options: ordens,
        }
      : {
          name: "vendaId",
          label: "Venda",
          type: "select",
          required: true,
          options: vendas,
        },
    { name: "dataEmissao", label: "Data", type: "date", required: true },
    {
      name: "observacoesNota",
      label: "Observações da nota",
      type: "textarea",
      span: "full",
      maxLength: 1000,
    },
    {
      name: "gerarContaReceber",
      label: "Gerar conta a receber",
      type: "checkbox",
    },
    ...(tipoDocumento !== "nfse"
      ? [
          {
            name: "validarTributacaoCompleta",
            label: "Validar tributação completa",
            type: "checkbox",
          } satisfies FieldConfig,
        ]
      : []),
  ];

  function setField(name: string, value: unknown) {
    setForm((current) => ({ ...current, [name]: value }));
    setEmissaoErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
  }

  async function emitir(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");
    setNotice("");

    const errors = validateForm(emissaoFields, form);
    if (Object.keys(errors).length > 0) {
      setEmissaoErrors(errors);
      setFailure("Corrija os campos da emissão antes de continuar.");
      return;
    }

    try {
      if (tipoDocumento === "nfse") {
        await apiRequest(`/documentos-fiscais/nfse/emitir-por-os/${form.ordemServicoId}`, {
          method: "POST",
          body: {
            dataCompetencia: form.dataEmissao,
            observacoesNota: form.observacoesNota,
            gerarContaReceber: Boolean(form.gerarContaReceber),
          },
        });
      } else {
        await apiRequest(`/documentos-fiscais/${tipoDocumento}/emitir-por-venda/${form.vendaId}`, {
          method: "POST",
          body: {
            dataEmissao: form.dataEmissao,
            observacoesNota: form.observacoesNota,
            gerarContaReceber: Boolean(form.gerarContaReceber),
            validarTributacaoCompleta: Boolean(form.validarTributacaoCompleta),
          },
        });
      }

      setReloadKey((key) => key + 1);
      setEmissaoErrors({});
      setNotice("Documento fiscal fake emitido.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function consultar(row: ApiRecord) {
    try {
      await apiRequest(`/documentos-fiscais/${row.id}/consultar`, { method: "POST" });
      setReloadKey((key) => key + 1);
      setNotice("Consulta realizada.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function cancelar(row: ApiRecord) {
    const motivo = window.prompt("Motivo do cancelamento");
    if (!motivo) return;

    try {
      await apiRequest(`/documentos-fiscais/${row.id}/cancelar`, {
        method: "POST",
        body: { motivo },
      });
      setReloadKey((key) => key + 1);
      setNotice("Documento cancelado.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  return (
    <div className="space-y-6">
      {panelTitle(
        "Emissão e consulta",
        "Emita documentos fake para validar fluxo, numeração, regras fiscais e telas.",
      )}

      <section className="rounded-3xl border border-slate-200 bg-slate-50 p-5">
        <div className="mb-5 grid gap-4 md:grid-cols-[1fr_auto] md:items-end">
          <label>
            <span className="mb-2 block text-sm font-medium text-slate-700">
              Tipo de documento
            </span>
            <select
              className={inputClass}
              value={tipoDocumento}
              onChange={(event) => {
                setTipoDocumento(event.target.value);
                setEmissaoErrors({});
              }}
            >
              <option value="nfce">NFC-e por venda</option>
              <option value="nfe">NF-e por venda</option>
              <option value="nfse">NFS-e por OS</option>
            </select>
          </label>
        </div>

        <form onSubmit={emitir} className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {emissaoFields.map((field) => (
              <div
                key={field.name}
                className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}
              >
                <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
                  <FieldRenderer
                    field={field}
                    value={form[field.name]}
                    error={emissaoErrors[field.name]}
                    onChange={setField}
                  />
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-end">
            <button
              type="submit"
              className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
            >
              Emitir fake
            </button>
          </div>
        </form>
      </section>

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure || documentos.error ? (
        <Notice type="error">{failure || documentos.error}</Notice>
      ) : null}

      <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-5 flex flex-col gap-4 border-b border-slate-100 pb-5 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <h3 className="text-lg font-bold text-slate-900">Documentos emitidos</h3>
            <p className="mt-1 text-sm text-slate-500">
              Filtre e acompanhe o status dos documentos fiscais emitidos.
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">Tipo</span>
              <select
                className={inputClass}
                value={filtroTipo}
                onChange={(event) => setFiltroTipo(event.target.value)}
              >
                <option value="">Todos os tipos</option>
                <option value="Nfce">NFC-e</option>
                <option value="Nfe">NF-e</option>
                <option value="Nfse">NFS-e</option>
              </select>
            </label>

            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">Status</span>
              <select
                className={inputClass}
                value={filtroStatus}
                onChange={(event) => setFiltroStatus(event.target.value)}
              >
                <option value="">Todos os status</option>
                <option value="Autorizada">Autorizada</option>
                <option value="Cancelada">Cancelada</option>
                <option value="Rejeitada">Rejeitada</option>
              </select>
            </label>
          </div>
        </div>

        <DataTable
          columns={[
            { key: "tipoDocumento", label: "Tipo" },
            { key: "numero", label: "Número" },
            { key: "serie", label: "Série" },
            { key: "clienteNome", label: "Cliente" },
            { key: "status", label: "Status" },
            { key: "ambiente", label: "Ambiente" },
            {
              key: "valorTotal",
              label: "Total",
              render: (row) => formatCurrency(row.valorTotal),
            },
            {
              key: "dataEmissao",
              label: "Emissão",
              render: (row) => formatDate(row.dataEmissao),
            },
          ]}
          rows={documentos.data}
          loading={documentos.loading}
          emptyText="Nenhum documento fiscal emitido."
          actions={(row) => (
            <div className="flex flex-wrap gap-2">
              <button
                className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                type="button"
                onClick={() => void consultar(row)}
              >
                Consultar
              </button>

              <button
                className="inline-flex items-center justify-center rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-100"
                type="button"
                onClick={() => void cancelar(row)}
              >
                Cancelar
              </button>
            </div>
          )}
        />
      </section>
    </div>
  );
}