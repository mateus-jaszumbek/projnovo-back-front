import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { Building2, ImageUp, RotateCcw, Save, Trash2 } from "lucide-react";

import { useAuth } from "../auth/AuthContext";
import { FieldRenderer, Notice, PageFrame } from "../components/Ui";
import type { FieldConfig } from "../components/Ui";
import { PageSection } from "../components/app/PageSection";
import {
  defaultForm,
  errorMessage,
  formFromRecord,
  formatFieldInput,
  onlyDigits,
  payloadFromForm,
  validateForm,
} from "../components/uiHelpers";
import { apiRequest, apiResourceUrl, apiUpload, notifyCompanyUpdated } from "../lib/api";
import { lookupAddressByCep } from "../lib/cep";
import type { ApiRecord } from "../lib/api";

type EmpresaRecord = {
  id?: string;
  razaoSocial?: string;
  nomeFantasia?: string;
  cnpj?: string;
  inscricaoEstadual?: string | null;
  inscricaoMunicipal?: string | null;
  regimeTributario?: string | null;
  email?: string | null;
  telefone?: string | null;
  cep?: string | null;
  logradouro?: string | null;
  numero?: string | null;
  complemento?: string | null;
  bairro?: string | null;
  cidade?: string | null;
  uf?: string | null;
  logoUrl?: string | null;
};

const identityFields: FieldConfig[] = [
  {
    name: "razaoSocial",
    label: "Razao social",
    required: true,
    minLength: 3,
    maxLength: 150,
    span: "full",
  },
  {
    name: "nomeFantasia",
    label: "Nome fantasia",
    required: true,
    minLength: 2,
    maxLength: 150,
  },
  {
    name: "regimeTributario",
    label: "Regime tributario",
    type: "select",
    required: true,
    options: [
      { value: "SimplesNacional", label: "Simples Nacional" },
      { value: "MEI", label: "MEI" },
      { value: "LucroPresumido", label: "Lucro Presumido" },
      { value: "LucroReal", label: "Lucro Real" },
    ],
    defaultValue: "SimplesNacional",
  },
  {
    name: "inscricaoEstadual",
    label: "Inscricao estadual",
    maxLength: 20,
  },
  {
    name: "inscricaoMunicipal",
    label: "Inscricao municipal",
    maxLength: 20,
  },
];

const contactFields: FieldConfig[] = [
  {
    name: "email",
    label: "E-mail da empresa",
    type: "email",
    maxLength: 150,
  },
  {
    name: "telefone",
    label: "Telefone",
    mask: "phone",
  },
  {
    name: "cep",
    label: "CEP",
    mask: "cep",
    helper: "Ao completar o CEP, buscamos o endereco automaticamente.",
  },
  {
    name: "logradouro",
    label: "Logradouro",
    span: "full",
    maxLength: 150,
  },
  {
    name: "numero",
    label: "Numero",
    maxLength: 20,
  },
  {
    name: "complemento",
    label: "Complemento",
    maxLength: 100,
  },
  {
    name: "bairro",
    label: "Bairro",
    maxLength: 100,
  },
  {
    name: "cidade",
    label: "Cidade",
    maxLength: 100,
  },
  {
    name: "uf",
    label: "UF",
    mask: "uf",
  },
];

const allFields = [...identityFields, ...contactFields];

const buttonClass =
  "inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-60";

const inputClass =
  "h-11 w-full rounded-lg border border-emerald-200/70 bg-white/95 px-3.5 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80";

function buildReadonlyCnpj(value: string | null | undefined) {
  return formatFieldInput(
    { name: "cnpj", label: "CNPJ", mask: "cnpj" },
    String(value ?? ""),
  );
}

