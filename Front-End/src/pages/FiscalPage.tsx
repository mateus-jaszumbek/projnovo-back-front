import { useEffect, useMemo, useRef, useState } from "react";
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
import { apiAbsoluteResourceUrl, apiDownload, apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { useList, useOptions } from "../hooks/useApi";

const hoje = new Date().toISOString().slice(0, 10);

type FiscalTab = "config" | "credenciais" | "regras" | "emissao" | "checklist";

type DocumentoFiscalPrintItem = {
  tipoItem?: string;
  descricao?: string;
  quantidade?: number;
  valorUnitario?: number;
  desconto?: number;
  valorTotal?: number;
};

type DocumentoFiscalPrintData = {
  tipoDocumento?: string;
  numero?: number;
  serie?: number;
  status?: string;
  ambiente?: string;
  dataEmissao?: string;
  dataAutorizacao?: string | null;
  empresaRazaoSocial?: string;
  empresaNomeFantasia?: string;
  empresaCnpj?: string;
  empresaTelefone?: string | null;
  empresaEmail?: string | null;
  empresaLogoUrl?: string | null;
  empresaEnderecoCompleto?: string | null;
  clienteNome?: string;
  clienteCpfCnpj?: string | null;
  clienteTelefone?: string | null;
  clienteEmail?: string | null;
  clienteEnderecoCompleto?: string | null;
  chaveAcesso?: string | null;
  protocolo?: string | null;
  codigoVerificacao?: string | null;
  officialPdfUrl?: string | null;
  valorServicos?: number;
  valorProdutos?: number;
  desconto?: number;
  valorTotal?: number;
  itens?: DocumentoFiscalPrintItem[];
};

type DocumentoFiscalEmitResult = {
  status?: string;
  mensagemRejeicao?: string | null;
};

type DocumentoFiscalWebhookReplayResult = {
  documentoFiscalId?: string;
  tipoDocumento?: string;
  providerCode?: string;
  numeroExterno?: string | null;
  statusAtual?: string | null;
  reenvioAceito?: boolean;
  mensagem?: string;
};

type FocusMunicipioValidation = {
  providerCode?: string;
  municipioCodigo?: string | null;
  municipioNome?: string | null;
  uf?: string | null;
  statusNfse?: string | null;
  remoteValidationAvailable?: boolean;
  podeEmitirNfse?: boolean;
  itemListaServicoConfigurado?: boolean;
  cnaePrincipalConfigurado?: boolean;
  codigoTributarioMunicipioConfigurado?: boolean;
  codigoTributarioMunicipioObrigatorio?: boolean | null;
  errors?: string[];
  warnings?: string[];
};

function buildMunicipioFieldErrors(result: FocusMunicipioValidation) {
  const next: Record<string, string> = {};

  if (!result.municipioCodigo) {
    next.municipioCodigo = "Configure o código IBGE do município para a NFS-e.";
  }

  if (!result.itemListaServicoConfigurado) {
    next.itemListaServico = "Configure o item da lista de serviço antes de emitir NFS-e.";
  }

  if (!result.cnaePrincipalConfigurado) {
    next.cnaePrincipal = "Configure o CNAE principal antes de emitir NFS-e.";
  }

  if (result.codigoTributarioMunicipioObrigatorio === true && !result.codigoTributarioMunicipioConfigurado) {
    next.codigoTributarioMunicipio = "Este município exige código tributário municipal para a NFS-e.";
  }

  return next;
}

function summarizeMunicipioErrors(errors?: string[]) {
  if (!Array.isArray(errors) || errors.length === 0) return "";
  const preview = errors.slice(0, 3).join(" ");
  return errors.length > 3 ? `${preview} ...` : preview;
}

type FocusWebhookSetup = {
  providerCode?: string | null;
  focusProviderSelected?: boolean;
  enabled?: boolean;
  secretConfigured?: boolean;
  publicBaseUrl?: string | null;
  baseUrlLooksPublic?: boolean;
  dfeWebhookUrl?: string | null;
  nfseWebhookUrl?: string | null;
  urlsReady?: boolean;
  canRegisterRemotely?: boolean;
  checkedRemotely?: boolean;
  syncedRemotely?: boolean;
  dfeRemoteStatus?: {
    event?: string;
    credentialTipoDocumento?: string | null;
    credentialConfigured?: boolean;
    checkedRemotely?: boolean;
    registered?: boolean;
    hookId?: string | null;
    remoteUrl?: string | null;
  };
  nfseRemoteStatus?: {
    event?: string;
    credentialTipoDocumento?: string | null;
    credentialConfigured?: boolean;
    checkedRemotely?: boolean;
    registered?: boolean;
    hookId?: string | null;
    remoteUrl?: string | null;
  };
  actionsTaken?: string[];
  warnings?: string[];
  nextSteps?: string[];
};

type FiscalChecklistItem = {
  key?: string;
  title?: string;
  scope?: string;
  status?: string;
  detail?: string;
  blocksHomologacao?: boolean;
  blocksProducao?: boolean;
};

type FiscalReadiness = {
  empresaId?: string;
  ambiente?: string;
  providerCode?: string | null;
  focusProviderSelected?: boolean;
  homologacaoReady?: boolean;
  producaoReady?: boolean;
  okCount?: number;
  warningCount?: number;
  errorCount?: number;
  summary?: string;
  missingForHomologacao?: string[];
  missingForProducao?: string[];
  nextSteps?: string[];
  items?: FiscalChecklistItem[];
};

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

function displayPrintValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  return String(value);
}

