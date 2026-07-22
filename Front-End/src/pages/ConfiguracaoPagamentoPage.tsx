import { useEffect, useState } from "react";
import { CreditCard, RefreshCw, ShieldCheck } from "lucide-react";

import { Notice } from "../components/Ui";
import { PageHeader } from "../components/app/PageHeader";
import { PageSection } from "../components/app/PageSection";
import { apiRequest, ApiError } from "../lib/api";
import { errorMessage } from "../components/uiHelpers";

type PagamentoProviderInfo = {
  codigo: string;
  nome: string;
  implementado: boolean;
  suportaMaquininha: boolean;
  suportaPix: boolean;
};

type ConfiguracaoPagamento = {
  id?: string;
  provider: string;
  publicKey?: string | null;
  posId?: string | null;
  userIdExterno?: string | null;
  accessTokenConfigurado: boolean;
  suportaMaquininha: boolean;
  suportaPix: boolean;
  ativo: boolean;
  webhookUrl?: string | null;
};

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60";

function Field({ label, children, hint }: { label: string; children: React.ReactNode; hint?: string }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {hint ? <span className="mt-1 block text-xs text-slate-500">{hint}</span> : null}
    </label>
  );
}

function buttonClass() {
  return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";
}

export function ConfiguracaoPagamentoPage() {
  const [provedores, setProvedores] = useState<PagamentoProviderInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");

  const [provider, setProvider] = useState("");
  const [accessToken, setAccessToken] = useState("");
  const [publicKey, setPublicKey] = useState("");
  const [posId, setPosId] = useState("");
  const [userIdExterno, setUserIdExterno] = useState("");
  const [suportaMaquininha, setSuportaMaquininha] = useState(false);
  const [suportaPix, setSuportaPix] = useState(false);
  const [ativo, setAtivo] = useState(true);
  const [accessTokenConfigurado, setAccessTokenConfigurado] = useState(false);
  const [webhookUrl, setWebhookUrl] = useState("");

  useEffect(() => {
    void carregar();
  }, []);

  async function carregar() {
    setLoading(true);
    setFailure("");

    try {
      const listaProvedores = await apiRequest<PagamentoProviderInfo[]>("/configuracao-pagamento/provedores");
      setProvedores(listaProvedores);

      try {
        const config = await apiRequest<ConfiguracaoPagamento>("/configuracao-pagamento");
        setProvider(config.provider);
        setPublicKey(config.publicKey ?? "");
        setPosId(config.posId ?? "");
        setUserIdExterno(config.userIdExterno ?? "");
        setSuportaMaquininha(config.suportaMaquininha);
        setSuportaPix(config.suportaPix);
        setAtivo(config.ativo);
        setAccessTokenConfigurado(config.accessTokenConfigurado);
        setWebhookUrl(config.webhookUrl ?? "");
      } catch (err) {
        if (!(err instanceof ApiError && err.status === 404)) throw err;
        if (listaProvedores.length > 0) setProvider(listaProvedores[0].codigo);
      }
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  async function salvar() {
    setNotice("");
    setFailure("");
    setSaving(true);

    try {
      const config = await apiRequest<ConfiguracaoPagamento>("/configuracao-pagamento", {
        method: "PUT",
        body: {
          provider,
          accessToken: accessToken.trim() || null,
          publicKey: publicKey.trim() || null,
          posId: posId.trim() || null,
          userIdExterno: userIdExterno.trim() || null,
          suportaMaquininha,
          suportaPix,
          ativo,
        },
      });

      setAccessToken("");
      setAccessTokenConfigurado(config.accessTokenConfigurado);
      setWebhookUrl(config.webhookUrl ?? "");
      setNotice("Configuração de pagamento salva com sucesso.");
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  const provedorSelecionado = provedores.find((p) => p.codigo === provider);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Configuração"
        title="Pagamento / Maquininha"
        description="Configure o provedor de pagamento desta loja para cobrar via Pix ou acionar a maquininha direto do sistema."
        actions={
          <button type="button" className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50" onClick={carregar}>
            <RefreshCw size={16} />
            Atualizar
          </button>
        }
      />

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure ? <Notice type="error">{failure}</Notice> : null}

      <PageSection
        title="Provedor"
        description="Cada loja escolhe seu próprio provedor de pagamento. Marcas sem integração pronta aparecem como 'em breve'."
      >
        {loading ? (
          <p className="text-sm text-slate-500">Carregando...</p>
        ) : (
          <div className="space-y-5">
            <Field label="Provedor de pagamento">
              <select className={inputClass} value={provider} onChange={(event) => setProvider(event.target.value)}>
                <option value="" disabled>
                  Selecione...
                </option>
                {provedores.map((p) => (
                  <option key={p.codigo} value={p.codigo} disabled={!p.implementado}>
                    {p.nome}
                    {!p.implementado ? " (em breve)" : ""}
                  </option>
                ))}
              </select>
            </Field>

            {provedorSelecionado && !provedorSelecionado.implementado ? (
              <Notice type="info">
                A integração com {provedorSelecionado.nome} ainda não está disponível. Selecione outro
                provedor por enquanto.
              </Notice>
            ) : null}

            <div className="grid gap-4 md:grid-cols-2">
              <Field
                label="Access Token"
                hint={accessTokenConfigurado ? "Já configurado - preencha só para trocar." : "Token de acesso da API do provedor."}
              >
                <input
                  className={inputClass}
                  type="password"
                  autoComplete="off"
                  value={accessToken}
                  onChange={(event) => setAccessToken(event.target.value)}
                  placeholder={accessTokenConfigurado ? "•••••••• (configurado)" : "Cole aqui o access token"}
                />
              </Field>

              <Field label="Public Key" hint="Opcional, usada por alguns provedores no checkout.">
                <input className={inputClass} value={publicKey} onChange={(event) => setPublicKey(event.target.value)} />
              </Field>

              <Field label="ID do dispositivo (maquininha)" hint="Necessário para acionar a maquininha (Point Smart etc.).">
                <input className={inputClass} value={posId} onChange={(event) => setPosId(event.target.value)} />
              </Field>

              <Field label="ID de usuário/coletor" hint="Opcional, conforme exigido pelo provedor.">
                <input
                  className={inputClass}
                  value={userIdExterno}
                  onChange={(event) => setUserIdExterno(event.target.value)}
                />
              </Field>
            </div>

            <div className="flex flex-wrap gap-6">
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={suportaMaquininha}
                  disabled={!provedorSelecionado?.suportaMaquininha}
                  onChange={(event) => setSuportaMaquininha(event.target.checked)}
                />
                Cobrar na maquininha
              </label>

              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={suportaPix}
                  disabled={!provedorSelecionado?.suportaPix}
                  onChange={(event) => setSuportaPix(event.target.checked)}
                />
                Cobrar via Pix (QR Code)
              </label>

              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input type="checkbox" checked={ativo} onChange={(event) => setAtivo(event.target.checked)} />
                Ativo
              </label>
            </div>

            {webhookUrl ? (
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <span className="flex items-center gap-2 text-sm font-medium text-slate-700">
                  <ShieldCheck size={16} />
                  URL de notificação (webhook)
                </span>
                <p className="mt-1 break-all text-xs text-slate-500">
                  Cadastre esta URL no painel do provedor para receber a confirmação automática do pagamento:
                </p>
                <code className="mt-1 block break-all rounded-xl bg-white px-3 py-2 text-xs text-slate-700">
                  {webhookUrl}
                </code>
              </div>
            ) : null}

            <div className="flex justify-end">
              <button type="button" className={buttonClass()} disabled={saving || !provider} onClick={() => void salvar()}>
                <CreditCard size={16} />
                {saving ? "Salvando..." : "Salvar configuração"}
              </button>
            </div>
          </div>
        )}
      </PageSection>
    </div>
  );
}
