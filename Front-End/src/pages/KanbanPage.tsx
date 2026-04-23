import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import {
  Columns3,
  Eye,
  EyeOff,
  GripVertical,
  Pencil,
  Plus,
  RefreshCw,
  RotateCcw,
  Search,
  Settings2,
  Shield,
  Trash2,
  UserSquare2,
  X,
  Copy,
  Check,
  Package,
  Wallet,
  Clock3,
  MessageCircle,
  Filter,
  Download,
} from "lucide-react";


import { Notice, PageFrame } from "../components/Ui";
import { useList } from "../hooks/useApi";
import { apiRequest } from "../lib/api";

type TabKey = "publico" | "privado" | "configuracao";

type DragState = {
  cardId: string;
  fromColumnId: string;
};

type DragPrivateState = {
  cardId: string;
  fromColumnId: string;
};

type KanbanPublicoCardDto = {
  id: string;
  ordemServicoId: string;
  kanbanColunaId: string;
  publicTrackingToken: string;
  numeroOs: string;
  cliente: string;
  telefoneCliente?: string | null;
  aparelho: string;
  defeito?: string | null;
  tecnico?: string | null;
  valorTotal?: number | null;
  statusFinanceiro?: string | null;
  statusPeca?: string | null;
  atrasada: boolean;
  ordem: number;
};

type KanbanPublicoColunaDto = {
  id: string;
  nomeInterno: string;
  nomePublico?: string | null;
  cor: string;
  ordem: number;
  sistema: boolean;
  ativa: boolean;
  visivelCliente: boolean;
  geraEventoCliente: boolean;
  etapaFinal: boolean;
  permiteEnvioWhatsApp: boolean;
  descricaoPublica?: string | null;
  cards: KanbanPublicoCardDto[];
};

type KanbanPrivadoCardDto = {
  id: string;
  kanbanColunaId: string;
  ordemServicoId?: string | null;
  titulo: string;
  descricao?: string | null;
  ordem: number;
  createdAt?: string;
  updatedAt?: string;
};

type KanbanPrivadoColunaDto = {
  id: string;
  nome: string;
  ordem: number;
  sistema: boolean;
  ativa: boolean;
  cards: KanbanPrivadoCardDto[];
};

type KanbanConfiguracaoColunaDto = {
  id: string;
  nomeInterno: string;
  nomePublico?: string | null;
  cor: string;
  ordem: number;
  sistema: boolean;
  ativa: boolean;
  visivelCliente: boolean;
  geraEventoCliente: boolean;
  etapaFinal: boolean;
  permiteEnvioWhatsApp: boolean;
  descricaoPublica?: string | null;
};

type CreateKanbanPublicoColunaDto = {
  nomeInterno: string;
  nomePublico?: string | null;
  cor?: string | null;
  visivelCliente: boolean;
  geraEventoCliente: boolean;
  etapaFinal: boolean;
  permiteEnvioWhatsApp: boolean;
  descricaoPublica?: string | null;
};

type UpdateKanbanPublicoColunaDto = {
  nomeInterno: string;
  nomePublico?: string | null;
  cor?: string | null;
  ativa: boolean;
  visivelCliente: boolean;
  geraEventoCliente: boolean;
  etapaFinal: boolean;
  permiteEnvioWhatsApp: boolean;
  descricaoPublica?: string | null;
};

type MoveKanbanPublicoCardDto = {
  colunaId: string;
  ordem: number;
};

type CreateKanbanPrivadoColunaDto = {
  nome: string;
};

type UpdateKanbanPrivadoColunaDto = {
  nome: string;
  ativa: boolean;
};

type CreateKanbanTarefaPrivadaDto = {
  kanbanColunaId: string;
  titulo: string;
  descricao?: string | null;
  ordemServicoId?: string | null;
};

type UpdateKanbanTarefaPrivadaDto = {
  titulo: string;
  descricao?: string | null;
  ordemServicoId?: string | null;
};

type MoveKanbanTarefaPrivadaDto = {
  colunaId: string;
  ordem: number;
};

type ResumoFiltroState = {
  periodoInicio: string;
  periodoFim: string;
  colunaId: string;
  cliente: string;
  tecnico: string;
  statusFinanceiro: string;
  statusPeca: string;
  valorMinimo: string;
  valorMaximo: string;
  atrasadas: boolean;
  finalizadas: boolean;
};

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60";

const textareaClass =
  "min-h-[110px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 resize-y";

function buttonClass(variant: "primary" | "secondary" | "danger" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800";
  }

  if (variant === "danger") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-semibold text-rose-700 transition hover:bg-rose-100";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50";
}

function tabClass(active: boolean) {
  return [
    "inline-flex items-center gap-2 rounded-2xl px-4 py-2.5 text-sm font-semibold transition",
    active
      ? "bg-slate-900 text-white shadow-sm"
      : "border border-slate-200 bg-white text-slate-700 hover:bg-slate-50",
  ].join(" ");
}

function cardShellClass(highlight = false) {
  return [
    "rounded-[28px] border bg-white shadow-sm transition",
    highlight ? "border-slate-400 ring-4 ring-slate-200/60" : "border-slate-200",
  ].join(" ");
}

function formatMoney(value?: number | null) {
  if (value == null) return "—";
  return value.toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL",
  });
}

function formatDate(value?: string) {
  if (!value) return "—";
  return new Date(value).toLocaleString("pt-BR");
}

function normalizeText(value?: string | null) {
  return value?.trim() || "";
}

function PublicCard({
  card,
  onCopyLink,
  copied,
}: {
  card: KanbanPublicoCardDto;
  onCopyLink: (token: string) => void;
  copied: boolean;
}) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-slate-50 p-4 shadow-sm transition hover:border-slate-300 hover:bg-white">
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <strong className="block text-sm font-semibold text-slate-900">
            OS #{card.numeroOs}
          </strong>
          <span className="mt-1 block text-xs text-slate-400">{card.cliente}</span>
        </div>

        <span className="inline-flex shrink-0 items-center gap-1 rounded-xl border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-500">
          <GripVertical size={12} />
          mover
        </span>
      </div>

      <div className="space-y-2 text-sm">
        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            Aparelho
          </span>
          <span className="mt-1 block text-slate-700">{card.aparelho || "—"}</span>
        </div>

        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            Defeito
          </span>
          <span className="mt-1 block text-slate-700">{card.defeito || "—"}</span>
        </div>
      </div>

      <div className="mt-3 grid gap-2 sm:grid-cols-2">
        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="flex items-center gap-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            <UserSquare2 size={12} />
            Técnico
          </span>
          <span className="mt-1 block text-sm text-slate-700">{card.tecnico || "—"}</span>
        </div>

        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="flex items-center gap-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            <Wallet size={12} />
            Valor
          </span>
          <span className="mt-1 block text-sm text-slate-700">{formatMoney(card.valorTotal)}</span>
        </div>

        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="flex items-center gap-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            <Clock3 size={12} />
            Financeiro
          </span>
          <span className="mt-1 block text-sm text-slate-700">{card.statusFinanceiro || "—"}</span>
        </div>

        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="flex items-center gap-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            <Package size={12} />
            Peça
          </span>
          <span className="mt-1 block text-sm text-slate-700">{card.statusPeca || "—"}</span>
        </div>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        {card.atrasada ? (
          <span className="rounded-full border border-rose-200 bg-rose-50 px-2.5 py-1 text-xs font-semibold text-rose-700">
            Atrasada
          </span>
        ) : (
          <span className="rounded-full border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700">
            No prazo
          </span>
        )}
      </div>

      <div className="mt-4 flex flex-wrap justify-end gap-2 border-t border-slate-200 pt-3">
        <button className={buttonClass()} type="button" onClick={() => onCopyLink(card.publicTrackingToken)}>
          {copied ? <Check size={14} /> : <Copy size={14} />}
          {copied ? "Link copiado" : "Copiar link"}
        </button>
      </div>
    </article>
  );
}

