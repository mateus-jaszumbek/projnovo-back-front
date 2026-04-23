import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import {
  Check,
  Clock3,
  Copy,
  MessageCircle,
  Package,
  ShieldCheck,
  Sparkles,
  Smartphone,
  UserRound,
  Wrench,
} from "lucide-react";

import { Notice } from "../components/Ui";
import { apiRequest, apiResourceUrl } from "../lib/api";

type KanbanTrackingEtapaDto = {
  colunaId: string;
  nome: string;
  cor: string;
  ordem: number;
  atual: boolean;
  concluida: boolean;
};

type KanbanTrackingEventoDto = {
  titulo: string;
  descricao?: string | null;
  data: string;
};

type KanbanTrackingItemDto = {
  tipoItem: string;
  descricao: string;
  quantidade: number;
  valorTotal: number;
};

type OrdemServicoFotoDto = {
  id: string;
  nomeArquivo: string;
  descricao?: string | null;
  dataUrl: string;
};

type KanbanTrackingPublicoDto = {
  publicTrackingToken: string;
  numeroOs: string;
  cliente: string;
  aparelho: string;
  defeito?: string | null;
  empresaLogoUrl?: string | null;
  statusAtual: string;
  colunaAtualId?: string | null;
  valorTotal?: number | null;
  etapas: KanbanTrackingEtapaDto[];
  historico: KanbanTrackingEventoDto[];
  itens: KanbanTrackingItemDto[];
  fotos: OrdemServicoFotoDto[];
};

function formatMoney(value?: number | null) {
  if (value == null) return "—";
  return value.toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL",
  });
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

function statCardClass() {
  return "rounded-[26px] border border-white/70 bg-white/90 p-5 shadow-[0_8px_30px_rgba(15,23,42,0.06)] backdrop-blur";
}

function getStepState(etapa: KanbanTrackingEtapaDto) {
  if (etapa.atual) return "atual";
  if (etapa.concluida) return "concluida";
  return "pendente";
}

