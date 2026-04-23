import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import {
  GripVertical,
  Pencil,
  Plus,
  SlidersHorizontal,
  Trash2,
  X,
} from "lucide-react";

import { DataTable, FieldRenderer, Notice, PageFrame } from "../components/Ui";
import type { ColumnConfig, FieldConfig, FieldType } from "../components/Ui";
import {
  defaultForm,
  displayValue,
  errorMessage,
  payloadFromForm,
  validateForm,
} from "../components/uiHelpers";
import { useList } from "../hooks/useApi";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { useAuth } from "../auth/AuthContext";

type CampoPersonalizado = {
  id: string;
  nome: string;
  chave: string;
  tipo: FieldType;
  obrigatorio: boolean;
  aba?: string;
  linha: number;
  posicao: number;
  ordem: number;
  placeholder?: string;
  valorPadrao?: string;
  opcoes?: string[];
  exportarExcel?: boolean;
  exportarExcelResumo?: boolean;
  exportarPdf?: boolean;
};

type ModuloPersonalizado = {
  id: string;
  nome: string;
  descricao?: string;
  ordem: number;
  campos?: CampoPersonalizado[];
};

type FieldLayout = {
  campoChave: string;
  aba: string;
  linha: number;
  posicao: number;
  ordem: number;
};

const tipoOptions = [
  { value: "text", label: "Texto curto" },
  { value: "email", label: "E-mail" },
  { value: "number", label: "Número" },
  { value: "currency", label: "Valor" },
  { value: "percentage", label: "Porcentagem" },
  { value: "date", label: "Data" },
  { value: "select", label: "Lista" },
  { value: "textarea", label: "Texto longo" },
  { value: "checkbox", label: "Sim/Não" },
];

const moduloFields: FieldConfig[] = [
  { name: "nome", label: "Nome do módulo", required: true, maxLength: 100 },
  { name: "descricao", label: "Descrição", maxLength: 300 },
  { name: "ordem", label: "Ordem no menu", type: "number", min: 0, defaultValue: 0 },
];

const campoBaseFields: FieldConfig[] = [
  { name: "nome", label: "Nome do campo", required: true, maxLength: 100 },
  { name: "aba", label: "Aba", type: "select", required: true, defaultValue: "Principal" },
  { name: "tipo", label: "Tipo", type: "select", required: true, options: tipoOptions },
  { name: "obrigatorio", label: "Obrigatório", type: "checkbox" },
  { name: "exportarExcel", label: "Aparecer no Excel", type: "checkbox", defaultValue: true },
  { name: "exportarExcelResumo", label: "Aparecer no Excel resumido", type: "checkbox" },
  { name: "exportarPdf", label: "Aparecer no PDF", type: "checkbox", defaultValue: true },
  {
    name: "opcoesText",
    label: "Opções",
    type: "textarea",
    span: "full",
    helper: "Uma opção por linha.",
  },
];

const protectedModules = [
  "Painel",
  "Usuarios",
  "Fiscal",
  "Ordem de servico",
  "Nota fiscal",
];

function buttonClass(variant: "primary" | "secondary" | "danger" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800";
  }

  if (variant === "danger") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50";
}

function normalizeTabName(value: unknown) {
  const text = String(value ?? "").trim();
  return text || "Principal";
}

function normalizeTabId(value: unknown) {
  const normalized = normalizeTabName(value)
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");

  return normalized || "principal";
}

function orderForTab(tabIndex: number, fieldIndex: number) {
  return tabIndex * 1000 + fieldIndex + 1;
}

function tabMarkerKey(tab: string) {
  return `__tab__${normalizeTabId(tab)}`;
}

function isTabMarker(value: unknown) {
  return String(value ?? "").startsWith("__tab__");
}

function tabMarkerLayout(tab: string, index: number): FieldLayout {
  return {
    campoChave: tabMarkerKey(tab),
    aba: tab,
    linha: 10000 + index,
    posicao: 1,
    ordem: orderForTab(index, 0),
  };
}

function defaultLayout(fieldName: string, index: number, aba = "Principal"): FieldLayout {
  const zeroBased = Math.max(0, index);
  return {
    campoChave: fieldName,
    aba: normalizeTabName(aba),
    linha: Math.floor(zeroBased / 3) + 1,
    posicao: (zeroBased % 3) + 1,
    ordem: zeroBased + 1,
  };
}