function PrivateCard({
  card,
  onEdit,
}: {
  card: KanbanPrivadoCardDto;
  onEdit: (card: KanbanPrivadoCardDto) => void;
}) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-slate-50 p-4 shadow-sm transition hover:border-slate-300 hover:bg-white">
      <div className="mb-3 flex items-start justify-between gap-3">
        <strong className="line-clamp-2 text-sm font-semibold text-slate-900">{card.titulo}</strong>

        <span className="inline-flex shrink-0 items-center gap-1 rounded-xl border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-500">
          <GripVertical size={12} />
          mover
        </span>
      </div>

      <p className={["text-sm leading-6", card.descricao ? "text-slate-600" : "italic text-slate-400"].join(" ")}>
        {card.descricao || "Sem descrição"}
      </p>

      <div className="mt-4 grid gap-2">
        <div className="rounded-2xl bg-white px-3 py-2">
          <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
            OS vinculada
          </span>
          <span className="mt-1 block text-sm text-slate-700">{card.ordemServicoId || "Não vinculada"}</span>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap items-center justify-between gap-2 border-t border-slate-200 pt-3">
        <span className="text-[11px] text-slate-400">Atualizado em {formatDate(card.updatedAt)}</span>
        <button className={buttonClass()} type="button" onClick={() => onEdit(card)}>
          <Pencil size={14} />
          Editar
        </button>
      </div>
    </article>
  );
}