export function AcompanhamentoPublicoPage() {
  const { token = "" } = useParams<{ token: string }>();

  const [data, setData] = useState<KanbanTrackingPublicoDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [failure, setFailure] = useState("");
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let active = true;

    async function load() {
      if (!token.trim()) {
        setFailure("Link de acompanhamento inválido.");
        setLoading(false);
        return;
      }

      setLoading(true);
      setFailure("");

      try {
        const result = await apiRequest<KanbanTrackingPublicoDto>(
          `/public/ordens-servico/${token}/acompanhamento`,
          { method: "GET" },
        );

        if (!active) return;
        setData(result);
      } catch (error) {
        if (!active) return;
        setFailure(error instanceof Error ? error.message : "Não foi possível carregar o acompanhamento.");
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
    return `${window.location.origin}/acompanhar/${token}`;
  }, [token]);

  const progresso = useMemo(() => {
    if (!data?.etapas?.length) return 0;
    const concluidas = data.etapas.filter((x) => x.concluida).length;
    const atual = data.etapas.some((x) => x.atual) ? 1 : 0;
    return Math.round(((concluidas + atual) / data.etapas.length) * 100);
  }, [data]);

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
      `Você pode acompanhar sua ordem de serviço ${data.numeroOs} por este link:`,
      publicUrl,
      ``,
      `Status atual: ${data.statusAtual}`,
    ].join("\n");

    window.open(`https://wa.me/?text=${encodeURIComponent(texto)}`, "_blank", "noopener,noreferrer");
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(148,163,184,0.18),_transparent_35%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-10">
        <div className="mx-auto max-w-6xl">
          <div className={`${sectionCardClass()} p-12 text-center`}>
            <div className="mx-auto mb-4 h-12 w-12 animate-pulse rounded-2xl bg-slate-200" />
            <p className="text-sm font-medium text-slate-500">Carregando acompanhamento...</p>
          </div>
        </div>
      </div>
    );
  }

  if (failure || !data) {
    return (
      <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(148,163,184,0.18),_transparent_35%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-10">
        <div className="mx-auto max-w-6xl space-y-4">
          <Notice type="error">{failure || "Acompanhamento não encontrado."}</Notice>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(148,163,184,0.18),_transparent_35%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-8">
      <div className="mx-auto max-w-6xl space-y-6">
        <header className="relative overflow-hidden rounded-[32px] border border-white/70 bg-[linear-gradient(135deg,rgba(15,23,42,1)_0%,rgba(30,41,59,0.96)_40%,rgba(51,65,85,0.92)_100%)] p-6 text-white shadow-[0_20px_60px_rgba(15,23,42,0.24)]">
          <div className="pointer-events-none absolute -right-12 -top-12 h-44 w-44 rounded-full bg-white/10 blur-2xl" />
          <div className="pointer-events-none absolute -bottom-20 left-10 h-48 w-48 rounded-full bg-sky-400/10 blur-3xl" />

          <div className="relative flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
            <div className="max-w-3xl">
              {data.empresaLogoUrl ? (
                <div className="mb-4 flex h-20 w-20 items-center justify-center rounded-2xl border border-white/15 bg-white/95 p-2 shadow-sm">
                  <img
                    src={apiResourceUrl(data.empresaLogoUrl)}
                    alt="Logo da empresa"
                    className="h-full w-full object-contain"
                  />
                </div>
              ) : null}

              <span className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.14em] text-slate-200">
                <Sparkles size={13} />
                Acompanhamento premium
              </span>

              <h1 className="mt-4 text-3xl font-bold tracking-tight md:text-4xl">
                OS #{data.numeroOs}
              </h1>

              <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-300">
                Acompanhe o andamento do seu reparo em tempo real, com etapas, histórico detalhado e link para compartilhamento.
              </p>

              <div className="mt-5 flex flex-wrap items-center gap-3">
                <span className="inline-flex items-center gap-2 rounded-2xl border border-emerald-400/20 bg-emerald-400/10 px-3 py-2 text-sm font-semibold text-emerald-200">
                  <ShieldCheck size={15} />
                  Status atual: {data.statusAtual}
                </span>

                <span className="inline-flex items-center gap-2 rounded-2xl border border-white/10 bg-white/10 px-3 py-2 text-sm font-medium text-slate-200">
                  Progresso: {progresso}%
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

          <div className="relative mt-6">
            <div className="h-2 overflow-hidden rounded-full bg-white/10">
              <div
                className="h-full rounded-full bg-gradient-to-r from-emerald-400 via-sky-400 to-indigo-400 transition-all duration-500"
                style={{ width: `${progresso}%` }}
              />
            </div>
          </div>
        </header>

        <section className={`${sectionCardClass()} p-6`}>
          <div className="mb-5 flex items-center gap-2">
            <Clock3 size={18} className="text-slate-500" />
            <h2 className="text-xl font-bold text-slate-900">Etapas do reparo</h2>
          </div>

          <div className="overflow-x-auto">
            <div className="flex min-w-max items-start gap-0 pb-2">
              {data.etapas?.map((etapa, index) => {
                const state = getStepState(etapa);

                return (
                  <div key={etapa.colunaId} className="flex items-start">
                    <div className="flex min-w-[150px] flex-col items-center">
                      <div
                        className={[
                          "flex h-12 w-12 items-center justify-center rounded-full border-2 text-xs font-bold shadow-sm transition-all",
                          state === "atual"
                            ? "scale-105 border-slate-900 bg-slate-900 text-white shadow-[0_8px_24px_rgba(15,23,42,0.25)]"
                            : state === "concluida"
                            ? "border-emerald-500 bg-emerald-500 text-white"
                            : "bg-white text-slate-500",
                        ].join(" ")}
                        style={state === "pendente" ? { borderColor: etapa.cor } : undefined}
                      >
                        {state === "concluida" ? <Check size={16} /> : index + 1}
                      </div>

                      <span
                        className={[
                          "mt-3 max-w-[130px] text-center text-xs font-semibold leading-5",
                          state === "atual"
                            ? "text-slate-900"
                            : state === "concluida"
                            ? "text-emerald-700"
                            : "text-slate-500",
                        ].join(" ")}
                      >
                        {etapa.nome}
                      </span>
                    </div>

                    {index < data.etapas.length - 1 ? (
                      <div className="mt-6 h-[3px] w-16 rounded-full bg-slate-200">
                        <div
                          className={[
                            "h-full rounded-full",
                            state === "concluida" ? "bg-emerald-500" : "bg-slate-200",
                          ].join(" ")}
                          style={{ width: state === "concluida" ? "100%" : "100%" }}
                        />
                      </div>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </div>
        </section>

        <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <article className={statCardClass()}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                  Cliente
                </span>
                <strong className="mt-2 block text-lg text-slate-900">{data.cliente || "—"}</strong>
              </div>
              <div className="rounded-2xl bg-slate-100 p-3 text-slate-600">
                <UserRound size={18} />
              </div>
            </div>
          </article>

          <article className={statCardClass()}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                  Aparelho
                </span>
                <strong className="mt-2 block text-lg text-slate-900">{data.aparelho || "—"}</strong>
              </div>
              <div className="rounded-2xl bg-slate-100 p-3 text-slate-600">
                <Smartphone size={18} />
              </div>
            </div>
          </article>

          <article className={statCardClass()}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                  Status atual
                </span>
                <strong className="mt-2 block text-lg text-slate-900">{data.statusAtual || "—"}</strong>
              </div>
              <div className="rounded-2xl bg-slate-100 p-3 text-slate-600">
                <Clock3 size={18} />
              </div>
            </div>
          </article>

          <article className={statCardClass()}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                  Valor total
                </span>
                <strong className="mt-2 block text-lg text-slate-900">{formatMoney(data.valorTotal)}</strong>
              </div>
              <div className="rounded-2xl bg-slate-100 p-3 text-slate-600">
                <Package size={18} />
              </div>
            </div>
          </article>
        </section>

        <section className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <article className={`${sectionCardClass()} p-6`}>
            <div className="mb-5 flex items-center gap-2">
              <Wrench size={18} className="text-slate-500" />
              <h2 className="text-xl font-bold text-slate-900">Resumo do atendimento</h2>
            </div>

            <div className="space-y-4">
              <div className="rounded-[24px] border border-slate-200 bg-slate-50/80 p-4">
                <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                  Defeito relatado
                </span>
                <p className="mt-2 text-sm leading-6 text-slate-700">{data.defeito || "—"}</p>
              </div>

              <div className="rounded-[24px] border border-slate-200 bg-slate-50/80 p-4">
                <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                  Link de acompanhamento
                </span>
                <p className="mt-2 break-all text-sm text-slate-700">{publicUrl}</p>
              </div>

              
            </div>
          </article>

          <article className={`${sectionCardClass()} p-6`}>
            <div className="mb-5 flex items-center gap-2">
              <Package size={18} className="text-slate-500" />
              <h2 className="text-xl font-bold text-slate-900">Peças e serviços</h2>
            </div>

            <div className="space-y-3">
              {data.itens?.length ? (
                data.itens.map((item, index) => (
                  <div
                    key={`${item.descricao}-${index}`}
                    className="rounded-[24px] border border-slate-200 bg-white p-4 shadow-sm"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <strong className="text-sm font-semibold text-slate-900">
                        {item.descricao || "Item sem descrição"}
                      </strong>

                      <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-[11px] font-semibold text-slate-500">
                        {item.tipoItem || "Item"}
                      </span>
                    </div>

                    <div className="mt-3 grid gap-2 md:grid-cols-2">
                      <div className="rounded-2xl bg-slate-50 px-3 py-2 text-sm text-slate-600">
                        Qtd.: <strong className="text-slate-900">{item.quantidade ?? "—"}</strong>
                      </div>
                      <div className="rounded-2xl bg-slate-50 px-3 py-2 text-sm text-slate-600">
                        Total: <strong className="text-slate-900">{formatMoney(item.valorTotal)}</strong>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="rounded-[24px] border border-dashed border-slate-200 bg-slate-50 p-6 text-center text-sm text-slate-500">
                  Nenhum item disponível nesta OS.
                </div>
              )}
            </div>
          </article>
        </section>

        {data.fotos?.length ? (
          <section className={`${sectionCardClass()} p-6`}>
            <div className="mb-5 flex items-center gap-2">
              <Smartphone size={18} className="text-slate-500" />
              <h2 className="text-xl font-bold text-slate-900">Fotos do aparelho</h2>
            </div>

            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {data.fotos.map((foto) => (
                <figure
                  key={foto.id}
                  className="overflow-hidden rounded-[24px] border border-slate-200 bg-white shadow-sm"
                >
                  <img
                    src={apiResourceUrl(foto.dataUrl)}
                    alt={foto.descricao || foto.nomeArquivo || "Foto da ordem de servico"}
                    className="h-56 w-full object-cover"
                  />
                  <figcaption className="p-4 text-sm font-medium text-slate-700">
                    {foto.descricao || foto.nomeArquivo || "Foto da ordem de servico"}
                  </figcaption>
                </figure>
              ))}
            </div>
          </section>
        ) : null}
      </div>
    </div>
  );
}