export function EmpresaPage() {
  const { session } = useAuth();
  const [company, setCompany] = useState<EmpresaRecord | null>(null);
  const [form, setForm] = useState<ApiRecord>(() => defaultForm(allFields));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [logoLoading, setLogoLoading] = useState(false);
  const [cepLookupLoading, setCepLookupLoading] = useState(false);
  const [cepLookupMessage, setCepLookupMessage] = useState("");
  const [logoUrlInput, setLogoUrlInput] = useState("");
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");

  const userRole = String(session?.perfil ?? "").toLowerCase();
  const canEdit = Boolean(
    session?.isSuperAdmin ||
      ["owner", "admin", "administrador", "super-admin", "superadmin"].includes(userRole),
  );

  const identityFieldsWithState = useMemo(
    () => identityFields.map((field) => ({ ...field, disabled: !canEdit || loading })),
    [canEdit, loading],
  );

  const contactFieldsWithState = useMemo(
    () => contactFields.map((field) => ({ ...field, disabled: !canEdit || loading })),
    [canEdit, loading],
  );

  async function loadCompany() {
    setLoading(true);

    try {
      const result = await apiRequest<EmpresaRecord>("/empresas/minha");
      setCompany(result);
      setForm(formFromRecord(allFields, result as ApiRecord));
      setErrors({});
      setCepLookupMessage("");
      setFailure("");
    } catch (err) {
      setFailure(errorMessage(err));
      setCompany(null);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadCompany();
  }, []);

  function updateField(name: string, value: unknown) {
    setForm((current) => ({ ...current, [name]: value }));
    setErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
    if (name === "cep") {
      setCepLookupMessage("");
    }
    setNotice("");
    setFailure("");

    const field = allFields.find((item) => item.name === name);
    if (field?.mask === "cep" && onlyDigits(value).length === 8) {
      void preencherEnderecoPorCep(value);
    }
  }

  async function preencherEnderecoPorCep(value: unknown) {
    const cep = onlyDigits(value);
    if (cep.length !== 8) {
      setErrors((current) => ({
        ...current,
        cep: "Informe um CEP com 8 digitos.",
      }));
      setCepLookupMessage("");
      return;
    }

    setCepLookupLoading(true);
    setCepLookupMessage("");

    try {
      const data = await lookupAddressByCep(cep);

      setErrors((current) => {
        const next = { ...current };
        delete next.cep;
        return next;
      });

      setForm((current) => ({
        ...current,
        logradouro: data.logradouro ?? current.logradouro,
        complemento: data.complemento ?? current.complemento,
        bairro: data.bairro ?? current.bairro,
        cidade: data.cidade ?? current.cidade,
        uf: data.uf ?? current.uf,
      }));
      setCepLookupMessage("Endereco preenchido a partir do CEP.");
    } catch (error) {
      setErrors((current) => ({
        ...current,
        cep: error instanceof Error ? error.message : "Nao foi possivel consultar o CEP agora.",
      }));
      setCepLookupMessage("");
    } finally {
      setCepLookupLoading(false);
    }
  }

  async function saveCompany(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canEdit) return;

    const validation = validateForm(allFields, form);
    setErrors(validation);

    if (Object.keys(validation).length > 0) {
      setFailure("Corrija os campos destacados antes de salvar.");
      setNotice("");
      return;
    }

    setSaving(true);
    setNotice("");
    setFailure("");

    try {
      const result = await apiRequest<EmpresaRecord>("/empresas/minha", {
        method: "PUT",
        body: payloadFromForm(allFields, form),
      });

      setCompany(result);
      setForm(formFromRecord(allFields, result as ApiRecord));
      setErrors({});
      setNotice("Dados da empresa atualizados com sucesso.");
      notifyCompanyUpdated();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function uploadLogo(file?: File | null) {
    if (!file || !canEdit) return;

    setLogoLoading(true);
    setNotice("");
    setFailure("");

    try {
      const formData = new FormData();
      formData.append("arquivo", file);
      const result = await apiUpload<EmpresaRecord>("/empresas/minha/logo", formData);
      setCompany(result);
      setLogoUrlInput("");
      setNotice("Logo atualizada com sucesso.");
      notifyCompanyUpdated();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLogoLoading(false);
    }
  }

  async function uploadLogoByUrl() {
    if (!canEdit) return;

    const url = logoUrlInput.trim();
    if (!url) {
      setFailure("Informe a URL da logo antes de continuar.");
      setNotice("");
      return;
    }

    setLogoLoading(true);
    setNotice("");
    setFailure("");

    try {
      const formData = new FormData();
      formData.append("url", url);
      const result = await apiUpload<EmpresaRecord>("/empresas/minha/logo", formData);
      setCompany(result);
      setLogoUrlInput("");
      setNotice("Logo atualizada com sucesso.");
      notifyCompanyUpdated();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLogoLoading(false);
    }
  }

  async function removeLogo() {
    if (!canEdit) return;

    setLogoLoading(true);
    setNotice("");
    setFailure("");

    try {
      await apiRequest("/empresas/minha/logo", { method: "DELETE" });
      setCompany((current) => (current ? { ...current, logoUrl: null } : current));
      setNotice("Logo removida com sucesso.");
      notifyCompanyUpdated();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLogoLoading(false);
    }
  }

  return (
    <PageFrame
      eyebrow="Empresa"
      title="Dados da empresa"
      description="Revise e atualize os dados da sua empresa. O CNPJ fica bloqueado para preservar o historico fiscal e cadastral."
      actions={
        canEdit ? (
          <>
            <button
              type="button"
              className={`${buttonClass} border border-emerald-200/70 bg-white text-slate-700 hover:bg-emerald-50`}
              disabled={loading || saving}
              onClick={() => void loadCompany()}
            >
              <RotateCcw size={16} />
              Recarregar
            </button>
            <button
              type="submit"
              form="empresa-profile-form"
              className={`${buttonClass} bg-slate-900 text-white hover:bg-slate-800`}
              disabled={loading || saving}
            >
              <Save size={16} />
              {saving ? "Salvando..." : "Salvar dados"}
            </button>
          </>
        ) : null
      }
    >
      {!canEdit ? (
        <Notice type="info">
          Voce pode consultar os dados da empresa aqui. Para editar, entre com um perfil owner, admin ou superadmin.
        </Notice>
      ) : null}

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure ? <Notice type="error">{failure}</Notice> : null}

      <PageSection
        title="Logo e identificacao"
        description="A logo aparece no sistema, em impressos e em telas publicas vinculadas a sua empresa."
        actions={
          canEdit ? (
            <>
              <label
                className={`${buttonClass} cursor-pointer border border-emerald-200/70 bg-white text-slate-700 hover:bg-emerald-50`}
              >
                <ImageUp size={16} />
                {logoLoading ? "Enviando..." : "Enviar logo"}
                <input
                  className="sr-only"
                  type="file"
                  accept="image/png,image/jpeg,image/webp,image/svg+xml"
                  disabled={logoLoading}
                  onChange={(event) => {
                    void uploadLogo(event.target.files?.[0]);
                    event.target.value = "";
                  }}
                />
              </label>

              {company?.logoUrl ? (
                <button
                  type="button"
                  className={`${buttonClass} border border-rose-200 bg-rose-50 text-rose-700 hover:bg-rose-100`}
                  disabled={logoLoading}
                  onClick={() => void removeLogo()}
                >
                  <Trash2 size={16} />
                  Remover
                </button>
              ) : null}
            </>
          ) : null
        }
      >
        <div className="grid gap-5 lg:grid-cols-[auto_minmax(0,1fr)]">
          <div className="flex h-28 w-28 items-center justify-center overflow-hidden rounded-lg border border-slate-200 bg-slate-50">
            {company?.logoUrl ? (
              <img
                src={apiResourceUrl(String(company.logoUrl))}
                alt="Logo da empresa"
                className="h-full w-full object-contain p-2"
              />
            ) : (
              <Building2 size={28} className="text-slate-400" />
            )}
          </div>

          <div className="space-y-4">
            <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3">
              <span className="block text-xs font-medium uppercase tracking-wide text-slate-500">
                CNPJ
              </span>
              <strong className="mt-1 block text-sm text-slate-950">
                {buildReadonlyCnpj(company?.cnpj)}
              </strong>
              <p className="mt-2 text-xs leading-5 text-slate-500">
                O CNPJ nao pode ser alterado nesta tela para evitar impacto em dados fiscais, cadastros e historico da empresa.
              </p>
            </div>

            {canEdit ? (
              <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto]">
                <input
                  className={inputClass}
                  value={logoUrlInput}
                  placeholder="Ou cole a URL da logo"
                  onChange={(event) => setLogoUrlInput(event.target.value)}
                />
                <button
                  type="button"
                  className={`${buttonClass} border border-emerald-200/70 bg-white text-slate-700 hover:bg-emerald-50`}
                  disabled={logoLoading}
                  onClick={() => void uploadLogoByUrl()}
                >
                  <ImageUp size={16} />
                  Usar URL
                </button>
              </div>
            ) : null}
          </div>
        </div>
      </PageSection>

      <form id="empresa-profile-form" className="space-y-6" onSubmit={saveCompany}>
        <PageSection
          title="Identidade fiscal"
          description="Dados principais da empresa, sem alterar o CNPJ."
        >
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {identityFieldsWithState.map((field) => (
              <FieldRenderer
                key={field.name}
                field={field}
                value={form[field.name]}
                error={errors[field.name]}
                onChange={updateField}
              />
            ))}
          </div>
        </PageSection>

        <PageSection
          title="Contato e endereco"
          description="Mantenha e-mail, telefone, cidade e endereco sempre atualizados para operacao e impressos."
        >
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {contactFieldsWithState.map((field) => (
              <div
                key={field.name}
                className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}
              >
                <FieldRenderer
                  field={field}
                  value={form[field.name]}
                  error={errors[field.name]}
                  onChange={updateField}
                />
                {field.mask === "cep" ? (
                  <div className="mt-2 flex flex-col gap-2">
                    <button
                      type="button"
                      className="inline-flex w-fit items-center justify-center rounded-2xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                      disabled={cepLookupLoading}
                      onClick={() => void preencherEnderecoPorCep(form[field.name])}
                    >
                      {cepLookupLoading ? "Consultando CEP..." : "Preencher endereco"}
                    </button>
                    {cepLookupMessage ? (
                      <small className="text-xs text-slate-500">{cepLookupMessage}</small>
                    ) : null}
                  </div>
                ) : null}
              </div>
            ))}
          </div>
        </PageSection>
      </form>
    </PageFrame>
  );
}