export function KanbanPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("publico");
  const [reloadKey, setReloadKey] = useState(0);

  const publico = useList("/kanban/publico", reloadKey);
  const encerrados = useList(activeTab === "publico" ? "/kanban/publico/encerrados" : "", reloadKey);
  const privado = useList("/kanban/privado", reloadKey);
  const configuracao = useList("/kanban/publico/configuracao", reloadKey);

  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");

  const [searchPublico, setSearchPublico] = useState("");
  const [searchPrivado, setSearchPrivado] = useState("");

  const [draggingPublico, setDraggingPublico] = useState<DragState | null>(null);
  const [overPublicColumnId, setOverPublicColumnId] = useState("");

  const [draggingPrivado, setDraggingPrivado] = useState<DragPrivateState | null>(null);
  const [overPrivateColumnId, setOverPrivateColumnId] = useState("");

  const [copiedToken, setCopiedToken] = useState("");

  const [publicConfigModalOpen, setPublicConfigModalOpen] = useState(false);
  const [publicConfigEditing, setPublicConfigEditing] = useState<KanbanConfiguracaoColunaDto | null>(null);
  const [publicConfigForm, setPublicConfigForm] = useState<CreateKanbanPublicoColunaDto>({
    nomeInterno: "",
    nomePublico: "",
    cor: "#CBD5E1",
    visivelCliente: true,
    geraEventoCliente: true,
    etapaFinal: false,
    permiteEnvioWhatsApp: false,
    descricaoPublica: "",
  });

  const [privateColumnModalOpen, setPrivateColumnModalOpen] = useState(false);
  const [privateColumnEditing, setPrivateColumnEditing] = useState<KanbanPrivadoColunaDto | null>(null);
  const [privateColumnForm, setPrivateColumnForm] = useState<CreateKanbanPrivadoColunaDto>({ nome: "" });
  const [privateColumnAtiva, setPrivateColumnAtiva] = useState(true);

  const [privateTaskModalOpen, setPrivateTaskModalOpen] = useState(false);
  const [privateTaskEditing, setPrivateTaskEditing] = useState<KanbanPrivadoCardDto | null>(null);
  const [privateTaskForm, setPrivateTaskForm] = useState<CreateKanbanTarefaPrivadaDto>({
    kanbanColunaId: "",
    titulo: "",
    descricao: "",
    ordemServicoId: "",
  });

  const [closedOpen, setClosedOpen] = useState(false);
  const [summaryOpen, setSummaryOpen] = useState(false);
  const [summaryFilters, setSummaryFilters] = useState<ResumoFiltroState>({
    periodoInicio: "",
    periodoFim: "",
    colunaId: "",
    cliente: "",
    tecnico: "",
    statusFinanceiro: "",
    statusPeca: "",
    valorMinimo: "",
    valorMaximo: "",
    atrasadas: false,
    finalizadas: false,
  });

  function refresh() {
    setReloadKey((current) => current + 1);
  }

  function resetMessages() {
    setNotice("");
    setFailure("");
  }

  const publicColumns = useMemo(() => {
    const data = Array.isArray(publico.data) ? (publico.data as KanbanPublicoColunaDto[]) : [];
    const term = searchPublico.trim().toLowerCase();

    const normalized = [...data]
      .sort((a, b) => a.ordem - b.ordem)
      .map((coluna) => ({
        ...coluna,
        cards: [...(coluna.cards ?? [])].sort((a, b) => a.ordem - b.ordem),
      }));

    if (!term) return normalized;

    return normalized.map((coluna) => ({
      ...coluna,
      cards: coluna.cards.filter((card) => {
        const bucket = [
          card.numeroOs,
          card.cliente,
          card.aparelho,
          card.defeito,
          card.tecnico,
          card.statusFinanceiro,
          card.statusPeca,
        ]
          .join(" ")
          .toLowerCase();

        return bucket.includes(term);
      }),
    }));
  }, [publico.data, searchPublico]);

  const privateColumns = useMemo(() => {
    const data = Array.isArray(privado.data) ? (privado.data as KanbanPrivadoColunaDto[]) : [];
    const term = searchPrivado.trim().toLowerCase();

    const normalized = [...data]
      .sort((a, b) => a.ordem - b.ordem)
      .map((coluna) => ({
        ...coluna,
        cards: [...(coluna.cards ?? [])].sort((a, b) => a.ordem - b.ordem),
      }));

    if (!term) return normalized;

    return normalized.map((coluna) => ({
      ...coluna,
      cards: coluna.cards.filter((card) =>
        `${card.titulo} ${card.descricao ?? ""} ${card.ordemServicoId ?? ""}`.toLowerCase().includes(term),
      ),
    }));
  }, [privado.data, searchPrivado]);

  const configColumns = useMemo(() => {
    const data = Array.isArray(configuracao.data) ? (configuracao.data as KanbanConfiguracaoColunaDto[]) : [];
    return [...data].sort((a, b) => a.ordem - b.ordem);
  }, [configuracao.data]);

  useEffect(() => {
    if (!copiedToken) return;
    const timer = setTimeout(() => setCopiedToken(""), 1800);
    return () => clearTimeout(timer);
  }, [copiedToken]);

  function trackingUrl(token: string) {
    return `${window.location.origin}/acompanhar/${token}`;
  }

  

  async function copyTrackingLink(token: string) {
    try {
      await navigator.clipboard.writeText(trackingUrl(token));
      setCopiedToken(token);
      setNotice("Link público copiado.");
      setFailure("");
    } catch {
      setFailure("Não foi possível copiar o link.");
    }
  }

  function onlyDigits(value?: string | null) {
    return String(value ?? "").replace(/\D/g, "");
  }

  function shareTrackingWhatsApp(card: KanbanPublicoCardDto, coluna: KanbanPublicoColunaDto) {
    const telefone = onlyDigits(card.telefoneCliente);
    const etapa = (coluna.nomePublico ?? coluna.nomeInterno ?? "").trim();
    const url = trackingUrl(card.publicTrackingToken);

    const texto = [
      `Olá!`,
      `Sua ordem de serviço ${card.numeroOs} está em: ${etapa}.`,
      `Acompanhe por este link:`,
      url,
    ].join("\n");

    const base = telefone ? `https://wa.me/55${telefone}` : "https://wa.me/";
    window.open(`${base}?text=${encodeURIComponent(texto)}`, "_blank", "noopener,noreferrer");
  }

  function openCreatePublicConfig() {
    setPublicConfigEditing(null);
    setPublicConfigForm({
      nomeInterno: "",
      nomePublico: "",
      cor: "#CBD5E1",
      visivelCliente: true,
      geraEventoCliente: true,
      etapaFinal: false,
      permiteEnvioWhatsApp: false,
      descricaoPublica: "",
    });
    setPublicConfigModalOpen(true);
  }

  function openEditPublicConfig(coluna: KanbanConfiguracaoColunaDto) {
    setPublicConfigEditing(coluna);
    setPublicConfigForm({
      nomeInterno: coluna.nomeInterno,
      nomePublico: coluna.nomePublico ?? "",
      cor: coluna.cor || "#CBD5E1",
      visivelCliente: coluna.visivelCliente,
      geraEventoCliente: coluna.geraEventoCliente,
      etapaFinal: coluna.etapaFinal,
      permiteEnvioWhatsApp: coluna.permiteEnvioWhatsApp,
      descricaoPublica: coluna.descricaoPublica ?? "",
    });
    setPublicConfigModalOpen(true);
  }

  async function submitPublicConfig(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    resetMessages();

    if (!publicConfigForm.nomeInterno.trim()) {
      setFailure("Informe o nome interno da coluna.");
      return;
    }

    try {
      if (publicConfigEditing) {
        const payload: UpdateKanbanPublicoColunaDto = {
          nomeInterno: publicConfigForm.nomeInterno.trim(),
          nomePublico: normalizeText(publicConfigForm.nomePublico) || null,
          cor: normalizeText(publicConfigForm.cor) || null,
          ativa: publicConfigEditing.ativa,
          visivelCliente: publicConfigForm.visivelCliente,
          geraEventoCliente: publicConfigForm.geraEventoCliente,
          etapaFinal: publicConfigForm.etapaFinal,
          permiteEnvioWhatsApp: publicConfigForm.permiteEnvioWhatsApp,
          descricaoPublica: normalizeText(publicConfigForm.descricaoPublica) || null,
        };

        await apiRequest(`/kanban/publico/colunas/${publicConfigEditing.id}`, {
          method: "PUT",
          body: payload,
        });

        setNotice("Coluna pública atualizada com sucesso.");
      } else {
        await apiRequest(`/kanban/publico/colunas`, {
          method: "POST",
          body: {
            ...publicConfigForm,
            nomeInterno: publicConfigForm.nomeInterno.trim(),
            nomePublico: normalizeText(publicConfigForm.nomePublico) || null,
            cor: normalizeText(publicConfigForm.cor) || null,
            descricaoPublica: normalizeText(publicConfigForm.descricaoPublica) || null,
          },
        });

        setNotice("Coluna pública criada com sucesso.");
      }

      setPublicConfigModalOpen(false);
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível salvar a coluna pública.");
    }
  }

  async function togglePublicColumnAtiva(coluna: KanbanConfiguracaoColunaDto) {
    resetMessages();

    try {
      const payload: UpdateKanbanPublicoColunaDto = {
        nomeInterno: coluna.nomeInterno,
        nomePublico: coluna.nomePublico ?? null,
        cor: coluna.cor,
        ativa: !coluna.ativa,
        visivelCliente: coluna.visivelCliente,
        geraEventoCliente: coluna.geraEventoCliente,
        etapaFinal: coluna.etapaFinal,
        permiteEnvioWhatsApp: coluna.permiteEnvioWhatsApp,
        descricaoPublica: coluna.descricaoPublica ?? null,
      };

      await apiRequest(`/kanban/publico/colunas/${coluna.id}`, {
        method: "PUT",
        body: payload,
      });

      setNotice(coluna.ativa ? "Coluna desativada." : "Coluna ativada.");
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível alterar o status da coluna.");
    }
  }

  async function reorderPublicColumn(colunaId: string, ordem: number) {
    resetMessages();

    try {
      await apiRequest(`/kanban/publico/colunas/${colunaId}/reordenar`, {
        method: "PATCH",
        body: { ordem },
      });

      setNotice("Ordem da coluna atualizada.");
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível reordenar a coluna.");
    }
  }

  async function removePublicColumn(coluna: KanbanConfiguracaoColunaDto) {
    resetMessages();

    if (!window.confirm(`Excluir a coluna "${coluna.nomeInterno}"?`)) return;

    try {
      await apiRequest(`/kanban/publico/colunas/${coluna.id}`, { method: "DELETE" });
      setNotice("Coluna removida com sucesso.");
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível excluir a coluna.");
    }
  }

  async function movePublicCard(ordemServicoId: string, dto: MoveKanbanPublicoCardDto) {
    resetMessages();

    try {
      await apiRequest(`/kanban/publico/os/${ordemServicoId}/mover`, {
        method: "PATCH",
        body: dto,
      });

      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível mover a OS.");
    }
  }

  async function reopenPublicCard(ordemServicoId: string) {
    resetMessages();

    try {
      await apiRequest(`/kanban/publico/os/${ordemServicoId}/reabrir`, {
        method: "PATCH",
      });

      setNotice("OS reaberta com sucesso.");
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível reabrir a OS.");
    }
  }

  function openCreatePrivateColumn() {
    setPrivateColumnEditing(null);
    setPrivateColumnForm({ nome: "" });
    setPrivateColumnAtiva(true);
    setPrivateColumnModalOpen(true);
  }

  function openEditPrivateColumn(coluna: KanbanPrivadoColunaDto) {
    setPrivateColumnEditing(coluna);
    setPrivateColumnForm({ nome: coluna.nome });
    setPrivateColumnAtiva(coluna.ativa);
    setPrivateColumnModalOpen(true);
  }

  async function submitPrivateColumn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    resetMessages();

    if (!privateColumnForm.nome.trim()) {
      setFailure("Informe o nome da coluna.");
      return;
    }

    try {
      if (privateColumnEditing) {
        const payload: UpdateKanbanPrivadoColunaDto = {
          nome: privateColumnForm.nome.trim(),
          ativa: privateColumnAtiva,
        };

        await apiRequest(`/kanban/privado/colunas/${privateColumnEditing.id}`, {
          method: "PUT",
          body: payload,
        });

        setNotice("Coluna privada atualizada.");
      } else {
        await apiRequest(`/kanban/privado/colunas`, {
          method: "POST",
          body: { nome: privateColumnForm.nome.trim() },
        });

        setNotice("Coluna privada criada.");
      }

      setPrivateColumnModalOpen(false);
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível salvar a coluna privada.");
    }
  }

  async function deletePrivateColumn(coluna: KanbanPrivadoColunaDto) {
    resetMessages();

    if (!window.confirm(`Excluir a coluna "${coluna.nome}"?`)) return;

    try {
      await apiRequest(`/kanban/privado/colunas/${coluna.id}`, { method: "DELETE" });
      setNotice("Coluna privada removida.");
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível excluir a coluna privada.");
    }
  }

  function openCreatePrivateTask() {
    setPrivateTaskEditing(null);
    setPrivateTaskForm({
      kanbanColunaId: privateColumns[0]?.id ?? "",
      titulo: "",
      descricao: "",
      ordemServicoId: "",
    });
    setPrivateTaskModalOpen(true);
  }

  function openEditPrivateTask(card: KanbanPrivadoCardDto) {
    setPrivateTaskEditing(card);
    setPrivateTaskForm({
      kanbanColunaId: card.kanbanColunaId,
      titulo: card.titulo,
      descricao: card.descricao ?? "",
      ordemServicoId: card.ordemServicoId ?? "",
    });
    setPrivateTaskModalOpen(true);
  }

  async function submitPrivateTask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    resetMessages();

    if (!privateTaskForm.titulo.trim()) {
      setFailure("Informe o título da tarefa.");
      return;
    }

    try {
      if (privateTaskEditing) {
        const payload: UpdateKanbanTarefaPrivadaDto = {
          titulo: privateTaskForm.titulo.trim(),
          descricao: normalizeText(privateTaskForm.descricao) || null,
          ordemServicoId: normalizeText(privateTaskForm.ordemServicoId) || null,
        };

        await apiRequest(`/kanban/privado/tarefas/${privateTaskEditing.id}`, {
          method: "PUT",
          body: payload,
        });

        setNotice("Tarefa privada atualizada.");
      } else {
        const payload: CreateKanbanTarefaPrivadaDto = {
          kanbanColunaId: privateTaskForm.kanbanColunaId,
          titulo: privateTaskForm.titulo.trim(),
          descricao: normalizeText(privateTaskForm.descricao) || null,
          ordemServicoId: normalizeText(privateTaskForm.ordemServicoId) || null,
        };

        await apiRequest(`/kanban/privado/tarefas`, {
          method: "POST",
          body: payload,
        });

        setNotice("Tarefa privada criada.");
      }

      setPrivateTaskModalOpen(false);
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível salvar a tarefa privada.");
    }
  }

  async function deletePrivateTask(cardId: string) {
    resetMessages();

    if (!window.confirm("Excluir esta tarefa privada?")) return;

    try {
      await apiRequest(`/kanban/privado/tarefas/${cardId}`, { method: "DELETE" });
      setNotice("Tarefa removida.");
      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível excluir a tarefa.");
    }
  }

  async function movePrivateTask(cardId: string, dto: MoveKanbanTarefaPrivadaDto) {
    resetMessages();

    try {
      await apiRequest(`/kanban/privado/tarefas/${cardId}/mover`, {
        method: "PATCH",
        body: dto,
      });

      refresh();
    } catch (error) {
      setFailure(error instanceof Error ? error.message : "Não foi possível mover a tarefa privada.");
    }
  }

  const totalPublicCards = useMemo(
    () => publicColumns.reduce((sum, coluna) => sum + coluna.cards.length, 0),
    [publicColumns],
  );

  const totalPrivateCards = useMemo(
    () => privateColumns.reduce((sum, coluna) => sum + coluna.cards.length, 0),
    [privateColumns],
  );

  return (
    <PageFrame
      eyebrow="Gestão"
      title="Kanban"
      description="Fluxo oficial da OS, quadro privado por usuário e configuração completa do fluxo público."
      actions={
        <div className="flex flex-wrap gap-2">
          <button className={buttonClass()} type="button" onClick={refresh}>
            <RefreshCw size={16} />
            Atualizar
          </button>

          {activeTab === "publico" ? (
            <>
              <button className={buttonClass()} type="button" onClick={() => setClosedOpen((current) => !current)}>
                <RotateCcw size={16} />
                Encerrados
              </button>

              <button className={buttonClass()} type="button" onClick={() => setSummaryOpen(true)}>
                <Filter size={16} />
                Resumo
              </button>
            </>
          ) : null}
        </div>
      }
    >
      <div className="space-y-6">
        {notice ? <Notice type="success">{notice}</Notice> : null}
        {failure ? <Notice type="error">{failure}</Notice> : null}
        {publico.error ? <Notice type="error">{publico.error}</Notice> : null}
        {privado.error ? <Notice type="error">{privado.error}</Notice> : null}
        {configuracao.error ? <Notice type="error">{configuracao.error}</Notice> : null}

        <div className="flex flex-wrap gap-2">
          <button className={tabClass(activeTab === "publico")} type="button" onClick={() => setActiveTab("publico")}>
            <Shield size={16} />
            Kanban público
          </button>
          <button className={tabClass(activeTab === "privado")} type="button" onClick={() => setActiveTab("privado")}>
            <UserSquare2 size={16} />
            Meu Kanban privado
          </button>
          <button
            className={tabClass(activeTab === "configuracao")}
            type="button"
            onClick={() => setActiveTab("configuracao")}
          >
            <Settings2 size={16} />
            Configuração do fluxo público
          </button>
        </div>

        {summaryOpen ? (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4">
            <div className="w-full max-w-5xl rounded-3xl bg-white p-6 shadow-2xl">
              <div className="mb-6 flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-bold text-slate-900">Resumo do Kanban público</h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Estrutura pronta para integrar com endpoint de resumo e exportação.
                  </p>
                </div>
                <button className={buttonClass()} type="button" onClick={() => setSummaryOpen(false)}>
                  Fechar
                </button>
              </div>

              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Período inicial</span>
                  <input
                    type="date"
                    className={inputClass}
                    value={summaryFilters.periodoInicio}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, periodoInicio: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Período final</span>
                  <input
                    type="date"
                    className={inputClass}
                    value={summaryFilters.periodoFim}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, periodoFim: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Coluna</span>
                  <select
                    className={inputClass}
                    value={summaryFilters.colunaId}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, colunaId: e.target.value }))}
                  >
                    <option value="">Todas</option>
                    {configColumns.map((coluna) => (
                      <option key={coluna.id} value={coluna.id}>
                        {coluna.nomeInterno}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Cliente</span>
                  <input
                    className={inputClass}
                    value={summaryFilters.cliente}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, cliente: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Técnico</span>
                  <input
                    className={inputClass}
                    value={summaryFilters.tecnico}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, tecnico: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Status financeiro</span>
                  <input
                    className={inputClass}
                    value={summaryFilters.statusFinanceiro}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, statusFinanceiro: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Status da peça</span>
                  <input
                    className={inputClass}
                    value={summaryFilters.statusPeca}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, statusPeca: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Valor mínimo</span>
                  <input
                    className={inputClass}
                    value={summaryFilters.valorMinimo}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, valorMinimo: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Valor máximo</span>
                  <input
                    className={inputClass}
                    value={summaryFilters.valorMaximo}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, valorMaximo: e.target.value }))}
                  />
                </label>

                <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={summaryFilters.atrasadas}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, atrasadas: e.target.checked }))}
                  />
                  <span className="text-sm font-medium text-slate-700">Somente atrasadas</span>
                </label>

                <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={summaryFilters.finalizadas}
                    onChange={(e) => setSummaryFilters((s) => ({ ...s, finalizadas: e.target.checked }))}
                  />
                  <span className="text-sm font-medium text-slate-700">Somente finalizadas</span>
                </label>
              </div>

              <div className="mt-6 rounded-3xl border border-dashed border-slate-300 bg-slate-50 p-6">
                <strong className="block text-base text-slate-900">Próximo passo no backend</strong>
                <p className="mt-2 text-sm leading-6 text-slate-600">
                  Criar endpoints como:
                  <br />
                  <code>POST /api/kanban/publico/resumo</code>
                  <br />
                  <code>POST /api/kanban/publico/exportar/csv</code>
                  <br />
                  <code>POST /api/kanban/publico/exportar/excel</code>
                  <br />
                  <code>POST /api/kanban/publico/exportar/pdf</code>
                </p>
              </div>

              <div className="mt-6 flex flex-wrap justify-end gap-3">
                <button className={buttonClass()} type="button">
                  <Download size={16} />
                  Exportar CSV
                </button>
                <button className={buttonClass()} type="button">
                  <Download size={16} />
                  Exportar Excel
                </button>
                <button className={buttonClass()} type="button">
                  <Download size={16} />
                  Exportar PDF
                </button>
                <button className={buttonClass("primary")} type="button">
                  Aplicar filtros
                </button>
              </div>
            </div>
          </div>
        ) : null}

        {publicConfigModalOpen ? (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4">
            <form className="w-full max-w-3xl rounded-3xl bg-white p-6 shadow-2xl" onSubmit={submitPublicConfig}>
              <div className="mb-6 flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-bold text-slate-900">
                    {publicConfigEditing ? "Editar coluna pública" : "Nova coluna pública"}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Configure visibilidade do cliente, trilha pública e comportamento final.
                  </p>
                </div>
                <button className={buttonClass()} type="button" onClick={() => setPublicConfigModalOpen(false)}>
                  Fechar
                </button>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Nome interno</span>
                  <input
                    className={inputClass}
                    value={publicConfigForm.nomeInterno}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, nomeInterno: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Nome público</span>
                  <input
                    className={inputClass}
                    value={publicConfigForm.nomePublico ?? ""}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, nomePublico: e.target.value }))}
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Cor</span>
                  <input
                    className={inputClass}
                    value={publicConfigForm.cor ?? "#CBD5E1"}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, cor: e.target.value }))}
                  />
                </label>

                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Prévia</span>
                  <div
                    className="h-11 rounded-2xl border border-slate-200"
                    style={{ backgroundColor: publicConfigForm.cor || "#CBD5E1" }}
                  />
                </div>

                <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={publicConfigForm.visivelCliente}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, visivelCliente: e.target.checked }))}
                  />
                  <span className="text-sm font-medium text-slate-700">Visível para o cliente</span>
                </label>

                <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={publicConfigForm.geraEventoCliente}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, geraEventoCliente: e.target.checked }))}
                  />
                  <span className="text-sm font-medium text-slate-700">Gera evento na trilha do cliente</span>
                </label>

                <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={publicConfigForm.etapaFinal}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, etapaFinal: e.target.checked }))}
                  />
                  <span className="text-sm font-medium text-slate-700">É etapa final</span>
                </label>

                <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={publicConfigForm.permiteEnvioWhatsApp}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, permiteEnvioWhatsApp: e.target.checked }))}
                  />
                  <span className="text-sm font-medium text-slate-700">Permite envio de WhatsApp</span>
                </label>

                <label className="block md:col-span-2">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Descrição pública</span>
                  <textarea
                    className={textareaClass}
                    value={publicConfigForm.descricaoPublica ?? ""}
                    onChange={(e) => setPublicConfigForm((s) => ({ ...s, descricaoPublica: e.target.value }))}
                  />
                </label>
              </div>

              <div className="mt-6 flex flex-wrap justify-end gap-3">
                <button className={buttonClass()} type="button" onClick={() => setPublicConfigModalOpen(false)}>
                  Cancelar
                </button>
                <button className={buttonClass("primary")} type="submit">
                  Salvar coluna
                </button>
              </div>
            </form>
          </div>
        ) : null}

        {privateColumnModalOpen ? (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4">
            <form className="w-full max-w-xl rounded-3xl bg-white p-6 shadow-2xl" onSubmit={submitPrivateColumn}>
              <div className="mb-5 flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-bold text-slate-900">
                    {privateColumnEditing ? "Editar coluna privada" : "Nova coluna privada"}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">Cada usuário organiza seu próprio fluxo.</p>
                </div>
                <button className={buttonClass()} type="button" onClick={() => setPrivateColumnModalOpen(false)}>
                  Fechar
                </button>
              </div>

              <div className="space-y-4">
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Nome</span>
                  <input
                    className={inputClass}
                    value={privateColumnForm.nome}
                    onChange={(e) => setPrivateColumnForm({ nome: e.target.value })}
                  />
                </label>

                {privateColumnEditing ? (
                  <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <input
                      type="checkbox"
                      checked={privateColumnAtiva}
                      onChange={(e) => setPrivateColumnAtiva(e.target.checked)}
                    />
                    <span className="text-sm font-medium text-slate-700">Coluna ativa</span>
                  </label>
                ) : null}
              </div>

              <div className="mt-5 flex justify-end gap-3">
                <button className={buttonClass()} type="button" onClick={() => setPrivateColumnModalOpen(false)}>
                  Cancelar
                </button>
                <button className={buttonClass("primary")} type="submit">
                  Salvar coluna
                </button>
              </div>
            </form>
          </div>
        ) : null}

        {privateTaskModalOpen ? (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4">
            <form className="w-full max-w-2xl rounded-3xl bg-white p-6 shadow-2xl" onSubmit={submitPrivateTask}>
              <div className="mb-5 flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-bold text-slate-900">
                    {privateTaskEditing ? "Editar tarefa privada" : "Nova tarefa privada"}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Pode vincular a uma OS sem alterar o fluxo oficial.
                  </p>
                </div>
                <button className={buttonClass()} type="button" onClick={() => setPrivateTaskModalOpen(false)}>
                  Fechar
                </button>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                {!privateTaskEditing ? (
                  <label className="block">
                    <span className="mb-2 block text-sm font-medium text-slate-700">Coluna</span>
                    <select
                      className={inputClass}
                      value={privateTaskForm.kanbanColunaId}
                      onChange={(e) => setPrivateTaskForm((s) => ({ ...s, kanbanColunaId: e.target.value }))}
                    >
                      <option value="">Selecione</option>
                      {privateColumns.map((coluna) => (
                        <option key={coluna.id} value={coluna.id}>
                          {coluna.nome}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <span className="block text-sm font-medium text-slate-700">Coluna atual</span>
                    <span className="mt-1 block text-sm text-slate-500">
                      {privateColumns.find((x) => x.id === privateTaskForm.kanbanColunaId)?.nome ?? "—"}
                    </span>
                  </div>
                )}

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">OS vinculada</span>
                  <input
                    className={inputClass}
                    value={privateTaskForm.ordemServicoId ?? ""}
                    placeholder="GUID da OS (opcional)"
                    onChange={(e) => setPrivateTaskForm((s) => ({ ...s, ordemServicoId: e.target.value }))}
                  />
                </label>

                <label className="block md:col-span-2">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Título</span>
                  <input
                    className={inputClass}
                    value={privateTaskForm.titulo}
                    onChange={(e) => setPrivateTaskForm((s) => ({ ...s, titulo: e.target.value }))}
                  />
                </label>

                <label className="block md:col-span-2">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Descrição</span>
                  <textarea
                    className={textareaClass}
                    value={privateTaskForm.descricao ?? ""}
                    onChange={(e) => setPrivateTaskForm((s) => ({ ...s, descricao: e.target.value }))}
                  />
                </label>
              </div>

              <div className="mt-5 flex justify-end gap-3">
                <button className={buttonClass()} type="button" onClick={() => setPrivateTaskModalOpen(false)}>
                  Cancelar
                </button>
                <button className={buttonClass("primary")} type="submit">
                  Salvar tarefa
                </button>
              </div>
            </form>
          </div>
        ) : null}

        {activeTab === "publico" ? (
          <section className="space-y-6">
            <div className="grid gap-4 lg:grid-cols-[auto_auto_1fr_auto] lg:items-center">
              <div className="rounded-3xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
                    <Columns3 size={18} />
                  </div>
                  <div>
                    <strong className="block text-lg font-bold text-slate-900">{publicColumns.length}</strong>
                    <span className="block text-xs uppercase tracking-[0.12em] text-slate-400">Colunas</span>
                  </div>
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
                    <Shield size={18} />
                  </div>
                  <div>
                    <strong className="block text-lg font-bold text-slate-900">{totalPublicCards}</strong>
                    <span className="block text-xs uppercase tracking-[0.12em] text-slate-400">OS no fluxo</span>
                  </div>
                </div>
              </div>

              <label className="relative block">
                <Search
                  size={16}
                  className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400"
                />
                <input
                  className={`${inputClass} pl-11 pr-11`}
                  value={searchPublico}
                  placeholder="Buscar por OS, cliente, aparelho, defeito ou técnico"
                  onChange={(e) => setSearchPublico(e.target.value)}
                />
                {searchPublico ? (
                  <button
                    type="button"
                    className="absolute right-3 top-1/2 inline-flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-lg text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                    onClick={() => setSearchPublico("")}
                  >
                    <X size={14} />
                  </button>
                ) : null}
              </label>

              <button className={buttonClass()} type="button" onClick={() => setActiveTab("configuracao")}>
                <Settings2 size={16} />
                Configurar fluxo
              </button>
            </div>

            {closedOpen ? (
              <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-5">
                  <div>
                    <h2 className="text-xl font-bold tracking-tight text-slate-900">OS encerradas</h2>
                    <p className="mt-1 text-sm text-slate-500">Cards que estão em etapa final.</p>
                  </div>
                  <button className={buttonClass()} type="button" onClick={() => setClosedOpen(false)}>
                    Fechar
                  </button>
                </div>

                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                  {((encerrados.data as KanbanPublicoCardDto[]) ?? []).map((card) => (
                    <div key={card.id} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                      <strong className="block text-sm font-semibold text-slate-900">
                        OS #{card.numeroOs} • {card.cliente}
                      </strong>
                      <p className="mt-2 text-sm leading-6 text-slate-600">{card.aparelho || "Sem aparelho"}</p>

                      <div className="mt-4 flex flex-wrap justify-between gap-2 border-t border-slate-200 pt-3">
                        <span className="text-xs text-slate-400">{formatMoney(card.valorTotal)}</span>
                        <button className={buttonClass()} type="button" onClick={() => reopenPublicCard(card.ordemServicoId)}>
                          <RotateCcw size={14} />
                          Reabrir
                        </button>
                      </div>
                    </div>
                  ))}

                  {encerrados.loading ? (
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-500">
                      Carregando encerradas...
                    </div>
                  ) : null}
                </div>
              </section>
            ) : null}

            <section className="overflow-x-auto rounded-3xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
              <div className="flex min-w-max items-start gap-4">
                {publicColumns.map((coluna) => (
                  <div
                    key={coluna.id}
                    className={cardShellClass(overPublicColumnId === coluna.id)}
                    style={{ width: 360 }}
                    onDragOver={(event) => {
                      event.preventDefault();
                      setOverPublicColumnId(coluna.id);
                    }}
                    onDragLeave={() => setOverPublicColumnId("")}
                    onDrop={(event) => {
                      event.preventDefault();
                      if (!draggingPublico) return;

                      void movePublicCard(draggingPublico.cardId, {
                        colunaId: coluna.id,
                        ordem: coluna.cards.length + 1,
                      });

                      setDraggingPublico(null);
                      setOverPublicColumnId("");
                    }}
                  >
                    <header className="border-b border-slate-100 px-4 py-4">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <strong className="block truncate text-sm font-semibold text-slate-900">
                            {coluna.nomeInterno}
                          </strong>
                          <span className="mt-1 block text-xs text-slate-400">
                            {coluna.cards.length} {coluna.cards.length === 1 ? "OS" : "OSs"}
                          </span>
                        </div>

                        <div className="flex flex-wrap items-center justify-end gap-2">
                          {coluna.visivelCliente ? (
                            <span className="inline-flex items-center gap-1 rounded-full border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-[11px] font-semibold text-emerald-700">
                              <Eye size={12} />
                              Cliente vê
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600">
                              <EyeOff size={12} />
                              Interna
                            </span>
                          )}

                          {coluna.etapaFinal ? (
                            <span className="rounded-full border border-violet-200 bg-violet-50 px-2.5 py-1 text-[11px] font-semibold text-violet-700">
                              Final
                            </span>
                          ) : null}
                        </div>
                      </div>
                    </header>

                    <div className="flex min-h-[320px] flex-1 flex-col gap-3 p-4">
                      {coluna.cards.map((card, index) => (
                        <div
                          key={card.id}
                          draggable
                          onDragStart={(event) => {
                            event.dataTransfer.effectAllowed = "move";
                            event.dataTransfer.setData("text/plain", card.ordemServicoId);
                            setDraggingPublico({
                              cardId: card.ordemServicoId,
                              fromColumnId: coluna.id,
                            });
                          }}
                          onDragEnd={() => {
                            setDraggingPublico(null);
                            setOverPublicColumnId("");
                          }}
                          onDragOver={(event) => event.preventDefault()}
                          onDrop={(event) => {
                            event.preventDefault();
                            event.stopPropagation();
                            if (!draggingPublico) return;

                            void movePublicCard(draggingPublico.cardId, {
                              colunaId: coluna.id,
                              ordem: index + 1,
                            });

                            setDraggingPublico(null);
                            setOverPublicColumnId("");
                          }}
                        >
                          <PublicCard
                            card={card}
                            onCopyLink={copyTrackingLink}
                            copied={copiedToken === card.publicTrackingToken}
                          />

                          {coluna.permiteEnvioWhatsApp ? (
                            <div className="mt-2 flex justify-end">
                              <button
                                className={buttonClass()}
                                type="button"
                                onClick={() => shareTrackingWhatsApp(card, coluna)}
                              >
                                <MessageCircle size={14} />
                                WhatsApp
                              </button>
                            </div>
                          ) : null}
                        </div>
                      ))}

                      {coluna.cards.length === 0 ? (
                        <div className="flex min-h-[180px] flex-1 flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-6 text-center">
                          <span className="text-sm font-medium text-slate-600">Nenhuma OS nesta etapa</span>
                          <small className="mt-1 text-xs text-slate-400">Arraste uma OS para cá</small>
                        </div>
                      ) : null}
                    </div>
                  </div>
                ))}

                {publico.loading ? (
                  <div className="flex h-[320px] w-[360px] items-center justify-center rounded-[28px] border border-slate-200 bg-white text-sm text-slate-500 shadow-sm">
                    Carregando quadro público...
                  </div>
                ) : null}
              </div>
            </section>
          </section>
        ) : null}

        {activeTab === "privado" ? (
          <section className="space-y-6">
            <div className="grid gap-4 lg:grid-cols-[auto_auto_1fr_auto_auto] lg:items-center">
              <div className="rounded-3xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
                    <Columns3 size={18} />
                  </div>
                  <div>
                    <strong className="block text-lg font-bold text-slate-900">{privateColumns.length}</strong>
                    <span className="block text-xs uppercase tracking-[0.12em] text-slate-400">Colunas</span>
                  </div>
                </div>
              </div>

              <div className="rounded-3xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
                    <UserSquare2 size={18} />
                  </div>
                  <div>
                    <strong className="block text-lg font-bold text-slate-900">{totalPrivateCards}</strong>
                    <span className="block text-xs uppercase tracking-[0.12em] text-slate-400">Tarefas</span>
                  </div>
                </div>
              </div>

              <label className="relative block">
                <Search
                  size={16}
                  className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400"
                />
                <input
                  className={`${inputClass} pl-11 pr-11`}
                  value={searchPrivado}
                  placeholder="Buscar tarefa por título, descrição ou OS"
                  onChange={(e) => setSearchPrivado(e.target.value)}
                />
                {searchPrivado ? (
                  <button
                    type="button"
                    className="absolute right-3 top-1/2 inline-flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-lg text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                    onClick={() => setSearchPrivado("")}
                  >
                    <X size={14} />
                  </button>
                ) : null}
              </label>

              <button className={buttonClass("primary")} type="button" onClick={openCreatePrivateTask}>
                <Plus size={16} />
                Nova tarefa
              </button>

              <button className={buttonClass()} type="button" onClick={openCreatePrivateColumn}>
                <Plus size={16} />
                Nova coluna
              </button>
            </div>

            <section className="overflow-x-auto rounded-3xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
              <div className="flex min-w-max items-start gap-4">
                {privateColumns.map((coluna) => (
                  <div
                    key={coluna.id}
                    className={cardShellClass(overPrivateColumnId === coluna.id)}
                    style={{ width: 340 }}
                    onDragOver={(event) => {
                      event.preventDefault();
                      setOverPrivateColumnId(coluna.id);
                    }}
                    onDragLeave={() => setOverPrivateColumnId("")}
                    onDrop={(event) => {
                      event.preventDefault();
                      if (!draggingPrivado) return;

                      void movePrivateTask(draggingPrivado.cardId, {
                        colunaId: coluna.id,
                        ordem: coluna.cards.length + 1,
                      });

                      setDraggingPrivado(null);
                      setOverPrivateColumnId("");
                    }}
                  >
                    <header className="border-b border-slate-100 px-4 py-4">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <strong className="block truncate text-sm font-semibold text-slate-900">{coluna.nome}</strong>
                          <span className="mt-1 block text-xs text-slate-400">
                            {coluna.cards.length} {coluna.cards.length === 1 ? "tarefa" : "tarefas"}
                          </span>
                        </div>

                        <div className="flex gap-2">
                          <button
                            type="button"
                            className="inline-flex h-8 w-8 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-50 hover:text-slate-900"
                            onClick={() => openEditPrivateColumn(coluna)}
                          >
                            <Pencil size={14} />
                          </button>

                          {!coluna.sistema ? (
                            <button
                              type="button"
                              className="inline-flex h-8 w-8 items-center justify-center rounded-xl border border-rose-200 bg-rose-50 text-rose-600 transition hover:bg-rose-100"
                              onClick={() => deletePrivateColumn(coluna)}
                            >
                              <Trash2 size={14} />
                            </button>
                          ) : null}
                        </div>
                      </div>
                    </header>

                    <div className="flex min-h-[320px] flex-1 flex-col gap-3 p-4">
                      {coluna.cards.map((card, index) => (
                        <div
                          key={card.id}
                          draggable
                          onDragStart={(event) => {
                            event.dataTransfer.effectAllowed = "move";
                            event.dataTransfer.setData("text/plain", card.id);
                            setDraggingPrivado({
                              cardId: card.id,
                              fromColumnId: coluna.id,
                            });
                          }}
                          onDragEnd={() => {
                            setDraggingPrivado(null);
                            setOverPrivateColumnId("");
                          }}
                          onDragOver={(event) => event.preventDefault()}
                          onDrop={(event) => {
                            event.preventDefault();
                            event.stopPropagation();
                            if (!draggingPrivado) return;

                            void movePrivateTask(draggingPrivado.cardId, {
                              colunaId: coluna.id,
                              ordem: index + 1,
                            });

                            setDraggingPrivado(null);
                            setOverPrivateColumnId("");
                          }}
                        >
                          <PrivateCard card={card} onEdit={openEditPrivateTask} />

                          <div className="mt-2 flex justify-end gap-2">
                            <button className={buttonClass("danger")} type="button" onClick={() => deletePrivateTask(card.id)}>
                              <Trash2 size={14} />
                              Excluir
                            </button>
                          </div>
                        </div>
                      ))}

                      {coluna.cards.length === 0 ? (
                        <div className="flex min-h-[180px] flex-1 flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-6 text-center">
                          <span className="text-sm font-medium text-slate-600">Nenhuma tarefa nesta coluna</span>
                          <small className="mt-1 text-xs text-slate-400">Arraste uma tarefa para cá</small>
                        </div>
                      ) : null}
                    </div>
                  </div>
                ))}

                {privado.loading ? (
                  <div className="flex h-[320px] w-[340px] items-center justify-center rounded-[28px] border border-slate-200 bg-white text-sm text-slate-500 shadow-sm">
                    Carregando Kanban privado...
                  </div>
                ) : null}
              </div>
            </section>
          </section>
        ) : null}

        {activeTab === "configuracao" ? (
          <section className="space-y-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-xl font-bold tracking-tight text-slate-900">Configuração do fluxo público</h2>
                <p className="mt-1 text-sm text-slate-500">
                  Controle visibilidade do cliente, trilha pública, ordem, etapa final e WhatsApp.
                </p>
              </div>

              <button className={buttonClass("primary")} type="button" onClick={openCreatePublicConfig}>
                <Plus size={16} />
                Nova coluna pública
              </button>
            </div>

            <div className="grid gap-4">
              {configColumns.map((coluna, index) => (
                <section key={coluna.id} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                  <div className="grid gap-4 xl:grid-cols-[1.4fr_auto] xl:items-start">
                    <div className="space-y-4">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <div className="flex flex-wrap items-center gap-2">
                            <strong className="text-lg font-bold text-slate-900">{coluna.nomeInterno}</strong>
                            {coluna.nomePublico ? (
                              <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-semibold text-slate-600">
                                Público: {coluna.nomePublico}
                              </span>
                            ) : null}
                            {coluna.sistema ? (
                              <span className="rounded-full border border-violet-200 bg-violet-50 px-2.5 py-1 text-xs font-semibold text-violet-700">
                                Sistema
                              </span>
                            ) : null}
                            {coluna.etapaFinal ? (
                              <span className="rounded-full border border-emerald-200 bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700">
                                Etapa final
                              </span>
                            ) : null}
                          </div>

                          <div className="mt-2 flex flex-wrap gap-2 text-xs">
                            <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 font-semibold text-slate-600">
                              Ordem {coluna.ordem}
                            </span>
                            <span className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 font-semibold text-slate-600">
                              Cor {coluna.cor}
                            </span>
                            <span
                              className={[
                                "rounded-full px-2.5 py-1 font-semibold border",
                                coluna.ativa
                                  ? "border-emerald-200 bg-emerald-50 text-emerald-700"
                                  : "border-rose-200 bg-rose-50 text-rose-700",
                              ].join(" ")}
                            >
                              {coluna.ativa ? "Ativa" : "Inativa"}
                            </span>
                          </div>
                        </div>

                        <div
                          className="h-12 w-20 rounded-2xl border border-slate-200"
                          style={{ backgroundColor: coluna.cor || "#CBD5E1" }}
                        />
                      </div>

                      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                        <div className="rounded-2xl bg-slate-50 px-4 py-3">
                          <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Visível para cliente
                          </span>
                          <span className="mt-1 block text-sm font-medium text-slate-700">
                            {coluna.visivelCliente ? "Sim" : "Não"}
                          </span>
                        </div>

                        <div className="rounded-2xl bg-slate-50 px-4 py-3">
                          <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Gera evento na trilha
                          </span>
                          <span className="mt-1 block text-sm font-medium text-slate-700">
                            {coluna.geraEventoCliente ? "Sim" : "Não"}
                          </span>
                        </div>

                        <div className="rounded-2xl bg-slate-50 px-4 py-3">
                          <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Permite WhatsApp
                          </span>
                          <span className="mt-1 block text-sm font-medium text-slate-700">
                            {coluna.permiteEnvioWhatsApp ? "Sim" : "Não"}
                          </span>
                        </div>
                      </div>

                      <div className="rounded-2xl bg-slate-50 px-4 py-3">
                        <span className="block text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">
                          Descrição pública
                        </span>
                        <span className="mt-1 block text-sm text-slate-700">{coluna.descricaoPublica || "—"}</span>
                      </div>
                    </div>

                    <div className="flex flex-col gap-2">
                      <button className={buttonClass()} type="button" onClick={() => openEditPublicConfig(coluna)}>
                        <Pencil size={14} />
                        Editar
                      </button>

                      <button className={buttonClass()} type="button" onClick={() => togglePublicColumnAtiva(coluna)}>
                        {coluna.ativa ? <EyeOff size={14} /> : <Eye size={14} />}
                        {coluna.ativa ? "Desativar" : "Ativar"}
                      </button>

                      {index > 0 ? (
                        <button
                          className={buttonClass()}
                          type="button"
                          onClick={() => reorderPublicColumn(coluna.id, coluna.ordem - 1)}
                        >
                          <GripVertical size={14} />
                          Subir
                        </button>
                      ) : null}

                      {index < configColumns.length - 1 ? (
                        <button
                          className={buttonClass()}
                          type="button"
                          onClick={() => reorderPublicColumn(coluna.id, coluna.ordem + 1)}
                        >
                          <GripVertical size={14} />
                          Descer
                        </button>
                      ) : null}

                      {!coluna.sistema && !coluna.etapaFinal ? (
                        <button className={buttonClass("danger")} type="button" onClick={() => removePublicColumn(coluna)}>
                          <Trash2 size={14} />
                          Excluir
                        </button>
                      ) : null}
                    </div>
                  </div>
                </section>
              ))}

              {configuracao.loading ? (
                <div className="rounded-3xl border border-slate-200 bg-white p-6 text-sm text-slate-500 shadow-sm">
                  Carregando configurações...
                </div>
              ) : null}
            </div>
          </section>
        ) : null}
      </div>
    </PageFrame>
  );
}