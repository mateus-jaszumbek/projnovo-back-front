import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import {
  Check,
  Copy,
  MessageCircle,
  Package,
  Sparkles,
  Smartphone,
  Store,
} from "lucide-react";

import { Notice } from "../components/Ui";
import { apiRequest } from "../lib/api";

type ClientePortalOrdemServicoDto = {
  numeroOs: number;
  aparelhoDescricao: string;
  status: string;
  defeitoRelatado: string;
  dataEntrada: string;
  dataPrevisao?: string | null;
  dataEntrega?: string | null;
  valorTotal: number;
  garantiaDias: number;
  dataVencimentoGarantia?: string | null;
  situacaoGarantia: string;
};

type ClientePortalDto = {
  clienteNome: string;
  ehLojista: boolean;
  empresaLogoUrl?: string | null;
  ordensServico: ClientePortalOrdemServicoDto[];
};

function formatMoney(value?: number | null) {
  if (value == null) return "—";
  return value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function formatDate(value?: string | null) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleDateString("pt-BR");
}

function statusLabel(status: string) {
  switch (status) {
    case "ABERTA":
      return "Aberta";
    case "APROVADA":
      return "Aprovada";
    case "EM_ANDAMENTO":
      return "Em andamento";
    case "PRONTA":
      return "Pronta para retirada";
    case "ENTREGUE":
      return "Entregue";
    case "CANCELADA":
      return "Cancelada";
    default:
      return status;
  }
}

function statusToneClass(status: string) {
  switch (status) {
    case "ABERTA":
      return "border-sky-200 bg-sky-50 text-sky-700";
    case "APROVADA":
      return "border-amber-200 bg-amber-50 text-amber-700";
    case "EM_ANDAMENTO":
      return "border-violet-200 bg-violet-50 text-violet-700";
    case "PRONTA":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";
    case "ENTREGUE":
      return "border-slate-200 bg-slate-100 text-slate-700";
    case "CANCELADA":
      return "border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border-slate-200 bg-slate-100 text-slate-700";
  }
}

function garantiaLabel(situacao: string) {
  switch (situacao) {
    case "VALIDA":
      return "Garantia válida";
    case "EXPIRADA":
      return "Garantia expirada";
    case "AGUARDANDO_ENTREGA":
      return "Garantia inicia na entrega";
    default:
      return "Sem garantia";
  }
}

function garantiaToneClass(situacao: string) {
  switch (situacao) {
    case "VALIDA":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";
    case "EXPIRADA":
      return "border-rose-200 bg-rose-50 text-rose-700";
    case "AGUARDANDO_ENTREGA":
      return "border-amber-200 bg-amber-50 text-amber-700";
    default:
      return "border-slate-200 bg-slate-100 text-slate-700";
  }
}

function buttonClass(variant: "primary" | "secondary" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200/80 bg-white/90 px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm backdrop-blur transition hover:bg-white";
}

function sectionCardClass() {
  return "rounded-[28px] border border-white/70 bg-white/85 shadow-[0_10px_40px_rgba(15,23,42,0.08)] backdrop-blur";
}