export function ModulosPage() {
  const { session } = useAuth();
  const userRole = String(session?.perfil ?? "").toLowerCase();
  const canManageCustomFields = Boolean(
    session?.isSuperAdmin ||
      ["owner", "admin", "administrador", "super-admin", "superadmin"].includes(userRole),
  );

  const [reloadKey, setReloadKey] = useState(0);
  const modulos = useList("/modulos-personalizados", reloadKey);

  const [selectedId, setSelectedId] = useState("");
  const [moduloForm, setModuloForm] = useState<ApiRecord>(() => defaultForm(moduloFields));
  const [campoForm, setCampoForm] = useState<ApiRecord>(() => defaultForm(campoBaseFields));
  const [registroForm, setRegistroForm] = useState<ApiRecord>({});
  const [editingCampoId, setEditingCampoId] = useState("");
  const [showCampoBuilder, setShowCampoBuilder] = useState(false);
  const [layoutMode, setLayoutMode] = useState(false);
  const [draggingFieldName, setDraggingFieldName] = useState("");
  const [activeTab, setActiveTab] = useState("Principal");
  const [newTabName, setNewTabName] = useState("");
  const [editingTabName, setEditingTabName] = useState("");
  const [draggingTabName, setDraggingTabName] = useState("");
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  const selectedModulo = useMemo(
    () =>
      modulos.data.find(
        (item) => String(item.id ?? "") === selectedId,
      ) as ModuloPersonalizado | undefined,
    [modulos.data, selectedId],
  );

  const campos = useMemo(
    () =>
      [...(selectedModulo?.campos ?? [])].sort(
        (a, b) => a.linha - b.linha || a.posicao - b.posicao || a.ordem - b.ordem,
      ),
    [selectedModulo],
  );

  const registroFields = useMemo<FieldConfig[]>(
    () =>
      campos.map((campo) => ({
        name: campo.chave,
        label: campo.nome,
        type: campo.tipo,
        required: campo.obrigatorio,
        placeholder: campo.placeholder,
        defaultValue:
          campo.tipo === "checkbox" ? campo.valorPadrao === "true" : campo.valorPadrao ?? "",
        options: campo.opcoes?.map((opcao) => ({ value: opcao, label: opcao })),
        line: campo.linha,
        position: campo.posicao,
      })),
    [campos],
  );

  const layoutList = useList(
    selectedId ? `/modulos-personalizados/${selectedId}/layout` : "",
    reloadKey,
  );

  const layoutByField = useMemo(() => {
    const map = new Map<string, FieldLayout>();

    layoutList.data.forEach((item) => {
      const key = String(item.campoChave ?? "");
      if (!key || isTabMarker(key)) return;

      map.set(key, {
        campoChave: key,
        aba: normalizeTabName(item.aba),
        linha: Number(item.linha ?? 1),
        posicao: Number(item.posicao ?? 1),
        ordem: Number(item.ordem ?? 1),
      });
    });

    return map;
  }, [layoutList.data]);

  const orderedRegistroFields = useMemo(
    () =>
      registroFields
        .map((field, index) => ({
          field,
          layout:
            layoutByField.get(field.name) ??
            defaultLayout(field.name, index, normalizeTabName(campos.find((campo) => campo.chave === field.name)?.aba)),
        }))
        .sort(
          (a, b) =>
            a.layout.ordem - b.layout.ordem ||
            a.layout.aba.localeCompare(b.layout.aba) ||
            a.layout.linha - b.layout.linha ||
            a.layout.posicao - b.layout.posicao,
        ),
    [campos, layoutByField, registroFields],
  );

  const tabs = useMemo(() => {
    const order = new Map<string, number>();
    const explicitTabs = new Set<string>();

    layoutList.data.forEach((item) => {
      if (!isTabMarker(item.campoChave)) return;
      const tab = normalizeTabName(item.aba);
      explicitTabs.add(tab);
      order.set(tab, Number(item.ordem ?? Number.MAX_SAFE_INTEGER));
    });

    if (!order.has("Principal")) order.set("Principal", Number.MAX_SAFE_INTEGER - 2);
    if (!order.has(activeTab)) order.set(activeTab, Number.MAX_SAFE_INTEGER - 1);

    orderedRegistroFields.forEach((item) => {
      const tab = normalizeTabName(item.layout.aba);
      if (explicitTabs.has(tab)) return;
      order.set(tab, Math.min(order.get(tab) ?? Number.MAX_SAFE_INTEGER, item.layout.ordem));
    });

    campos.forEach((campo) => {
      const tab = normalizeTabName(campo.aba);
      if (explicitTabs.has(tab)) return;
      order.set(tab, Math.min(order.get(tab) ?? Number.MAX_SAFE_INTEGER, campo.ordem));
    });

    return [...order.entries()].sort((a, b) => a[1] - b[1] || a[0].localeCompare(b[0])).map(([tab]) => tab);
  }, [activeTab, campos, layoutList.data, orderedRegistroFields]);

  const tabOptions = useMemo(
    () => tabs.map((tab) => ({ value: tab, label: tab })),
    [tabs],
  );

  const visibleOrderedRegistroFields = useMemo(
    () => orderedRegistroFields.filter((item) => normalizeTabName(item.layout.aba) === activeTab),
    [activeTab, orderedRegistroFields],
  );

  const registros = useList(
    selectedId ? `/modulos-personalizados/${selectedId}/registros` : "",
    reloadKey,
  );

  const registroRows = useMemo(
    () =>
      registros.data.map((row) => ({
        ...row,
        ...((row.valores as ApiRecord | undefined) ?? {}),
      })),
    [registros.data],
  );

  const registroColumns = useMemo<ColumnConfig[]>(
    () =>
      campos.slice(0, 6).map((campo) => ({
        key: campo.chave,
        label: campo.nome,
        render: (row) => displayValue(row[campo.chave]),
      })),
    [campos],
  );

  function refresh() {
    setReloadKey((key) => key + 1);
  }

  function resetCampoBuilder() {
    setEditingCampoId("");
    setCampoForm(defaultForm(campoBaseFields));
    setErrors({});
    setShowCampoBuilder(false);
  }

  function updateModuloForm(name: string, value: unknown) {
    setModuloForm((current) => ({ ...current, [name]: value }));
  }

  function updateCampoForm(name: string, value: unknown) {
    setCampoForm((current) => ({ ...current, [name]: value }));
    setErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
  }

  function updateRegistroForm(name: string, value: unknown) {
    setRegistroForm((current) => ({ ...current, [name]: value }));
  }

  async function salvarModulo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setNotice("");
    setFailure("");

    const validation = validateForm(moduloFields, moduloForm);
    if (Object.keys(validation).length > 0) {
      setFailure("Corrija os dados do módulo.");
      return;
    }

    try {
      const created = await apiRequest<ModuloPersonalizado>("/modulos-personalizados", {
        method: "POST",
        body: payloadFromForm(moduloFields, moduloForm),
      });

      setSelectedId(created.id);
      setModuloForm(defaultForm(moduloFields));
      setRegistroForm({});
      setNotice("Módulo criado.");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function salvarCampo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedModulo || !canManageCustomFields) return;

    setNotice("");
    setFailure("");

    const validation = validateForm(campoBaseFields, campoForm);
    if (Object.keys(validation).length > 0) {
      setErrors(validation);
      setFailure("Corrija os dados do campo.");
      return;
    }

    const basePayload = payloadFromForm(campoBaseFields, campoForm);
    const opcoesText = String(campoForm.opcoesText ?? "");
    const fieldTab = normalizeTabName(basePayload.aba);
    const index = orderedRegistroFields.filter(
      (item) => normalizeTabName(item.layout.aba) === fieldTab,
    ).length;
    const layout = defaultLayout(
      String(basePayload.nome ?? `campo_${campos.length + 1}`),
      index,
      fieldTab,
    );

    const body = {
      nome: basePayload.nome,
      tipo: basePayload.tipo,
      obrigatorio: basePayload.obrigatorio,
      aba: fieldTab,
      linha: layout.linha,
      posicao: layout.posicao,
      ordem: orderForTab(Math.max(0, tabs.indexOf(fieldTab)), index),
      placeholder: null,
      valorPadrao: null,
      opcoes: opcoesText
        .split(/\r?\n/)
        .map((item) => item.trim())
        .filter(Boolean),
      exportarExcel: basePayload.exportarExcel !== false,
      exportarExcelResumo: basePayload.exportarExcelResumo === true,
      exportarPdf: basePayload.exportarPdf !== false,
      ativo: true,
    };

    try {
      if (editingCampoId) {
        const { tipo: _tipo, ...updateBody } = body;
        await apiRequest(
          `/modulos-personalizados/${selectedModulo.id}/campos/${editingCampoId}`,
          {
            method: "PUT",
            body: updateBody,
          },
        );
        setNotice("Campo atualizado. O tipo foi preservado.");
      } else {
        await apiRequest(`/modulos-personalizados/${selectedModulo.id}/campos`, {
          method: "POST",
          body,
        });
        setNotice("Campo criado.");
      }

      setEditingCampoId("");
      setCampoForm(defaultForm(campoBaseFields));
      setShowCampoBuilder(false);
      setActiveTab(fieldTab);
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function excluirCampo() {
    if (!selectedModulo || !editingCampoId || !canManageCustomFields) return;
    if (!window.confirm("Excluir este campo?")) return;

    try {
      await apiRequest(
        `/modulos-personalizados/${selectedModulo.id}/campos/${editingCampoId}`,
        {
          method: "DELETE",
        },
      );
      setNotice("Campo excluído.");
      resetCampoBuilder();
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function salvarRegistro(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedModulo) return;

    setNotice("");
    setFailure("");

    const validation = validateForm(registroFields, registroForm);
    if (Object.keys(validation).length > 0) {
      setFailure("Corrija os dados do registro.");
      return;
    }

    try {
      await apiRequest(`/modulos-personalizados/${selectedModulo.id}/registros`, {
        method: "POST",
        body: { valores: payloadFromForm(registroFields, registroForm) },
      });
      setRegistroForm(defaultForm(registroFields));
      setNotice("Registro salvo.");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function editarCampo(campo: CampoPersonalizado) {
    setEditingCampoId(campo.id);
    setShowCampoBuilder(true);
    setCampoForm({
      nome: campo.nome,
      aba: normalizeTabName(campo.aba),
      tipo: campo.tipo,
      obrigatorio: campo.obrigatorio,
      exportarExcel: campo.exportarExcel !== false,
      exportarExcelResumo: campo.exportarExcelResumo === true,
      exportarPdf: campo.exportarPdf !== false,
      opcoesText: (campo.opcoes ?? []).join("\n"),
    });
  }

  async function moveField(targetName: string) {
    if (!selectedModulo || !draggingFieldName || draggingFieldName === targetName) return;

    const names = visibleOrderedRegistroFields.map((item) => item.field.name);
    const from = names.indexOf(draggingFieldName);
    const to = names.indexOf(targetName);
    if (from < 0 || to < 0) return;

    const next = [...names];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);

    const overrides = new Map<string, string[]>();
    overrides.set(activeTab, next);
    const body = buildLayoutPayload(tabs, overrides);

    try {
      layoutList.setData(body as ApiRecord[]);
      await apiRequest(`/modulos-personalizados/${selectedModulo.id}/layout`, {
        method: "PATCH",
        body,
      });
      setDraggingFieldName("");
      setNotice("Ordem dos campos atualizada.");
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function selectModulo(id: string) {
    setSelectedId(id);
    setRegistroForm({});
    setActiveTab("Principal");
    setNewTabName("");
    setEditingTabName("");
    setNotice("");
    setFailure("");
    setLayoutMode(false);
    resetCampoBuilder();
  }

  function buildLayoutPayload(nextTabs: string[], overrides?: Map<string, string[]>) {
    const payload: FieldLayout[] = [];
    const usedFields = new Set<string>();

    nextTabs.forEach((tab, index) => {
      payload.push(tabMarkerLayout(tab, index));
    });

    nextTabs.forEach((tab, tabIndex) => {
      const names =
        overrides?.get(tab) ??
        orderedRegistroFields
          .filter((item) => normalizeTabName(item.layout.aba) === tab)
          .map((item) => item.field.name);

      names.forEach((name, fieldIndex) => {
        payload.push({
          campoChave: name,
          aba: tab,
          linha: Math.floor(fieldIndex / 3) + 1,
          posicao: (fieldIndex % 3) + 1,
          ordem: orderForTab(tabIndex, fieldIndex),
        });
        usedFields.add(name);
      });
    });

    orderedRegistroFields
      .filter((item) => !usedFields.has(item.field.name))
      .forEach((item) => {
        const tab = nextTabs.includes(normalizeTabName(item.layout.aba))
          ? normalizeTabName(item.layout.aba)
          : "Principal";
        const tabIndex = Math.max(0, nextTabs.indexOf(tab));
        const fieldIndex = payload.filter(
          (entry) => !isTabMarker(entry.campoChave) && normalizeTabName(entry.aba) === tab,
        ).length;

        payload.push({
          campoChave: item.field.name,
          aba: tab,
          linha: Math.floor(fieldIndex / 3) + 1,
          posicao: (fieldIndex % 3) + 1,
          ordem: orderForTab(tabIndex, fieldIndex),
        });
      });

    return payload;
  }

  async function saveTabOrder(nextTabs: string[], successMessage: string, overrides?: Map<string, string[]>) {
    if (!selectedModulo) return;

    const body = buildLayoutPayload(nextTabs, overrides);
    layoutList.setData(body as ApiRecord[]);

    try {
      await apiRequest(`/modulos-personalizados/${selectedModulo.id}/layout`, {
        method: "PATCH",
        body,
      });
      setNotice(successMessage);
      refresh();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function createTab() {
    const rawLabel = String(newTabName ?? "").trim();
    if (!rawLabel) {
      setFailure("Informe um nome para a nova aba.");
      return;
    }

    const name = normalizeTabName(rawLabel);
    const nextTabs = tabs.includes(name) ? tabs : [...tabs, name];

    setActiveTab(name);
    setCampoForm((current) => ({ ...current, aba: name }));
    setNewTabName("");
    await saveTabOrder(nextTabs, "Aba criada.");
  }

  async function saveTabName() {
    if (!editingTabName) {
      await createTab();
      return;
    }

    const rawLabel = String(newTabName ?? "").trim();
    if (!rawLabel) {
      setFailure("Informe um nome para a aba.");
      return;
    }

    const name = normalizeTabName(rawLabel);
    if (name !== editingTabName && tabs.includes(name)) {
      setFailure("Ja existe uma aba com esse nome.");
      return;
    }

    const nextTabs = tabs.map((tab) => (tab === editingTabName ? name : tab));
    const overrides = new Map<string, string[]>();
    nextTabs.forEach((tab) => {
      if (tab === name) {
        overrides.set(name, orderedRegistroFields
          .filter((item) => normalizeTabName(item.layout.aba) === editingTabName)
          .map((item) => item.field.name));
        return;
      }

      overrides.set(tab, orderedRegistroFields
        .filter((item) => normalizeTabName(item.layout.aba) === tab)
        .map((item) => item.field.name));
    });

    await saveTabOrder(nextTabs, "Aba atualizada.", overrides);
    setActiveTab(name);
    setCampoForm((current) => ({ ...current, aba: name }));
    setEditingTabName("");
    setNewTabName("");
  }

  function openEditTab(tab: string) {
    if (tab === "Principal") return;
    setEditingTabName(tab);
    setNewTabName(tab);
  }

  async function moveTab(targetTab: string) {
    if (!draggingTabName || draggingTabName === targetTab) return;

    const from = tabs.indexOf(draggingTabName);
    const to = tabs.indexOf(targetTab);
    if (from < 0 || to < 0) return;

    const nextTabs = [...tabs];
    const [moved] = nextTabs.splice(from, 1);
    nextTabs.splice(to, 0, moved);

    await saveTabOrder(nextTabs, "Ordem das abas atualizada.");
    setDraggingTabName("");
  }

  async function deleteTab(tab: string) {
    if (tab === "Principal") return;

    const fieldsToMove = orderedRegistroFields
      .filter((item) => normalizeTabName(item.layout.aba) === tab)
      .map((item) => item.field.name);
    const message =
      fieldsToMove.length > 0
        ? `Excluir a aba "${tab}"? Os campos dela serao movidos para a aba Principal.`
        : `Excluir a aba "${tab}"?`;

    if (!window.confirm(message)) return;

    const nextTabs = tabs.filter((item) => item !== tab);
    const overrides = new Map<string, string[]>();
    const principalFields = orderedRegistroFields
      .filter((item) => normalizeTabName(item.layout.aba) === "Principal")
      .map((item) => item.field.name);
    overrides.set("Principal", [...principalFields, ...fieldsToMove]);

    await saveTabOrder(nextTabs, "Aba excluida.", overrides);
    if (activeTab === tab) {
      setActiveTab("Principal");
    }
  }

  return (
    <PageFrame
      eyebrow="Personalização"
      title="Módulos da empresa"
      description="Monte cadastros livres sem mexer nos fluxos fixos de painel, usuários, fiscal, ordem de serviço e nota fiscal."
    >
      <div className="space-y-6">
        <Notice type="info">
          Permanecem fixos: {protectedModules.join(", ")}. Nos módulos criados aqui, o nome e a posição dos campos podem mudar; o tipo fica travado depois da criação.
        </Notice>

        {notice ? <Notice type="success">{notice}</Notice> : null}
        {failure || modulos.error || registros.error ? (
          <Notice type="error">{failure || modulos.error || registros.error}</Notice>
        ) : null}

        <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
          <form
            className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm"
            onSubmit={salvarModulo}
          >
            <div className="mb-5 border-b border-slate-100 pb-5">
              <h2 className="text-xl font-bold tracking-tight text-slate-900">Novo módulo</h2>
              <p className="mt-1 text-sm text-slate-500">
                Crie cadastros que a empresa pode adaptar.
              </p>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {moduloFields.map((field) => (
                <FieldRenderer
                  key={field.name}
                  field={field}
                  value={moduloForm[field.name]}
                  onChange={updateModuloForm}
                />
              ))}
            </div>

            <div className="mt-6 flex justify-end">
              <button className={buttonClass("primary")} type="submit">
                Criar módulo
              </button>
            </div>
          </form>

          <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-5 border-b border-slate-100 pb-5">
              <h2 className="text-xl font-bold tracking-tight text-slate-900">Módulos criados</h2>
              <p className="mt-1 text-sm text-slate-500">
                Escolha um módulo para organizar campos e registros.
              </p>
            </div>

            <div className="grid gap-3">
              {modulos.data.map((modulo) => {
                const active = selectedId === String(modulo.id ?? "");

                return (
                  <button
                    key={String(modulo.id ?? "")}
                    type="button"
                    onClick={() => selectModulo(String(modulo.id ?? ""))}
                    className={[
                      "rounded-2xl border px-4 py-4 text-left transition",
                      active
                        ? "border-slate-900 bg-slate-900 text-white"
                        : "border-slate-200 bg-slate-50 text-slate-700 hover:bg-slate-100",
                    ].join(" ")}
                  >
                    <strong className="block text-sm">{String(modulo.nome ?? "")}</strong>
                    <span
                      className={[
                        "mt-1 block text-sm",
                        active ? "text-slate-300" : "text-slate-500",
                      ].join(" ")}
                    >
                      {String(modulo.descricao ?? "Sem descrição")}
                    </span>
                  </button>
                );
              })}

              {!modulos.loading && modulos.data.length === 0 ? (
                <p className="text-sm text-slate-500">Nenhum módulo personalizado.</p>
              ) : null}
            </div>
          </section>
        </div>

        {selectedModulo ? (
          <div className="space-y-6">
            <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex flex-col gap-4 border-b border-slate-100 pb-5 xl:flex-row xl:items-start xl:justify-between">
                <div>
                  <h2 className="text-xl font-bold tracking-tight text-slate-900">
                    Campos de {selectedModulo.nome}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Adicione campos e escolha onde eles aparecem no formulário.
                  </p>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-600">
                    <strong className="mr-1 text-slate-900">{campos.length}</strong>
                    {campos.length === 1 ? "campo" : "campos"}
                  </div>

                  {canManageCustomFields ? (
                    <button
                      className={buttonClass()}
                      type="button"
                      onClick={() => {
                        if (showCampoBuilder && !editingCampoId) {
                          resetCampoBuilder();
                          return;
                        }
                        setEditingCampoId("");
                        setCampoForm({
                          ...defaultForm(campoBaseFields),
                          aba: activeTab,
                        });
                        setErrors({});
                        setShowCampoBuilder(true);
                      }}
                    >
                      <Plus size={16} />
                      {showCampoBuilder && !editingCampoId ? "Fechar campo" : "Adicionar campo"}
                    </button>
                  ) : null}

                  <button
                    className={buttonClass()}
                    type="button"
                    onClick={() => setLayoutMode((current) => !current)}
                  >
                    <SlidersHorizontal size={16} />
                    {layoutMode ? "Concluir layout" : "Organizar campos"}
                  </button>
                </div>
              </div>

              {campos.length > 0 ? (
                <div className="mt-4 flex flex-wrap gap-2">
                  {campos.map((campo) => (
                    <button
                      key={campo.id}
                      type="button"
                      className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700 transition hover:bg-slate-100"
                      onClick={() => editarCampo(campo)}
                      disabled={!canManageCustomFields}
                    >
                      <GripVertical size={14} className="text-slate-400" />
                      <strong className="text-slate-900">{campo.nome}</strong>
                      <span className="text-slate-500">
                        {tipoOptions.find((tipo) => tipo.value === campo.tipo)?.label ?? campo.tipo}
                      </span>
                    </button>
                  ))}
                </div>
              ) : (
                <div className="mt-4">
                  <Notice>Crie o primeiro campo para montar o formulário deste módulo.</Notice>
                </div>
              )}

              {selectedModulo ? (
                <div className="mt-4 flex flex-col gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-3">
                  <div className="flex flex-wrap gap-2">
                    {tabs.map((tab) => (
                      <div
                        key={tab}
                        className={`inline-flex items-center overflow-hidden rounded-xl text-sm font-medium transition ${
                          activeTab === tab
                            ? "bg-slate-900 text-white"
                            : "border border-slate-200 bg-white text-slate-700 hover:bg-slate-100"
                        }`}
                      >
                        <button
                          type="button"
                          draggable={canManageCustomFields}
                          onDragStart={() => setDraggingTabName(tab)}
                          onDragOver={(event) => event.preventDefault()}
                          onDrop={() => void moveTab(tab)}
                          className="px-3 py-2"
                          onClick={() => setActiveTab(tab)}
                        >
                          {tab}
                        </button>

                        {canManageCustomFields && tab !== "Principal" ? (
                          <button
                            type="button"
                            className={`border-l px-2 py-2 transition ${
                              activeTab === tab
                                ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                                : "border-slate-200 text-slate-400 hover:bg-slate-50 hover:text-slate-700"
                            }`}
                            title="Editar nome da aba"
                            onClick={() => openEditTab(tab)}
                          >
                            <Pencil size={13} />
                          </button>
                        ) : null}

                        {canManageCustomFields && tab !== "Principal" ? (
                          <button
                            type="button"
                            className={`border-l px-2 py-2 transition ${
                              activeTab === tab
                                ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                                : "border-slate-200 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                            }`}
                            title="Excluir aba"
                            onClick={() => void deleteTab(tab)}
                          >
                            <X size={13} />
                          </button>
                        ) : null}
                      </div>
                    ))}
                  </div>

                  {canManageCustomFields ? (
                    <div className="flex flex-col gap-2 sm:flex-row">
                      <input
                        className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
                        value={newTabName}
                        maxLength={80}
                        placeholder={editingTabName ? "Novo nome da aba" : "Nova aba"}
                        onChange={(event) => setNewTabName(event.target.value)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter") {
                            event.preventDefault();
                            void saveTabName();
                          }
                        }}
                      />
                      <button className={buttonClass()} type="button" onClick={() => void saveTabName()}>
                        <Plus size={15} />
                        {editingTabName ? "Salvar nome" : "Criar aba"}
                      </button>
                    </div>
                  ) : null}
                </div>
              ) : null}

              {showCampoBuilder ? (
                <div className="mt-5 rounded-3xl border border-slate-200 bg-slate-50 p-5">
                  <div className="mb-5 flex flex-col gap-3 border-b border-slate-200 pb-5 xl:flex-row xl:items-start xl:justify-between">
                    <div>
                      <h3 className="text-lg font-bold text-slate-900">
                        {editingCampoId ? "Editar campo" : "Adicionar campo"}
                      </h3>
                      <p className="mt-1 text-sm text-slate-500">
                        {editingCampoId
                          ? "O tipo fica bloqueado para preservar os dados."
                          : "Crie campos extras e depois ajuste a ordem com Organizar campos."}
                      </p>
                    </div>

                    <button
                      type="button"
                      className={buttonClass()}
                      onClick={resetCampoBuilder}
                    >
                      <X size={16} />
                      Fechar
                    </button>
                  </div>

                  <form onSubmit={salvarCampo}>
                    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                      {campoBaseFields
                        .filter(
                          (field) =>
                            field.name !== "opcoesText" || campoForm.tipo === "select",
                        )
                        .map((field) => (
                          <FieldRenderer
                            key={field.name}
                            field={
                              editingCampoId && field.name === "tipo"
                                ? { ...field, disabled: true }
                                : field.name === "aba"
                                  ? { ...field, options: tabOptions }
                                : field
                            }
                            value={campoForm[field.name]}
                            error={errors[field.name]}
                            onChange={updateCampoForm}
                          />
                        ))}
                    </div>

                    <div className="mt-4">
                      <Notice>
                        Depois de criar, arraste o campo no formulário para ajustar a posição.
                      </Notice>
                    </div>

                    <div className="mt-6 flex flex-wrap justify-end gap-3">
                      {editingCampoId ? (
                        <button
                          className={buttonClass("danger")}
                          type="button"
                          onClick={() => void excluirCampo()}
                        >
                          <Trash2 size={16} />
                          Excluir campo
                        </button>
                      ) : null}

                      <button className={buttonClass()} type="button" onClick={resetCampoBuilder}>
                        Cancelar
                      </button>

                      <button className={buttonClass("primary")} type="submit">
                        {editingCampoId ? "Salvar campo" : "Criar campo"}
                      </button>
                    </div>
                  </form>
                </div>
              ) : null}
            </section>

            <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="mb-5 border-b border-slate-100 pb-5">
                <h2 className="text-xl font-bold tracking-tight text-slate-900">Registros</h2>
                <p className="mt-1 text-sm text-slate-500">
                  Use o formulário gerado pelos campos do módulo.
                </p>
              </div>

              {layoutMode ? (
                <div className="mb-4">
                  <Notice>
                    Modo de organização ativo. Arraste os campos para mudar a posição.
                  </Notice>
                </div>
              ) : null}

              {registroFields.length > 0 ? (
                <form
                  className="rounded-3xl border border-slate-200 bg-slate-50 p-5"
                  onSubmit={salvarRegistro}
                >
                  <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    {visibleOrderedRegistroFields.map(({ field }) => (
                      <div
                        key={field.name}
                        className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}
                        draggable={layoutMode}
                        onDragStart={() => setDraggingFieldName(field.name)}
                        onDragOver={(event) => event.preventDefault()}
                        onDrop={() => void moveField(field.name)}
                      >
                        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
                          <div className="mb-3 flex items-center justify-between gap-3">
                            {layoutMode ? (
                              <span className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-medium text-slate-500">
                                <GripVertical size={14} />
                                Arrastar
                              </span>
                            ) : (
                              <span />
                            )}

                            {canManageCustomFields ? (
                              <button
                                type="button"
                                className="inline-flex items-center justify-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-600 transition hover:border-slate-300 hover:bg-slate-50 hover:text-slate-900"
                                onClick={() => {
                                  const campo = campos.find((item) => item.chave === field.name);
                                  if (campo) editarCampo(campo);
                                }}
                              >
                                <Pencil size={12} />
                                Editar
                              </button>
                            ) : null}
                          </div>

                          <FieldRenderer
                            field={field}
                            value={registroForm[field.name]}
                            onChange={updateRegistroForm}
                          />
                        </div>
                      </div>
                    ))}
                    {visibleOrderedRegistroFields.length === 0 ? (
                      <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-600 md:col-span-2 xl:col-span-3">
                        Esta aba ainda nao possui campos.
                      </div>
                    ) : null}
                  </div>

                  <div className="mt-6 flex justify-end">
                    <button className={buttonClass("primary")} type="submit">
                      Salvar registro
                    </button>
                  </div>
                </form>
              ) : (
                <Notice>Crie campos antes de cadastrar registros.</Notice>
              )}

              <div className="mt-6">
                <DataTable
                  columns={
                    registroColumns.length > 0
                      ? registroColumns
                      : [{ key: "id", label: "Registro" }]
                  }
                  rows={registroRows}
                  loading={registros.loading}
                  emptyText="Nenhum registro neste módulo."
                />
              </div>
            </section>
          </div>
        ) : null}
      </div>
    </PageFrame>
  );
}