function escapePrintHtml(value: unknown) {
  return displayPrintValue(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function fiscalLogoHtml(data: DocumentoFiscalPrintData) {
  const logo = apiAbsoluteResourceUrl(data.empresaLogoUrl ?? "");
  if (!logo) return "";
  return `<img class="company-logo" src="${escapePrintHtml(logo)}" alt="Logo da empresa" />`;
}

function renderFiscalPrint(popup: Window, data: DocumentoFiscalPrintData) {
  const itens = Array.isArray(data.itens) ? data.itens : [];
  const itemRows = itens
    .map(
      (item, index) => `
        <tr>
          <td>${index + 1}</td>
          <td>${escapePrintHtml(item.tipoItem)}</td>
          <td>${escapePrintHtml(item.descricao)}</td>
          <td>${escapePrintHtml(item.quantidade)}</td>
          <td>${escapePrintHtml(formatCurrency(item.valorUnitario ?? 0))}</td>
          <td>${escapePrintHtml(formatCurrency(item.desconto ?? 0))}</td>
          <td>${escapePrintHtml(formatCurrency(item.valorTotal ?? 0))}</td>
        </tr>
      `,
    )
    .join("");

  popup.document.write(`
    <html>
      <head>
        <title>${escapePrintHtml(data.tipoDocumento)} ${escapePrintHtml(data.numero)}</title>
        <style>
          * { box-sizing: border-box; }
          body { font-family: Arial, sans-serif; padding: 24px; color: #0f172a; font-size: 13px; }
          header { display: flex; justify-content: space-between; gap: 20px; border-bottom: 2px solid #0f172a; padding-bottom: 14px; margin-bottom: 18px; }
          .brand { display: flex; align-items: center; gap: 14px; }
          .company-logo { width: 76px; height: 76px; object-fit: contain; border: 1px solid #e2e8f0; border-radius: 8px; padding: 6px; }
          h1 { margin: 0; font-size: 24px; }
          .muted { color: #64748b; }
          .summary { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; min-width: 260px; }
          .summary div { border: 1px solid #e2e8f0; border-radius: 8px; padding: 8px; text-align: right; }
          .summary span { display: block; color: #64748b; font-size: 11px; }
          .summary strong { display: block; margin-top: 4px; font-size: 16px; }
          h2 { margin: 24px 0 10px; font-size: 16px; border-bottom: 1px solid #e2e8f0; padding-bottom: 6px; }
          .grid { display: grid; grid-template-columns: 180px 1fr; gap: 8px 16px; }
          .label { font-weight: 700; color: #475569; }
          .value { border-bottom: 1px solid #e5e7eb; padding-bottom: 8px; }
          table { width: 100%; border-collapse: collapse; margin-top: 10px; }
          th, td { border-bottom: 1px solid #e5e7eb; padding: 8px; text-align: left; font-size: 12px; }
          th { color: #475569; }
          .empty { border: 1px dashed #cbd5e1; border-radius: 8px; padding: 12px; color: #64748b; }
          @media print { body { padding: 12mm; } }
        </style>
      </head>
      <body>
        <header>
          <div class="brand">
            ${fiscalLogoHtml(data)}
            <div>
              <h1>${escapePrintHtml(data.tipoDocumento)} ${escapePrintHtml(data.numero)}</h1>
              <div class="muted">Série ${escapePrintHtml(data.serie)} | ${escapePrintHtml(data.status)} | ${escapePrintHtml(data.ambiente)}</div>
            </div>
          </div>
          <div class="summary">
            <div><span>Produtos</span><strong>${formatCurrency(data.valorProdutos ?? 0)}</strong></div>
            <div><span>Servicos</span><strong>${formatCurrency(data.valorServicos ?? 0)}</strong></div>
            <div><span>Desconto</span><strong>${formatCurrency(data.desconto ?? 0)}</strong></div>
            <div><span>Total</span><strong>${formatCurrency(data.valorTotal ?? 0)}</strong></div>
          </div>
        </header>

        <h2>Empresa</h2>
        <div class="grid">
          <div class="label">Razão social</div><div class="value">${escapePrintHtml(data.empresaRazaoSocial)}</div>
          <div class="label">Nome fantasia</div><div class="value">${escapePrintHtml(data.empresaNomeFantasia)}</div>
          <div class="label">CNPJ</div><div class="value">${escapePrintHtml(data.empresaCnpj)}</div>
          <div class="label">Endereço</div><div class="value">${escapePrintHtml(data.empresaEnderecoCompleto)}</div>
          <div class="label">Contato</div><div class="value">${escapePrintHtml([data.empresaTelefone, data.empresaEmail].filter(Boolean).join(" | "))}</div>
        </div>

        <h2>Destinatário</h2>
        <div class="grid">
          <div class="label">Nome</div><div class="value">${escapePrintHtml(data.clienteNome)}</div>
          <div class="label">CPF/CNPJ</div><div class="value">${escapePrintHtml(data.clienteCpfCnpj)}</div>
          <div class="label">Endereço</div><div class="value">${escapePrintHtml(data.clienteEnderecoCompleto)}</div>
          <div class="label">Contato</div><div class="value">${escapePrintHtml([data.clienteTelefone, data.clienteEmail].filter(Boolean).join(" | "))}</div>
        </div>

        <h2>Dados fiscais</h2>
        <div class="grid">
          <div class="label">Emissão</div><div class="value">${formatDate(data.dataEmissao)}</div>
          <div class="label">Autorização</div><div class="value">${formatDate(data.dataAutorizacao)}</div>
          <div class="label">Chave de acesso</div><div class="value">${escapePrintHtml(data.chaveAcesso)}</div>
          <div class="label">Protocolo</div><div class="value">${escapePrintHtml(data.protocolo)}</div>
          <div class="label">Código de verificação</div><div class="value">${escapePrintHtml(data.codigoVerificacao)}</div>
        </div>

        <h2>Itens</h2>
        ${
          itemRows
            ? `<table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Tipo</th>
                    <th>Descrição</th>
                    <th>Qtd.</th>
                    <th>Unitário</th>
                    <th>Desconto</th>
                    <th>Total</th>
                  </tr>
                </thead>
                <tbody>${itemRows}</tbody>
              </table>`
            : `<div class="empty">Nenhum item fiscal disponível para impressão.</div>`
        }
      </body>
    </html>
  `);

  popup.document.close();
  popup.focus();
  popup.print();
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export function FiscalPage() {
  const [tab, setTab] = useState<FiscalTab>("config");

  return (
    <PageFrame
      eyebrow="Fiscal"
      title="Notas e regras fiscais"
      description="Configure, valide regras e emita documentos fiscais em homologação ou com provider real por empresa."
    >
      <div className="space-y-6">
        <Notice type="info">
          Homologação ainda é o melhor lugar para validar fluxo, numeração e regras. Produção real exige credenciais por empresa, certificado e integração com SEFAZ ou prefeitura.
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
            className={tabButtonClass(tab === "credenciais")}
            onClick={() => setTab("credenciais")}
          >
            <Settings size={16} />
            Credenciais
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
          {tab === "credenciais" ? <CredenciaisFiscaisPanel /> : null}
          {tab === "regras" ? <RegrasFiscaisPanel /> : null}
          {tab === "emissao" ? <EmissaoFiscalPanel /> : null}
          {tab === "checklist" ? <FiscalChecklistLivePanel /> : null}
        </section>
      </div>
    </PageFrame>
  );
}

type FiscalCredentialRecord = {
  id?: string;
  tipoDocumentoFiscal?: string;
  provedor?: string;
  urlBase?: string | null;
  clientId?: string | null;
  usuarioApi?: string | null;
  clientSecretConfigurado?: boolean;
  senhaApiConfigurada?: boolean;
  certificadoConfigurado?: boolean;
  certificadoSenhaConfigurada?: boolean;
  tokenAcessoConfigurado?: boolean;
  tokenExpiraEm?: string | null;
  ativo?: boolean;
};

type FiscalCredentialForm = {
  tipoDocumentoFiscal: string;
  provedor: string;
  urlBase: string;
  clientId: string;
  clientSecret: string;
  limparClientSecret: boolean;
  usuarioApi: string;
  senhaApi: string;
  limparSenhaApi: boolean;
  certificadoBase64: string;
  certificadoSenha: string;
  limparCertificado: boolean;
  tokenAcesso: string;
  tokenExpiraEm: string;
  limparTokenAcesso: boolean;
  ativo: boolean;
};

const fiscalCredentialDefaults = (): FiscalCredentialForm => ({
  tipoDocumentoFiscal: "Nfse",
  provedor: "",
  urlBase: "",
  clientId: "",
  clientSecret: "",
  limparClientSecret: false,
  usuarioApi: "",
  senhaApi: "",
  limparSenhaApi: false,
  certificadoBase64: "",
  certificadoSenha: "",
  limparCertificado: false,
  tokenAcesso: "",
  tokenExpiraEm: "",
  limparTokenAcesso: false,
  ativo: true,
});

function secretConfiguredFlag(record: FiscalCredentialRecord) {
  const flags = [
    record.clientSecretConfigurado ? "Client secret" : "",
    record.senhaApiConfigurada ? "Senha API" : "",
    record.certificadoConfigurado ? "Certificado" : "",
    record.tokenAcessoConfigurado ? "Token" : "",
  ].filter(Boolean);

  return flags.length > 0 ? flags.join(", ") : "Sem segredos";
}

function toDateTimeInputValue(value: unknown) {
  if (!value) return "";
  return String(value).slice(0, 16);
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : undefined;
}

function CredenciaisFiscaisPanel() {
  const [reloadKey, setReloadKey] = useState(0);
  const credenciais = useList("/credenciais-fiscais", reloadKey);
  const [selectedId, setSelectedId] = useState("");
  const [form, setForm] = useState<FiscalCredentialForm>(() => fiscalCredentialDefaults());
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [saving, setSaving] = useState(false);

  const selectedRecord = useMemo(
    () =>
      credenciais.data.find((item) => String(item.id ?? "") === selectedId) as
        | FiscalCredentialRecord
        | undefined,
    [credenciais.data, selectedId],
  );

  function resetForm() {
    setSelectedId("");
    setForm(fiscalCredentialDefaults());
    setFailure("");
    setNotice("");
  }

  function loadRecord(record: FiscalCredentialRecord) {
    setSelectedId(String(record.id ?? ""));
    setForm({
      tipoDocumentoFiscal: String(record.tipoDocumentoFiscal ?? "Nfse"),
      provedor: String(record.provedor ?? ""),
      urlBase: String(record.urlBase ?? ""),
      clientId: String(record.clientId ?? ""),
      clientSecret: "",
      limparClientSecret: false,
      usuarioApi: String(record.usuarioApi ?? ""),
      senhaApi: "",
      limparSenhaApi: false,
      certificadoBase64: "",
      certificadoSenha: "",
      limparCertificado: false,
      tokenAcesso: "",
      tokenExpiraEm: toDateTimeInputValue(record.tokenExpiraEm),
      limparTokenAcesso: false,
      ativo: Boolean(record.ativo ?? true),
    });
    setFailure("");
    setNotice("");
  }

  function update<K extends keyof FiscalCredentialForm>(name: K, value: FiscalCredentialForm[K]) {
    setForm((current) => ({ ...current, [name]: value }));
  }

  async function salvar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setFailure("");
    setNotice("");

    try {
      const body: ApiRecord = {
        tipoDocumentoFiscal: form.tipoDocumentoFiscal,
        provedor: form.provedor.trim(),
        urlBase: normalizeOptionalText(form.urlBase),
        clientId: normalizeOptionalText(form.clientId),
        usuarioApi: normalizeOptionalText(form.usuarioApi),
        ativo: form.ativo,
      };

      if (form.clientSecret.trim()) body.clientSecret = form.clientSecret.trim();
      if (form.limparClientSecret) body.limparClientSecret = true;

      if (form.senhaApi.trim()) body.senhaApi = form.senhaApi.trim();
      if (form.limparSenhaApi) body.limparSenhaApi = true;

      if (form.certificadoBase64.trim()) body.certificadoBase64 = form.certificadoBase64.trim();
      if (form.certificadoSenha.trim()) body.certificadoSenha = form.certificadoSenha.trim();
      if (form.limparCertificado) body.limparCertificado = true;

      if (form.tokenAcesso.trim()) body.tokenAcesso = form.tokenAcesso.trim();
      if (form.tokenExpiraEm) body.tokenExpiraEm = form.tokenExpiraEm;
      if (form.limparTokenAcesso) body.limparTokenAcesso = true;

      const endpoint = selectedId
        ? `/credenciais-fiscais/${selectedId}`
        : "/credenciais-fiscais";

      await apiRequest<ApiRecord>(endpoint, {
        method: selectedId ? "PUT" : "POST",
        body,
      });

      setReloadKey((key) => key + 1);
      const successMessage = selectedId
        ? "Credencial fiscal atualizada com sucesso."
        : "Credencial fiscal criada com sucesso.";

      if (!selectedId) {
        setSelectedId("");
        setForm(fiscalCredentialDefaults());
      } else if (selectedRecord) {
        loadRecord(selectedRecord);
      }

      setNotice(successMessage);
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      {panelTitle(
        "Credenciais fiscais por empresa",
        "Cada empresa mantém suas próprias credenciais. Os segredos nunca voltam em texto para a tela.",
      )}

      <Notice type="info">
        Deixe os campos de segredo em branco para manter o valor atual. Use as opções de limpeza quando precisar remover um segredo salvo.
      </Notice>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)]">
        <section className="space-y-4 rounded-3xl border border-slate-200 bg-slate-50 p-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="text-lg font-bold text-slate-900">Credenciais cadastradas</h3>
              <p className="mt-1 text-sm text-slate-500">
                Somente credenciais da empresa atual aparecem aqui.
              </p>
            </div>

            <button type="button" className={tabButtonClass(false)} onClick={resetForm}>
              Nova credencial
            </button>
          </div>

          <DataTable
            columns={[
              { key: "tipoDocumentoFiscal", label: "Documento" },
              { key: "provedor", label: "Provedor" },
              { key: "urlBase", label: "URL base" },
              {
                key: "segredos",
                label: "Segredos",
                render: (row) => secretConfiguredFlag(row as FiscalCredentialRecord),
              },
              {
                key: "ativo",
                label: "Status",
                render: (row) => (row.ativo ? "Ativa" : "Inativa"),
              },
            ]}
            rows={credenciais.data}
            loading={credenciais.loading}
            emptyText="Nenhuma credencial fiscal cadastrada."
            actions={(row) => (
              <button
                className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                type="button"
                onClick={() => loadRecord(row as FiscalCredentialRecord)}
              >
                Editar
              </button>
            )}
          />
        </section>

        <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 border-b border-slate-100 pb-4">
            <h3 className="text-lg font-bold text-slate-900">
              {selectedId ? "Editar credencial" : "Nova credencial"}
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              Provedor, certificado, token e credenciais operacionais por empresa.
            </p>
          </div>

          <form onSubmit={salvar} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <label>
                <span className="mb-2 block text-sm font-medium text-slate-700">
                  Tipo de documento
                </span>
                <select
                  className={inputClass}
                  value={form.tipoDocumentoFiscal}
                  onChange={(event) => update("tipoDocumentoFiscal", event.target.value)}
                >
                  <option value="Nfse">NFS-e</option>
                  <option value="Nfe">NF-e</option>
                  <option value="Nfce">NFC-e</option>
                </select>
              </label>

              <label>
                <span className="mb-2 block text-sm font-medium text-slate-700">Provedor</span>
                <input
                  className={inputClass}
                  value={form.provedor}
                  placeholder="Ex.: focusnfe, tecnospeed, prefeiturax"
                  onChange={(event) => update("provedor", event.target.value)}
                  required
                />
              </label>
            </div>

            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">URL base</span>
              <input
                className={inputClass}
                value={form.urlBase}
                placeholder="https://api.seu-provedor.com"
                onChange={(event) => update("urlBase", event.target.value)}
              />
            </label>

            <div className="grid gap-4 md:grid-cols-2">
              <label>
                <span className="mb-2 block text-sm font-medium text-slate-700">Client ID</span>
                <input
                  className={inputClass}
                  value={form.clientId}
                  onChange={(event) => update("clientId", event.target.value)}
                />
              </label>

              <label>
                <span className="mb-2 block text-sm font-medium text-slate-700">
                  Client secret
                  {selectedRecord?.clientSecretConfigurado ? " (já salvo)" : ""}
                </span>
                <input
                  className={inputClass}
                  type="password"
                  value={form.clientSecret}
                  placeholder={selectedRecord?.clientSecretConfigurado ? "Manter atual" : ""}
                  onChange={(event) => update("clientSecret", event.target.value)}
                />
              </label>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <label>
                <span className="mb-2 block text-sm font-medium text-slate-700">Usuário API</span>
                <input
                  className={inputClass}
                  value={form.usuarioApi}
                  onChange={(event) => update("usuarioApi", event.target.value)}
                />
              </label>

              <label>
                <span className="mb-2 block text-sm font-medium text-slate-700">
                  Senha API
                  {selectedRecord?.senhaApiConfigurada ? " (já salva)" : ""}
                </span>
                <input
                  className={inputClass}
                  type="password"
                  value={form.senhaApi}
                  placeholder={selectedRecord?.senhaApiConfigurada ? "Manter atual" : ""}
                  onChange={(event) => update("senhaApi", event.target.value)}
                />
              </label>
            </div>

            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Token de acesso
                {selectedRecord?.tokenAcessoConfigurado ? " (já salvo)" : ""}
              </span>
              <input
                className={inputClass}
                type="password"
                value={form.tokenAcesso}
                placeholder={selectedRecord?.tokenAcessoConfigurado ? "Manter atual" : ""}
                onChange={(event) => update("tokenAcesso", event.target.value)}
              />
            </label>

            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Expiração do token
              </span>
              <input
                className={inputClass}
                type="datetime-local"
                value={form.tokenExpiraEm}
                onChange={(event) => update("tokenExpiraEm", event.target.value)}
              />
            </label>

            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Certificado base64
                {selectedRecord?.certificadoConfigurado ? " (já salvo)" : ""}
              </span>
              <textarea
                className="min-h-[140px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
                value={form.certificadoBase64}
                placeholder={selectedRecord?.certificadoConfigurado ? "Manter atual" : "Cole o conteúdo base64 do certificado"}
                onChange={(event) => update("certificadoBase64", event.target.value)}
              />
            </label>

            <label>
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Senha do certificado
                {selectedRecord?.certificadoSenhaConfigurada ? " (já salva)" : ""}
              </span>
              <input
                className={inputClass}
                type="password"
                value={form.certificadoSenha}
                placeholder={selectedRecord?.certificadoSenhaConfigurada ? "Manter atual" : ""}
                onChange={(event) => update("certificadoSenha", event.target.value)}
              />
            </label>

            <div className="grid gap-3 md:grid-cols-2">
              <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={form.limparClientSecret}
                  onChange={(event) => update("limparClientSecret", event.target.checked)}
                />
                Limpar client secret salvo
              </label>

              <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={form.limparSenhaApi}
                  onChange={(event) => update("limparSenhaApi", event.target.checked)}
                />
                Limpar senha API salva
              </label>

              <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={form.limparCertificado}
                  onChange={(event) => update("limparCertificado", event.target.checked)}
                />
                Limpar certificado salvo
              </label>

              <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={form.limparTokenAcesso}
                  onChange={(event) => update("limparTokenAcesso", event.target.checked)}
                />
                Limpar token salvo
              </label>

              <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700 md:col-span-2">
                <input
                  type="checkbox"
                  checked={form.ativo}
                  onChange={(event) => update("ativo", event.target.checked)}
                />
                Credencial ativa
              </label>
            </div>

            <div className="flex flex-wrap justify-end gap-3 pt-2">
              <button type="button" className={tabButtonClass(false)} onClick={resetForm}>
                Limpar formulário
              </button>

              <button
                type="submit"
                disabled={saving}
                className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {saving ? "Salvando..." : selectedId ? "Salvar alterações" : "Criar credencial"}
              </button>
            </div>
          </form>

          {notice ? <Notice type="success">{notice}</Notice> : null}
          {failure || credenciais.error ? (
            <Notice type="error">{failure || credenciais.error}</Notice>
          ) : null}
        </section>
      </div>
    </div>
  );
}

function FiscalChecklistLivePanel() {
  const [loading, setLoading] = useState(true);
  const [readiness, setReadiness] = useState<FiscalReadiness | null>(null);
  const [failure, setFailure] = useState("");

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const result = await apiRequest<FiscalReadiness>("/configuracao-fiscal/checklist");
        if (active) setReadiness(result);
      } catch (err) {
        if (active) setFailure(errorMessage(err));
      } finally {
        if (active) setLoading(false);
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, []);

  const items = Array.isArray(readiness?.items) ? readiness.items : [];
  const missingForHomologacao = Array.isArray(readiness?.missingForHomologacao)
    ? readiness.missingForHomologacao
    : [];
  const missingForProducao = Array.isArray(readiness?.missingForProducao)
    ? readiness.missingForProducao
    : [];
  const nextSteps = Array.isArray(readiness?.nextSteps) ? readiness.nextSteps : [];

  function statusLabel(status?: string) {
    switch ((status || "").toLowerCase()) {
      case "ok":
        return "OK";
      case "warning":
        return "Ajustar";
      case "error":
        return "Bloqueia";
      default:
        return "Info";
    }
  }

  function statusClasses(status?: string) {
    switch ((status || "").toLowerCase()) {
      case "ok":
        return {
          card: "border-emerald-200 bg-emerald-50",
          badge: "bg-emerald-100 text-emerald-700",
        };
      case "warning":
        return {
          card: "border-amber-200 bg-amber-50",
          badge: "bg-amber-100 text-amber-700",
        };
      case "error":
        return {
          card: "border-rose-200 bg-rose-50",
          badge: "bg-rose-100 text-rose-700",
        };
      default:
        return {
          card: "border-slate-200 bg-slate-50",
          badge: "bg-slate-200 text-slate-700",
        };
    }
  }

  return (
    <div className="space-y-6">
      {panelTitle(
        "Checklist para homologacao e producao",
        "Aqui a empresa enxerga o que ja esta pronto e o que ainda falta para sair do fake e fechar a operacao fiscal real.",
      )}

      {loading ? (
        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm text-slate-500">
          Carregando checklist fiscal da empresa...
        </div>
      ) : null}

      {!loading && failure ? <Notice type="error">{failure}</Notice> : null}

      {!loading && readiness ? (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <article className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Resumo</span>
              <p className="mt-2 text-sm font-medium text-slate-900">{readiness.summary || "-"}</p>
              <p className="mt-2 text-xs text-slate-500">
                Provider: {readiness.providerCode || "-"} | Ambiente atual: {readiness.ambiente || "-"}
              </p>
            </article>

            <article className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Homologacao</span>
              <p className="mt-2 text-sm font-medium text-slate-900">
                {readiness.homologacaoReady ? "Liberada" : "Com bloqueios"}
              </p>
              <p className="mt-2 text-xs text-slate-500">
                {missingForHomologacao.length > 0
                  ? `${missingForHomologacao.length} pendencia(s) bloqueando homologacao.`
                  : "Sem bloqueios para homologacao."}
              </p>
            </article>

            <article className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Producao</span>
              <p className="mt-2 text-sm font-medium text-slate-900">
                {readiness.producaoReady ? "Pronta" : "Pendente"}
              </p>
              <p className="mt-2 text-xs text-slate-500">
                {missingForProducao.length > 0
                  ? `${missingForProducao.length} pendencia(s) para fechar producao.`
                  : "Sem bloqueios de producao."}
              </p>
            </article>

            <article className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Sinais</span>
              <p className="mt-2 text-sm font-medium text-slate-900">
                {readiness.okCount || 0} ok | {readiness.warningCount || 0} avisos | {readiness.errorCount || 0} bloqueios
              </p>
              <p className="mt-2 text-xs text-slate-500">
                O painel considera configuracao, provider, credenciais e os fluxos fiscais ativos da empresa.
              </p>
            </article>
          </div>

          {missingForHomologacao.length > 0 || missingForProducao.length > 0 ? (
            <div className="grid gap-4 xl:grid-cols-2">
              <section className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                <strong className="text-sm font-semibold text-amber-700">Falta para homologacao</strong>
                {missingForHomologacao.length > 0 ? (
                  <ul className="mt-3 space-y-2 text-sm text-amber-700">
                    {missingForHomologacao.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                ) : (
                  <p className="mt-3 text-sm text-emerald-700">Homologacao ja pode seguir.</p>
                )}
              </section>

              <section className="rounded-2xl border border-rose-200 bg-rose-50 p-4">
                <strong className="text-sm font-semibold text-rose-700">Falta para producao</strong>
                {missingForProducao.length > 0 ? (
                  <ul className="mt-3 space-y-2 text-sm text-rose-700">
                    {missingForProducao.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                ) : (
                  <p className="mt-3 text-sm text-emerald-700">Producao pronta para o provider atual.</p>
                )}
              </section>
            </div>
          ) : null}

          {items.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {items.map((item, index) => {
                const tone = statusClasses(item.status);

                return (
                  <article
                    key={item.key || `${item.title || "item"}-${index}`}
                    className={`rounded-2xl border p-4 ${tone.card}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex items-start gap-3">
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-white text-slate-700">
                          <Sparkles size={16} />
                        </div>
                        <div>
                          <strong className="text-sm font-medium leading-6 text-slate-900">
                            {item.title || "-"}
                          </strong>
                          <p className="mt-1 text-xs uppercase tracking-wide text-slate-500">
                            {item.scope || "geral"}
                          </p>
                        </div>
                      </div>

                      <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${tone.badge}`}>
                        {statusLabel(item.status)}
                      </span>
                    </div>

                    <p className="mt-4 text-sm leading-6 text-slate-700">{item.detail || "-"}</p>

                    <div className="mt-4 flex flex-wrap gap-2">
                      {item.blocksHomologacao ? (
                        <span className="rounded-full bg-slate-900 px-2.5 py-1 text-xs font-semibold text-white">
                          Bloqueia homologacao
                        </span>
                      ) : null}
                      {item.blocksProducao ? (
                        <span className="rounded-full bg-rose-600 px-2.5 py-1 text-xs font-semibold text-white">
                          Bloqueia producao
                        </span>
                      ) : null}
                      {!item.blocksHomologacao && !item.blocksProducao ? (
                        <span className="rounded-full bg-white px-2.5 py-1 text-xs font-semibold text-slate-600">
                          Sem bloqueio direto
                        </span>
                      ) : null}
                    </div>
                  </article>
                );
              })}
            </div>
          ) : null}

          {nextSteps.length > 0 ? (
            <section className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <strong className="text-sm font-semibold text-slate-900">Proximos passos</strong>
              <ul className="mt-3 space-y-2 text-sm text-slate-600">
                {nextSteps.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </section>
          ) : null}
        </>
      ) : null}
    </div>
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

void ChecklistFiscalPanel;

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
      placeholder: "fake ou focusnfe",
      helper: "Use focusnfe para o provider real já integrado nesta etapa.",
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
    {
      name: "codigoTributarioMunicipio",
      label: "Código tributário municipal",
      maxLength: 40,
      helper: "Preencha quando a prefeitura ou a Focus exigir esse código para a NFS-e.",
    },
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
  const [municipioValidation, setMunicipioValidation] = useState<FocusMunicipioValidation | null>(null);
  const [validatingMunicipio, setValidatingMunicipio] = useState(false);
  const [webhookSetup, setWebhookSetup] = useState<FocusWebhookSetup | null>(null);
  const [loadingWebhookSetup, setLoadingWebhookSetup] = useState(false);
  const [syncingWebhookSetup, setSyncingWebhookSetup] = useState(false);
  const municipioValidationRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    let active = true;

    async function load() {
      setLoadingWebhookSetup(true);

      try {
        const result = await apiRequest<ApiRecord>("/configuracao-fiscal");
        if (active) setForm(formFromRecord(fields, result));
      } catch {
        if (active) {
          setNotice("Salve a configuração fiscal inicial para começar em homologação.");
        }
      }

      try {
        const setup = await apiRequest<FocusWebhookSetup>(
          "/configuracao-fiscal/focus/webhook-status",
        );
        if (active) setWebhookSetup(setup);
      } catch {
        if (active) setWebhookSetup(null);
      } finally {
        if (active) {
          setLoading(false);
          setLoadingWebhookSetup(false);
        }
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
      void recarregarWebhookSetup();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function validarMunicipioFocus() {
    setFailure("");
    setNotice("");
    setValidatingMunicipio(true);

    try {
      const result = await apiRequest<FocusMunicipioValidation>(
        "/configuracao-fiscal/focus-nfse/municipio-validacao",
      );
      setMunicipioValidation(result);
      setValidationErrors((current) => {
        const next = { ...current };
        delete next.municipioCodigo;
        delete next.itemListaServico;
        delete next.cnaePrincipal;
        delete next.codigoTributarioMunicipio;
        return {
          ...next,
          ...buildMunicipioFieldErrors(result),
        };
      });

      if (Array.isArray(result.errors) && result.errors.length > 0) {
        setFailure(
          `A validação do município encontrou pendências para a NFS-e. ${summarizeMunicipioErrors(result.errors)}`,
        );
      } else if (result.podeEmitirNfse) {
        setNotice("Validação do município concluída. A configuração atual está pronta para a NFS-e na Focus.");
      } else {
        setNotice("Validação do município concluída.");
      }

      window.requestAnimationFrame(() => {
        municipioValidationRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setValidatingMunicipio(false);
    }
  }

  async function recarregarWebhookSetup() {
    setLoadingWebhookSetup(true);

    try {
      const result = await apiRequest<FocusWebhookSetup>(
        "/configuracao-fiscal/focus/webhook-status",
      );
      setWebhookSetup(result);
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLoadingWebhookSetup(false);
    }
  }

  async function sincronizarWebhookFocus() {
    setSyncingWebhookSetup(true);
    setFailure("");
    setNotice("");

    try {
      const result = await apiRequest<FocusWebhookSetup>(
        "/configuracao-fiscal/focus/webhook-sync",
        {
          method: "POST",
        },
      );
      setWebhookSetup(result);

      if (Array.isArray(result.actionsTaken) && result.actionsTaken.length > 0) {
        setNotice(result.actionsTaken.join(" "));
      } else {
        setNotice("Sincronização concluída. O status remoto foi atualizado.");
      }
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSyncingWebhookSetup(false);
    }
  }

  async function copiarWebhookUrl(url: string | null | undefined, label: string) {
    if (!url) return;

    try {
      await navigator.clipboard.writeText(url);
      setNotice(`${label} copiada.`);
      setFailure("");
    } catch {
      setFailure(`Não foi possível copiar ${label.toLowerCase()}.`);
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

        <div className="flex flex-wrap justify-end gap-3">
          <button
            type="button"
            onClick={() => void validarMunicipioFocus()}
            disabled={loading || validatingMunicipio}
            className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {validatingMunicipio ? "Validando município..." : "Validar município Focus"}
          </button>

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

      <section className="rounded-3xl border border-slate-200 bg-slate-50 p-5">
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3 border-b border-slate-200 pb-4">
          <div>
            <h3 className="text-lg font-bold text-slate-900">Webhook da Focus</h3>
            <p className="mt-1 text-sm text-slate-500">
              Veja as URLs prontas, confira o cadastro remoto e deixe a Focus alinhada com a empresa atual sem sair da tela fiscal.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <button
              type="button"
              onClick={() => void recarregarWebhookSetup()}
              disabled={loadingWebhookSetup || syncingWebhookSetup}
              className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loadingWebhookSetup ? "Verificando..." : "Verificar na Focus"}
            </button>

            <button
              type="button"
              onClick={() => void sincronizarWebhookFocus()}
              disabled={syncingWebhookSetup || loadingWebhookSetup || !webhookSetup?.canRegisterRemotely}
              className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {syncingWebhookSetup ? "Cadastrando..." : "Cadastrar na Focus"}
            </button>
          </div>
        </div>

        {webhookSetup ? (
          <div className="space-y-5">
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Provedor</span>
                <p className="mt-2 text-sm text-slate-800">
                  {webhookSetup.providerCode || "-"}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  {webhookSetup.focusProviderSelected
                    ? "Focus selecionada para a empresa atual."
                    : "A empresa ainda não está apontando para a Focus."}
                </p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Receiver</span>
                <p className="mt-2 text-sm text-slate-800">
                  {webhookSetup.enabled ? "Ativo" : "Desativado"}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  O backend precisa estar ativo para processar os pushes da Focus.
                </p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Segredo</span>
                <p className="mt-2 text-sm text-slate-800">
                  {webhookSetup.secretConfigured ? "Configurado" : "Pendente"}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Sem o segredo a URL não fica pronta para cadastro na Focus.
                </p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">URL pública</span>
                <p className="mt-2 text-sm text-slate-800">
                  {webhookSetup.publicBaseUrl || "-"}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  {webhookSetup.baseUrlLooksPublic
                    ? "Base pública pronta para webhook."
                    : "Use HTTPS público para receber chamadas externas."}
                </p>
              </div>
            </div>

            <div className="grid gap-4 xl:grid-cols-2">
              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Focus remoto DF-e</span>
                <p className="mt-2 text-sm text-slate-800">
                  {webhookSetup.dfeRemoteStatus?.registered
                    ? "Cadastrado"
                    : webhookSetup.dfeRemoteStatus?.checkedRemotely
                      ? "Não encontrado"
                      : "Não verificado"}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Credencial: {webhookSetup.dfeRemoteStatus?.credentialTipoDocumento || "-"} | Hook: {webhookSetup.dfeRemoteStatus?.hookId || "-"}
                </p>
                <p className="mt-3 text-xs text-slate-500 break-all">
                  {webhookSetup.dfeRemoteStatus?.remoteUrl || "Sem URL remota cadastrada para este escopo."}
                </p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Focus remoto NFS-e</span>
                <p className="mt-2 text-sm text-slate-800">
                  {webhookSetup.nfseRemoteStatus?.registered
                    ? "Cadastrado"
                    : webhookSetup.nfseRemoteStatus?.checkedRemotely
                      ? "Não encontrado"
                      : "Não verificado"}
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Credencial: {webhookSetup.nfseRemoteStatus?.credentialTipoDocumento || "-"} | Hook: {webhookSetup.nfseRemoteStatus?.hookId || "-"}
                </p>
                <p className="mt-3 text-xs text-slate-500 break-all">
                  {webhookSetup.nfseRemoteStatus?.remoteUrl || "Sem URL remota cadastrada para este escopo."}
                </p>
              </div>
            </div>

            <div className="grid gap-4 xl:grid-cols-2">
              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">URL DF-e</span>
                <div className="mt-3 flex flex-col gap-3 sm:flex-row">
                  <input
                    className={inputClass}
                    readOnly
                    value={webhookSetup.dfeWebhookUrl ?? ""}
                    placeholder="Configure o segredo e a URL pública para gerar este endpoint."
                  />
                  <button
                    type="button"
                    onClick={() => void copiarWebhookUrl(webhookSetup.dfeWebhookUrl, "URL DF-e")}
                    disabled={!webhookSetup.dfeWebhookUrl}
                    className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Copiar
                  </button>
                </div>
                <p className="mt-2 text-xs text-slate-500">
                  Use para NF-e e NFC-e.
                </p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <span className="text-xs font-medium uppercase tracking-wide text-slate-500">URL NFS-e</span>
                <div className="mt-3 flex flex-col gap-3 sm:flex-row">
                  <input
                    className={inputClass}
                    readOnly
                    value={webhookSetup.nfseWebhookUrl ?? ""}
                    placeholder="Configure o segredo e a URL pública para gerar este endpoint."
                  />
                  <button
                    type="button"
                    onClick={() => void copiarWebhookUrl(webhookSetup.nfseWebhookUrl, "URL NFS-e")}
                    disabled={!webhookSetup.nfseWebhookUrl}
                    className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Copiar
                  </button>
                </div>
                <p className="mt-2 text-xs text-slate-500">
                  Use para notificações de nota de serviço.
                </p>
              </div>
            </div>

            {Array.isArray(webhookSetup.warnings) && webhookSetup.warnings.length > 0 ? (
              <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                <strong className="text-sm font-semibold text-amber-700">Ajustes pendentes</strong>
                <ul className="mt-3 space-y-2 text-sm text-amber-700">
                  {webhookSetup.warnings.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            {Array.isArray(webhookSetup.actionsTaken) && webhookSetup.actionsTaken.length > 0 ? (
              <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-4">
                <strong className="text-sm font-semibold text-emerald-700">Ações aplicadas</strong>
                <ul className="mt-3 space-y-2 text-sm text-emerald-700">
                  {webhookSetup.actionsTaken.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            {Array.isArray(webhookSetup.nextSteps) && webhookSetup.nextSteps.length > 0 ? (
              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                <strong className="text-sm font-semibold text-slate-900">Próximos passos</strong>
                <ul className="mt-3 space-y-2 text-sm text-slate-600">
                  {webhookSetup.nextSteps.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
            ) : null}
          </div>
        ) : (
          <p className="text-sm text-slate-500">
            Não foi possível carregar o setup do webhook agora.
          </p>
        )}
      </section>

      {municipioValidation ? (
        <section
          ref={municipioValidationRef}
          className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm"
        >
          <div className="mb-4 border-b border-slate-100 pb-4">
            <h3 className="text-lg font-bold text-slate-900">Validação municipal da NFS-e</h3>
            <p className="mt-1 text-sm text-slate-500">
              Conferência rápida do município e dos campos críticos para emissão pela Focus.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Município</span>
              <p className="mt-2 text-sm text-slate-800">
                {municipioValidation.municipioNome || "-"}
                {municipioValidation.uf ? ` / ${municipioValidation.uf}` : ""}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                Código: {municipioValidation.municipioCodigo || "-"}
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Status NFSe</span>
              <p className="mt-2 text-sm text-slate-800">{municipioValidation.statusNfse || "-"}</p>
              <p className="mt-1 text-xs text-slate-500">
                {municipioValidation.remoteValidationAvailable
                  ? "Consulta remota concluída."
                  : "Somente validação local nesta checagem."}
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Prontidão</span>
              <p className="mt-2 text-sm text-slate-800">
                {municipioValidation.podeEmitirNfse ? "Pronta para emitir" : "Com pendências"}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                Provider: {municipioValidation.providerCode || "-"}
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">Item lista serviço</span>
              <p className="mt-2 text-sm text-slate-800">
                {municipioValidation.itemListaServicoConfigurado ? "Configurado" : "Pendente"}
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">CNAE principal</span>
              <p className="mt-2 text-sm text-slate-800">
                {municipioValidation.cnaePrincipalConfigurado ? "Configurado" : "Pendente"}
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Código tributário municipal
              </span>
              <p className="mt-2 text-sm text-slate-800">
                {municipioValidation.codigoTributarioMunicipioConfigurado ? "Configurado" : "Não configurado"}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                {municipioValidation.codigoTributarioMunicipioObrigatorio === true
                  ? "Obrigatório neste município."
                  : municipioValidation.codigoTributarioMunicipioObrigatorio === false
                    ? "Não obrigatório nesta checagem."
                    : "Obrigatoriedade não informada pela Focus."}
              </p>
            </div>
          </div>

          {Array.isArray(municipioValidation.errors) && municipioValidation.errors.length > 0 ? (
            <div className="mt-5 rounded-2xl border border-rose-200 bg-rose-50 p-4">
              <strong className="text-sm font-semibold text-rose-700">Pendências</strong>
              <ul className="mt-3 space-y-2 text-sm text-rose-700">
                {municipioValidation.errors.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          ) : null}

          {Array.isArray(municipioValidation.warnings) && municipioValidation.warnings.length > 0 ? (
            <div className="mt-4 rounded-2xl border border-amber-200 bg-amber-50 p-4">
              <strong className="text-sm font-semibold text-amber-700">Atenção</strong>
              <ul className="mt-3 space-y-2 text-sm text-amber-700">
                {municipioValidation.warnings.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          ) : null}
        </section>
      ) : null}
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
      let result: DocumentoFiscalEmitResult;

      if (tipoDocumento === "nfse") {
        result = await apiRequest<DocumentoFiscalEmitResult>(
          `/documentos-fiscais/nfse/emitir-por-os/${form.ordemServicoId}`,
          {
            method: "POST",
            body: {
              dataCompetencia: form.dataEmissao,
              observacoesNota: form.observacoesNota,
              gerarContaReceber: Boolean(form.gerarContaReceber),
            },
          },
        );
      } else {
        result = await apiRequest<DocumentoFiscalEmitResult>(
          `/documentos-fiscais/${tipoDocumento}/emitir-por-venda/${form.vendaId}`,
          {
            method: "POST",
            body: {
              dataEmissao: form.dataEmissao,
              observacoesNota: form.observacoesNota,
              gerarContaReceber: Boolean(form.gerarContaReceber),
              validarTributacaoCompleta: Boolean(form.validarTributacaoCompleta),
            },
          },
        );
      }

      setReloadKey((key) => key + 1);
      setEmissaoErrors({});

      const status = String(result.status ?? "").toLowerCase();
      if (status === "rejeitado") {
        setFailure(result.mensagemRejeicao ?? "Documento fiscal rejeitado pelo provedor.");
        return;
      }

      if (status === "autorizado") {
        setNotice("Documento fiscal autorizado.");
      } else if (status === "pendenteenvio") {
        setNotice("Documento fiscal enviado e aguardando autorização do provedor.");
      } else {
        setNotice("Documento fiscal enviado.");
      }
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

  async function baixarXml(row: ApiRecord) {
    try {
      const result = await apiDownload(`/documentos-fiscais/${row.id}/xml`);
      downloadBlob(
        result.blob,
        result.fileName ?? `documento-fiscal-${String(row.numero ?? "sem-numero")}.xml`,
      );
      setNotice("XML baixado com sucesso.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function imprimir(row: ApiRecord) {
    const popup = window.open("", "_blank", "width=960,height=760");
    if (!popup) {
      setFailure("Não foi possível abrir a janela de impressão.");
      return;
    }

    popup.document.write(
      "<html><body style=\"font-family:Arial,sans-serif;padding:24px\">Gerando impressão...</body></html>",
    );
    popup.document.close();

    try {
      const result = await apiRequest<DocumentoFiscalPrintData>(
        `/documentos-fiscais/${row.id}/impressao`,
      );

      const officialPdfUrl = apiAbsoluteResourceUrl(result.officialPdfUrl ?? "");
      if (officialPdfUrl) {
        popup.location.replace(officialPdfUrl);
        return;
      }

      renderFiscalPrint(popup, result);
    } catch (err) {
      popup.close();
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

  async function reenviarWebhook(row: ApiRecord) {
    try {
      const result = await apiRequest<DocumentoFiscalWebhookReplayResult>(
        `/documentos-fiscais/${row.id}/reenviar-webhook`,
        { method: "POST" },
      );

      setNotice(
        result.mensagem ||
          "Reenvio do webhook solicitado. Se o cadastro estiver certo, o status deve atualizar em instantes.",
      );
      setFailure("");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  return (
    <div className="space-y-6">
      {panelTitle(
        "Emissão e consulta",
        "Emita documentos em homologação para validar fluxo e use provider real quando a empresa estiver pronta.",
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
              Emitir documento
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
              {String(row.tipoDocumento ?? "").toLowerCase() !== "nfce" ? (
                <button
                  className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                  type="button"
                  onClick={() => void reenviarWebhook(row)}
                >
                  Reenviar webhook
                </button>
              ) : null}

              <button
                className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                type="button"
                onClick={() => void baixarXml(row)}
              >
                Baixar XML
              </button>

              <button
                className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                type="button"
                onClick={() => void imprimir(row)}
              >
                Imprimir
              </button>

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
