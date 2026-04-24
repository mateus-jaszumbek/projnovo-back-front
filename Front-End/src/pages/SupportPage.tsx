import { useEffect, useMemo, useState } from "react";
import {
  Copy,
  LifeBuoy,
  Mail,
  MessageCircle,
  SendHorizontal,
  Sparkles,
} from "lucide-react";

import { useAuth } from "../auth/AuthContext";
import { Notice, PageFrame } from "../components/Ui";
import { PageSection } from "../components/app/PageSection";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import {
  buildSupportWhatsAppUrl,
  formatSupportPhone,
  resolveSupportEmail,
  resolveSupportPhone,
} from "../support/supportContact";

const inputClass =
  "h-11 w-full rounded-lg border border-emerald-200/70 bg-white/95 px-3.5 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80";

const textareaClass =
  "min-h-[180px] w-full rounded-lg border border-emerald-200/70 bg-white/95 px-3.5 py-3 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80 resize-y";

const primaryButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";

const secondaryButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-lg border border-emerald-200/70 bg-white/92 px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm transition hover:border-emerald-300 hover:bg-emerald-50/70 hover:text-slate-950 disabled:cursor-not-allowed disabled:opacity-60";

const topicOptions = [
  { value: "ajuda", label: "Preciso de ajuda" },
  { value: "melhoria", label: "Quero sugerir uma melhoria" },
  { value: "bug", label: "Encontrei um problema" },
  { value: "outro", label: "Outro assunto" },
];

function topicLabel(value: string) {
  return topicOptions.find((item) => item.value === value)?.label ?? "Preciso de ajuda";
}