export function ClientePortalPublicoPage() {
  const { token = "" } = useParams<{ token: string }>();

  const [data, setData] = useState<ClientePortalDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [failure, setFailure] = useState("");
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let active = true;

    async function load() {
      if (!token.trim()) {
        setFailure("Link do portal inválido.");
        setLoading(false);
        return;
      }

      setLoading(true);
      setFailure("");

      try {
        const result = await apiRequest<ClientePortalDto>(`/public/clientes/${token}/portal`, {
          method: "GET",
        });

        if (!active) return;
        setData(result);
      } catch (error) {
        if (!active) return;
        setFailure(error instanceof Error ? error.message : "Não foi possível carregar o portal.");
      } finally {
        if (active) setLoading(false);
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, [token]);

  useEffect(() => {
    if (!copied) return;
    const timer = setTimeout(() => setCopied(false), 1800);
    return () => clearTimeout(timer);
  }, [copied]);

  const publicUrl = useMemo(() => {
    if (!token) return "";
    return `${window.location.origin}/portal/${token}`;
  }, [token]);

  async function copyLink() {
    if (!publicUrl) return;

    try {
      await navigator.clipboard.writeText(publicUrl);
      setCopied(true);
    } catch {
      setFailure("Não foi possível copiar o link.");
    }
  }

  function openWhatsApp() {
    if (!data) return;

    const texto = [
      `Olá!`,
      `Você pode acompanhar todos os seus aparelhos por este link:`,
      publicUrl,
    ].join("\n");

    window.open(`https://wa.me/?text=${encodeURIComponent(texto)}`, "_blank", "noopener,noreferrer");
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(148,163,184,0.18),_transparent_35%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-10">
        <div className="mx-auto max-w-5xl">
          <div className={`${sectionCardClass()} p-12 text-center`}>
            <div className="mx-auto mb-4 h-12 w-12 animate-pulse rounded-2xl bg-slate-200" />
            <p className="text-sm font-medium text-slate-500">Carregando portal...</p>
          </div>
        </div>
      </div>
    );
  }

  if (failure || !data) {
    return (
      <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(148,163,184,0.18),_transparent_35%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-10">
        <div className="mx-auto max-w-5xl space-y-4">
          <Notice type="error">{failure || "Portal não encontrado."}</Notice>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(148,163,184,0.18),_transparent_35%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-8">
      <div className="mx-auto max-w-5xl space-y-6">
        <header className="relative overflow-hidden rounded-[32px] border border-white/70 bg-[linear-gradient(135deg,rgba(15,23,42,1)_0%,rgba(30,41,59,0.96)_40%,rgba(51,65,85,0.92)_100%)] p-6 text-white shadow-[0_20px_60px_rgba(15,23,42,0.24)]">
          <div className="pointer-events-none absolute -right-12 -top-12 h-44 w-44 rounded-full bg-white/10 blur-2xl" />

          <div className="relative flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
            <div className="max-w-2xl">
              <span className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.14em] text-slate-200">
                {data.ehLojista ? <Store size={13} /> : <Sparkles size={13} />}
                {data.ehLojista ? "Portal do lojista" : "Portal de acompanhamento"}
              </span>

              <h1 className="mt-4 text-3xl font-bold tracking-tight md:text-4xl">
                {data.clienteNome}
              </h1>

              <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-300">
                Acompanhe todos os aparelhos entregues para reparo, com status e garantia de cada um, num único link.
              </p>

              <div className="mt-5 flex flex-wrap items-center gap-3">
                <span className="inline-flex items-center gap-2 rounded-2xl border border-white/10 bg-white/10 px-3 py-2 text-sm font-medium text-slate-200">
                  {data.ordensServico.length} {data.ordensServico.length === 1 ? "aparelho" : "aparelhos"}
                </span>
              </div>
            </div>

            <div className="flex flex-wrap gap-2">
              <button className={buttonClass()} type="button" onClick={copyLink}>
                {copied ? <Check size={16} /> : <Copy size={16} />}
                {copied ? "Link copiado" : "Copiar link"}
              </button>

              <button className={buttonClass("primary")} type="button" onClick={openWhatsApp}>
                <MessageCircle size={16} />
                Compartilhar no WhatsApp
              </button>
            </div>
          </div>
        </header>

        <section className="space-y-4">
          {data.ordensServico.length === 0 ? (
            <div className={`${sectionCardClass()} p-10 text-center`}>
              <Package size={28} className="mx-auto text-slate-400" />
              <p className="mt-3 text-sm text-slate-500">Nenhuma ordem de serviço encontrada ainda.</p>
            </div>
          ) : (
            data.ordensServico.map((os) => (
              <article key={os.numeroOs} className={`${sectionCardClass()} p-6`}>
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div className="flex items-start gap-3">
                    <div className="rounded-2xl bg-slate-100 p-3 text-slate-600">
                      <Smartphone size={20} />
                    </div>
                    <div>
                      <strong className="block text-lg text-slate-900">
                        OS #{os.numeroOs} - {os.aparelhoDescricao || "Aparelho"}
                      </strong>
                      <p className="mt-1 text-sm text-slate-500">{os.defeitoRelatado || "—"}</p>
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <span
                      className={[
                        "inline-flex rounded-full border px-3 py-1 text-xs font-semibold",
                        statusToneClass(os.status),
                      ].join(" ")}
                    >
                      {statusLabel(os.status)}
                    </span>
                    <span
                      className={[
                        "inline-flex rounded-full border px-3 py-1 text-xs font-semibold",
                        garantiaToneClass(os.situacaoGarantia),
                      ].join(" ")}
                    >
                      {garantiaLabel(os.situacaoGarantia)}
                    </span>
                  </div>
                </div>

                <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                  <div className="rounded-2xl bg-slate-50 px-4 py-3">
                    <span className="block text-xs font-semibold uppercase tracking-[0.1em] text-slate-400">
                      Entrada
                    </span>
                    <strong className="mt-1 block text-sm text-slate-900">{formatDate(os.dataEntrada)}</strong>
                  </div>
                  <div className="rounded-2xl bg-slate-50 px-4 py-3">
                    <span className="block text-xs font-semibold uppercase tracking-[0.1em] text-slate-400">
                      Entrega
                    </span>
                    <strong className="mt-1 block text-sm text-slate-900">{formatDate(os.dataEntrega)}</strong>
                  </div>
                  <div className="rounded-2xl bg-slate-50 px-4 py-3">
                    <span className="block text-xs font-semibold uppercase tracking-[0.1em] text-slate-400">
                      Valor
                    </span>
                    <strong className="mt-1 block text-sm text-slate-900">{formatMoney(os.valorTotal)}</strong>
                  </div>
                  <div className="rounded-2xl bg-slate-50 px-4 py-3">
                    <span className="block text-xs font-semibold uppercase tracking-[0.1em] text-slate-400">
                      Garantia até
                    </span>
                    <strong className="mt-1 block text-sm text-slate-900">
                      {os.dataVencimentoGarantia ? formatDate(os.dataVencimentoGarantia) : "—"}
                    </strong>
                  </div>
                </div>
              </article>
            ))
          )}
        </section>
      </div>
    </div>
  );
}