export function SupportPage() {
  const { session } = useAuth();
  const [company, setCompany] = useState<ApiRecord | null>(null);
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [form, setForm] = useState({
    nome: session?.nome ?? "",
    email: session?.email ?? "",
    assunto: "ajuda",
    mensagem: "",
  });

  useEffect(() => {
    let active = true;

    async function loadCompany() {
      if (!session?.token) {
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

    void loadCompany();

    return () => {
      active = false;
    };
  }, [session?.empresaId, session?.token]);

  const supportEmail = resolveSupportEmail(company);
  const supportPhone = resolveSupportPhone(company);
  const supportPhoneLabel = formatSupportPhone(supportPhone);

  const composedMessage = useMemo(() => {
    const companyName = String(
      company?.nomeFantasia ?? session?.empresaNomeFantasia ?? "minha empresa",
    );

    return [
      `Olá, equipe de suporte!`,
      ``,
      `Assunto: ${topicLabel(form.assunto)}`,
      `Empresa: ${companyName}`,
      `Nome: ${form.nome.trim() || "Não informado"}`,
      `E-mail: ${form.email.trim() || "Não informado"}`,
      ``,
      form.mensagem.trim() || "Escreva aqui a sua mensagem.",
    ].join("\n");
  }, [
    company?.nomeFantasia,
    form.assunto,
    form.email,
    form.mensagem,
    form.nome,
    session?.empresaNomeFantasia,
  ]);

  function updateField(name: "nome" | "email" | "assunto" | "mensagem", value: string) {
    setForm((current) => ({ ...current, [name]: value }));
    setNotice("");
    setFailure("");
  }

  function validateBeforeSend() {
    if (!form.mensagem.trim()) {
      setFailure("Escreva a mensagem que você quer enviar para o suporte.");
      return false;
    }

    return true;
  }

  async function copyMessage() {
    if (!validateBeforeSend()) return;

    try {
      await navigator.clipboard.writeText(composedMessage);
      setNotice("Mensagem copiada. Agora você pode colar onde preferir.");
    } catch {
      setFailure("Não foi possível copiar a mensagem agora.");
    }
  }

  function sendEmail() {
    if (!validateBeforeSend()) return;

    const subject = encodeURIComponent(
      `${topicLabel(form.assunto)} - ${session?.empresaNomeFantasia ?? "Suporte"}`,
    );
    const body = encodeURIComponent(composedMessage);
    window.location.href = `mailto:${supportEmail}?subject=${subject}&body=${body}`;
  }

  function sendWhatsApp() {
    if (!validateBeforeSend()) return;

    const url = buildSupportWhatsAppUrl(supportPhone, composedMessage);
    if (!url) {
      setFailure("Cadastre um número de suporte para abrir o WhatsApp.");
      return;
    }

    window.open(url, "_blank", "noopener,noreferrer");
  }

  return (
    <PageFrame
      eyebrow="Ajuda"
      title="Suporte e melhorias"
      description="Abra uma conversa com o suporte ou envie uma sugestão de melhoria sem sair do sistema."
      actions={
        <button
          type="button"
          className={secondaryButtonClass}
          onClick={copyMessage}
        >
          <Copy size={16} />
          Copiar mensagem
        </button>
      }
    >
      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure ? <Notice type="error">{failure}</Notice> : null}

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <PageSection
          title="Envie sua mensagem"
          description="Conte o que você precisa. O texto fica pronto para mandar por WhatsApp ou e-mail."
        >
          <div className="grid gap-4 md:grid-cols-2">
            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Seu nome</span>
              <input
                className={inputClass}
                value={form.nome}
                placeholder="Como você quer ser identificado"
                onChange={(event) => updateField("nome", event.target.value)}
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Seu e-mail</span>
              <input
                className={inputClass}
                type="email"
                value={form.email}
                placeholder="voce@empresa.com"
                onChange={(event) => updateField("email", event.target.value)}
              />
            </label>

            <label className="block md:col-span-2">
              <span className="mb-2 block text-sm font-medium text-slate-700">Assunto</span>
              <select
                className={inputClass}
                value={form.assunto}
                onChange={(event) => updateField("assunto", event.target.value)}
              >
                {topicOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="block md:col-span-2">
              <span className="mb-2 block text-sm font-medium text-slate-700">Mensagem</span>
              <textarea
                className={textareaClass}
                value={form.mensagem}
                placeholder="Descreva sua dúvida, sugestão ou o que você gostaria de melhorar."
                onChange={(event) => updateField("mensagem", event.target.value)}
              />
            </label>
          </div>

          <div className="mt-5 flex flex-wrap gap-2">
            <button type="button" className={primaryButtonClass} onClick={sendWhatsApp}>
              <MessageCircle size={16} />
              Enviar no WhatsApp
            </button>

            <button type="button" className={secondaryButtonClass} onClick={sendEmail}>
              <Mail size={16} />
              Enviar por e-mail
            </button>
          </div>
        </PageSection>

        <PageSection
          title="Canais de atendimento"
          description="Escolha o caminho mais rápido para falar com a equipe."
        >
          <div className="space-y-4">
            <div className="border-b border-emerald-100/80 pb-4">
              <div className="flex items-start gap-3">
                <div className="mt-0.5 text-emerald-600">
                  <MessageCircle size={18} />
                </div>
                <div>
                  <strong className="block text-sm text-slate-950">WhatsApp do suporte</strong>
                  <p className="mt-1 text-sm leading-6 text-slate-600">
                    {supportPhoneLabel || "Cadastre um telefone da empresa ou o WhatsApp de suporte para abrir a conversa aqui."}
                  </p>
                </div>
              </div>
            </div>

            <div className="border-b border-emerald-100/80 pb-4">
              <div className="flex items-start gap-3">
                <div className="mt-0.5 text-emerald-600">
                  <Mail size={18} />
                </div>
                <div>
                  <strong className="block text-sm text-slate-950">E-mail de atendimento</strong>
                  <p className="mt-1 text-sm leading-6 text-slate-600">{supportEmail}</p>
                </div>
              </div>
            </div>

            <div className="border-b border-emerald-100/80 pb-4">
              <div className="flex items-start gap-3">
                <div className="mt-0.5 text-emerald-600">
                  <LifeBuoy size={18} />
                </div>
                <div>
                  <strong className="block text-sm text-slate-950">Quando usar esta página</strong>
                  <p className="mt-1 text-sm leading-6 text-slate-600">
                    Dúvidas do dia a dia, ideias de melhoria, relatos de erro e pedidos de ajuste.
                  </p>
                </div>
              </div>
            </div>

            <div>
              <div className="flex items-start gap-3">
                <div className="mt-0.5 text-emerald-600">
                  <Sparkles size={18} />
                </div>
                <div>
                  <strong className="block text-sm text-slate-950">Mensagem pronta</strong>
                  <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-slate-600">
                    {composedMessage}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="mt-5 flex flex-wrap gap-2">
            <button type="button" className={secondaryButtonClass} onClick={copyMessage}>
              <Copy size={16} />
              Copiar texto
            </button>

            <button type="button" className={secondaryButtonClass} onClick={sendWhatsApp}>
              <SendHorizontal size={16} />
              Abrir conversa
            </button>
          </div>
        </PageSection>
      </div>
    </PageFrame>
  );
}
