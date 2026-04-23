import { useEffect, useMemo, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import {
  Download,
  Eye,
  FileText,
  Pencil,
  Plus,
  Search,
  SlidersHorizontal,
  Trash2,
  X,
} from "lucide-react";

import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { useList } from "../hooks/useApi";
import { useAuth } from "../auth/AuthContext";
import { DataTable, FieldRenderer, Notice, PageFrame } from "./Ui";
import {
  defaultForm,
  errorMessage,
  formFromRecord,
  onlyDigits,
  payloadFromForm,
  validateForm,
} from "./uiHelpers";
import type { ColumnConfig, FieldConfig } from "./Ui";

type CrudPageProps = {
  title: string;
  description: string;
  endpoint: string;
  fields: FieldConfig[];
  columns: ColumnConfig[];
  eyebrow?: string;
  submitLabel?: string;
  emptyText?: string;
  allowEdit?: boolean;
  allowDelete?: boolean;
  deleteMode?: "delete" | "inativar";
  embedded?: boolean;
  extraContent?: ReactNode;
  transformPayload?: (payload: ApiRecord) => ApiRecord;
  onAfterChange?: () => void | Promise<void>;
  rowActions?: (row: ApiRecord, refresh: () => void) => ReactNode;
  formFieldActions?: (args: {
    field: FieldConfig;
    form: ApiRecord;
    setField: (name: string, value: unknown) => void;
    isEditing: boolean;
  }) => ReactNode;
  customModuleKey?: string;
  customModuleName?: string;
};

type CustomField = {
  id: string;
  nome: string;
  chave: string;
  tipo: FieldConfig["type"];
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

type CustomModule = {
  id: string;
  nome: string;
  campos?: CustomField[];
};

type FieldLayout = {
  campoChave: string;
  aba: string;
  linha: number;
  posicao: number;
  ordem: number;
};

type TabItem = {
  id: string;
  nome: string;
  ordem: number;
};

const customFieldTypes = [
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

const customFieldFormFields: FieldConfig[] = [
  { name: "nome", label: "Nome do campo", required: true, maxLength: 100 },
  { name: "aba", label: "Aba", maxLength: 80, defaultValue: "Principal" },
  { name: "tipo", label: "Tipo", type: "select", required: true, options: customFieldTypes },
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

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60";

function buttonClass(variant: "primary" | "secondary" | "danger" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";
  }

  if (variant === "danger") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100 disabled:cursor-not-allowed disabled:opacity-60";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60";
}

function displayCellValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "boolean") return value ? "Sim" : "Não";
  if (Array.isArray(value)) return value.join(", ");
  return String(value);
}

function escapeHtml(value: unknown) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
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

function resolveFieldTab(value: unknown) {
  const formTab = normalizeTabName(value);
  return formTab;
}

function defaultLayout(fieldName: string, index: number, aba = "Principal"): FieldLayout {
  const zeroBased = Math.max(0, index);
  return {
    campoChave: fieldName,
    aba,
    linha: Math.floor(zeroBased / 3) + 1,
    posicao: (zeroBased % 3) + 1,
    ordem: zeroBased + 1,
  };
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
    aba: normalizeTabName(tab),
    linha: 10000 + index,
    posicao: 1,
    ordem: orderForTab(index, 0),
  };
}

function areTabsEqual(current: TabItem[], next: TabItem[]) {
  if (current.length !== next.length) return false;

  for (let index = 0; index < current.length; index += 1) {
    if (
      current[index].id !== next[index].id ||
      current[index].nome !== next[index].nome ||
      current[index].ordem !== next[index].ordem
    ) {
      return false;
    }
  }

  return true;
}

function reorderList<T>(items: T[], from: number, to: number) {
  const next = [...items];
  const [moved] = next.splice(from, 1);
  next.splice(to, 0, moved);
  return next;
}

export function CrudPage({
  title,
  description,
  endpoint,
  fields,
  columns,
  eyebrow,
  submitLabel = "Salvar",
  emptyText,
  allowEdit = true,
  allowDelete,
  deleteMode = "delete",
  embedded,
  extraContent,
  transformPayload,
  onAfterChange,
  rowActions,
  formFieldActions,
  customModuleKey,
  customModuleName,
}: CrudPageProps) {
  const { session } = useAuth();
  const userRole = String(session?.perfil ?? "").toLowerCase();
  const canManageCustomFields = Boolean(
    session?.isSuperAdmin ||
      ["owner", "admin", "administrador", "super-admin", "superadmin"].includes(userRole),
  );

  const [reloadKey, setReloadKey] = useState(0);
  const [form, setForm] = useState<ApiRecord>(() => defaultForm(fields));
  const [editingId, setEditingId] = useState("");
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [showForm, setShowForm] = useState(false);
  const [viewingRow, setViewingRow] = useState<ApiRecord | null>(null);
  const [search, setSearch] = useState("");
  const [customModule, setCustomModule] = useState<CustomModule | null>(null);
  const [customFieldForm, setCustomFieldForm] = useState<ApiRecord>(() =>
    defaultForm(customFieldFormFields),
  );
  const [customFieldErrors, setCustomFieldErrors] = useState<Record<string, string>>({});
  const [editingCustomFieldId, setEditingCustomFieldId] = useState("");
  const [showCustomBuilder, setShowCustomBuilder] = useState(false);
  const [customReloadKey, setCustomReloadKey] = useState(0);
  const [draggingTabId, setDraggingTabId] = useState("");
  const [layoutMode, setLayoutMode] = useState(false);
  const [newTabName, setNewTabName] = useState("");
  const [showTabBuilder, setShowTabBuilder] = useState(false);
  const [editingTabId, setEditingTabId] = useState("");
  const [tabItems, setTabItems] = useState<TabItem[]>([
    { id: "principal", nome: "Principal", ordem: 0 },
  ]);
  const [activeTabId, setActiveTabId] = useState("principal");

  const { data, loading, error } = useList(endpoint, reloadKey);
  const customRecords = useList(
    customModule ? `/modulos-personalizados/${customModule.id}/registros` : "",
    customReloadKey,
  );
  const layoutList = useList(
    customModule ? `/modulos-personalizados/${customModule.id}/layout` : "",
    customReloadKey,
  );

  const refresh = () => setReloadKey((key) => key + 1);
  const isEditing = Boolean(editingId);

  const customFields = useMemo(
    () =>
      [...(customModule?.campos ?? [])].sort(
        (a, b) =>
          normalizeTabName(a.aba).localeCompare(normalizeTabName(b.aba)) ||
          a.linha - b.linha ||
          a.posicao - b.posicao ||
          a.ordem - b.ordem,
      ),
    [customModule],
  );

  const dynamicFields = useMemo<FieldConfig[]>(
    () =>
      customFields.map((field) => ({
        name: field.chave,
        label: field.nome,
        type: field.tipo,
        required: field.obrigatorio,
        placeholder: field.placeholder,
        defaultValue:
          field.tipo === "checkbox" ? field.valorPadrao === "true" : field.valorPadrao ?? "",
        options: field.opcoes?.map((option) => ({ value: option, label: option })),
        line: field.linha,
        position: field.posicao,
      })),
    [customFields],
  );

  const customFieldByName = useMemo(
    () => new Map(customFields.map((field) => [field.chave, field])),
    [customFields],
  );

  const allFields = useMemo(() => [...fields, ...dynamicFields], [fields, dynamicFields]);

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

  const orderedFields = useMemo(
    () =>
      allFields
        .map((field, index) => ({
          field,
          layout:
            layoutByField.get(field.name) ??
            layoutByField.get(field.name.toLowerCase()) ??
            defaultLayout(
              field.name,
              index,
              normalizeTabName(customFieldByName.get(field.name)?.aba),
            ),
        }))
        .sort(
          (a, b) =>
            a.layout.ordem - b.layout.ordem ||
            a.layout.aba.localeCompare(b.layout.aba) ||
            a.layout.linha - b.layout.linha ||
            a.layout.posicao - b.layout.posicao,
        ),
    [allFields, customFieldByName, layoutByField],
  );

  useEffect(() => {
    const nextMap = new Map<string, TabItem>();

    const registerTab = (label: unknown, orderHint?: unknown) => {
      const nome = normalizeTabName(label);
      const id = normalizeTabId(nome);
      const ordem = Number(orderHint ?? Number.MAX_SAFE_INTEGER);
      const current = nextMap.get(id);

      if (!current) {
        nextMap.set(id, { id, nome, ordem });
        return;
      }

      nextMap.set(id, {
        id,
        nome: current.nome || nome,
        ordem: Math.min(current.ordem, ordem),
      });
    };

    registerTab("Principal", -1);

    layoutList.data.forEach((item) => {
      if (isTabMarker(item.campoChave)) {
        registerTab(item.aba, item.ordem);
      }
    });

    layoutList.data.forEach((item) => {
      if (!isTabMarker(item.campoChave)) {
        registerTab(item.aba, item.ordem);
      }
    });

    customFields.forEach((field) => {
      registerTab(field.aba, field.ordem);
    });

    const nextTabs = [...nextMap.values()].sort(
      (a, b) => a.ordem - b.ordem || a.nome.localeCompare(b.nome),
    );

    setTabItems((current) => (areTabsEqual(current, nextTabs) ? current : nextTabs));
  }, [customFields, layoutList.data]);

  useEffect(() => {
    if (!tabItems.some((tab) => tab.id === activeTabId)) {
      setActiveTabId(tabItems[0]?.id ?? "principal");
    }
  }, [activeTabId, tabItems]);

  const activeTab = useMemo(
    () => tabItems.find((tab) => tab.id === activeTabId)?.nome ?? "Principal",
    [activeTabId, tabItems],
  );

  const tabOptions = useMemo(
    () => tabItems.map((tab) => ({ value: tab.nome, label: tab.nome })),
    [tabItems],
  );

  const visibleOrderedFields = useMemo(
    () => orderedFields.filter((item) => normalizeTabName(item.layout.aba) === activeTab),
    [activeTab, orderedFields],
  );

  const blankForm = useMemo(() => defaultForm(allFields), [allFields]);

  useEffect(() => {
    setForm((current) => ({ ...defaultForm(allFields), ...current }));
  }, [allFields]);

  const addressFieldNames = useMemo(() => new Set(allFields.map((field) => field.name)), [allFields]);

  const customValuesByOrigin = useMemo(() => {
    const map = new Map<string, ApiRecord>();

    for (const record of customRecords.data) {
      const origemId = String(record.origemId ?? "");
      if (!origemId) continue;
      map.set(origemId, (record.valores as ApiRecord | undefined) ?? {});
    }

    return map;
  }, [customRecords.data]);

  useEffect(() => {
    if (!customModuleKey) return;

    let active = true;

    async function ensureModule() {
      try {
        const module = await apiRequest<CustomModule>("/modulos-personalizados/sistema", {
          method: "POST",
          body: {
            chave: customModuleKey,
            nome: customModuleName ?? title,
            descricao: `Campos extras de ${customModuleName ?? title}`,
          },
        });

        if (active) setCustomModule(module);
      } catch (err) {
        if (active) setFailure(errorMessage(err));
      }
    }

    void ensureModule();

    return () => {
      active = false;
    };
  }, [customModuleKey, customModuleName, customReloadKey, title]);

  const filteredData = useMemo(() => {
    const term = search.trim().toLowerCase();

    const mergedData = data.map((row) => ({
      ...row,
      ...(customValuesByOrigin.get(String(row.id ?? "")) ?? {}),
    }));

    if (!term) return mergedData;

    return mergedData.filter((row) =>
      [...columns, ...dynamicFields.map((field) => ({ key: field.name, label: field.label }))].some(
        (column) =>
          String(row[column.key] ?? "")
            .toLowerCase()
            .includes(term),
      ),
    );
  }, [columns, customValuesByOrigin, data, dynamicFields, search]);

  const excelColumns = useMemo(() => {
    const seen = new Set<string>();

    return [
      ...columns,
      ...customFields
        .filter((field) => field.exportarExcel !== false)
        .map((field) => ({ key: field.chave, label: field.nome })),
    ].filter((column) => {
      if (seen.has(column.key)) return false;
      seen.add(column.key);
      return true;
    });
  }, [columns, customFields]);

  const excelResumoColumns = useMemo(() => {
    const seen = new Set<string>();

    return [
      ...columns,
      ...customFields
        .filter((field) => field.exportarExcelResumo === true)
        .map((field) => ({ key: field.chave, label: field.nome })),
    ].filter((column) => {
      if (seen.has(column.key)) return false;
      seen.add(column.key);
      return true;
    });
  }, [columns, customFields]);

  const pdfColumns = useMemo(() => {
    const seen = new Set<string>();

    return [
      ...columns,
      ...customFields
        .filter((field) => field.exportarPdf !== false)
        .map((field) => ({ key: field.chave, label: field.nome })),
    ].filter((column) => {
      if (seen.has(column.key)) return false;
      seen.add(column.key);
      return true;
    });
  }, [columns, customFields]);

  const viewColumns = useMemo(() => {
    const seen = new Set<string>();

    return [
      ...columns,
      ...dynamicFields.map((field) => ({ key: field.name, label: field.label })),
    ].filter((column) => {
      if (seen.has(column.key)) return false;
      seen.add(column.key);
      return true;
    });
  }, [columns, dynamicFields]);

  const orderedFieldNamesByTab = useMemo(() => {
    const map = new Map<string, string[]>();

    tabItems.forEach((tab) => {
      map.set(tab.nome, []);
    });

    orderedFields.forEach((item) => {
      const tab = normalizeTabName(item.layout.aba);
      const current = map.get(tab) ?? [];
      current.push(item.field.name);
      map.set(tab, current);
    });

    return map;
  }, [orderedFields, tabItems]);

  function buildLayoutPayload(nextTabs: TabItem[], overrides?: Map<string, string[]>) {
    const payload: FieldLayout[] = [];
    const usedFields = new Set<string>();
    const nextTabNames = new Set(nextTabs.map((tab) => tab.nome));

    nextTabs.forEach((tab, tabIndex) => {
      payload.push(tabMarkerLayout(tab.nome, tabIndex));
    });

    nextTabs.forEach((tab, tabIndex) => {
      const names = overrides?.get(tab.nome) ?? orderedFieldNamesByTab.get(tab.nome) ?? [];

      names.forEach((name, fieldIndex) => {
        payload.push({
          campoChave: name,
          aba: tab.nome,
          linha: Math.floor(fieldIndex / 3) + 1,
          posicao: (fieldIndex % 3) + 1,
          ordem: orderForTab(tabIndex, fieldIndex),
        });

        usedFields.add(name);
      });
    });

    const fallbackTab = nextTabs[0]?.nome ?? "Principal";
    const fallbackTabIndex = Math.max(
      0,
      nextTabs.findIndex((tab) => tab.nome === fallbackTab),
    );

    const remainingFields = orderedFields
      .filter((item) => !usedFields.has(item.field.name))
      .map((item) => ({
        name: item.field.name,
        tab: nextTabNames.has(normalizeTabName(item.layout.aba))
          ? normalizeTabName(item.layout.aba)
          : fallbackTab,
      }));

    if (remainingFields.length > 0) {
      const grouped = new Map<string, string[]>();

      remainingFields.forEach((item) => {
        const group = grouped.get(item.tab) ?? [];
        group.push(item.name);
        grouped.set(item.tab, group);
      });

      grouped.forEach((names, tabName) => {
        const targetTabIndex = nextTabs.findIndex((tab) => tab.nome === tabName);
        const existingCount = payload.filter(
          (item) => !isTabMarker(item.campoChave) && normalizeTabName(item.aba) === tabName,
        ).length;

        names.forEach((name, offset) => {
          const fieldIndex = existingCount + offset;
          payload.push({
            campoChave: name,
            aba: tabName,
            linha: Math.floor(fieldIndex / 3) + 1,
            posicao: (fieldIndex % 3) + 1,
            ordem: orderForTab(targetTabIndex >= 0 ? targetTabIndex : fallbackTabIndex, fieldIndex),
          });
        });
      });
    }

    return payload;
  }

  async function persistLayout(nextTabs: TabItem[], successMessage: string, overrides?: Map<string, string[]>) {
    if (!customModule) return false;

    try {
      const body = buildLayoutPayload(nextTabs, overrides);
      layoutList.setData(body as ApiRecord[]);
      setTabItems(nextTabs);

      await apiRequest(`/modulos-personalizados/${customModule.id}/layout`, {
        method: "PATCH",
        body,
      });

      setCustomReloadKey((key) => key + 1);
      setNotice(successMessage);
      return true;
    } catch (err) {
      setFailure(errorMessage(err));
      return false;
    }
  }

  async function deleteTab(tab: TabItem) {
    if (!customModule || !canManageCustomFields || tab.id === "principal") return;

    const fieldsToMove = orderedFieldNamesByTab.get(tab.nome) ?? [];
    const message =
      fieldsToMove.length > 0
        ? `Excluir a aba "${tab.nome}"? Os campos dela serao movidos para a aba Principal.`
        : `Excluir a aba "${tab.nome}"?`;

    if (!window.confirm(message)) return;

    const nextTabs = tabItems
      .filter((item) => item.id !== tab.id)
      .map((item, index) => ({ ...item, ordem: index }));
    const overrides = new Map<string, string[]>();
    const principalFields = orderedFieldNamesByTab.get("Principal") ?? [];

    nextTabs.forEach((item) => {
      overrides.set(item.nome, [...(orderedFieldNamesByTab.get(item.nome) ?? [])]);
    });
    overrides.set("Principal", [...principalFields, ...fieldsToMove]);

    const success = await persistLayout(nextTabs, "Aba excluida.", overrides);
    if (success && activeTabId === tab.id) {
      setActiveTabId("principal");
    }
  }

  function setField(name: string, value: unknown) {
    setForm((current) => ({ ...current, [name]: value }));
    setValidationErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });

    const field = allFields.find((item) => item.name === name);
    if (field?.mask === "cep" && onlyDigits(value).length === 8) {
      void preencherEnderecoPorCep(value);
    }
  }

  async function preencherEnderecoPorCep(value: unknown) {
    const cep = onlyDigits(value);
    if (!addressFieldNames.has("logradouro") && !addressFieldNames.has("cidade")) return;

    try {
      const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
      const data = (await response.json()) as {
        erro?: boolean;
        logradouro?: string;
        complemento?: string;
        bairro?: string;
        localidade?: string;
        uf?: string;
      };

      if (!response.ok || data.erro) {
        setValidationErrors((current) => ({
          ...current,
          cep: "CEP não encontrado.",
        }));
        return;
      }

      setForm((current) => ({
        ...current,
        ...(addressFieldNames.has("logradouro")
          ? { logradouro: data.logradouro ?? current.logradouro }
          : {}),
        ...(addressFieldNames.has("complemento")
          ? { complemento: data.complemento ?? current.complemento }
          : {}),
        ...(addressFieldNames.has("bairro") ? { bairro: data.bairro ?? current.bairro } : {}),
        ...(addressFieldNames.has("cidade")
          ? { cidade: data.localidade ?? current.cidade }
          : {}),
        ...(addressFieldNames.has("uf") ? { uf: data.uf ?? current.uf } : {}),
      }));
    } catch {
      setValidationErrors((current) => ({
        ...current,
        cep: "Não foi possível consultar o CEP agora.",
      }));
    }
  }

  function resetCustomFieldState() {
    setEditingCustomFieldId("");
    setCustomFieldForm(defaultForm(customFieldFormFields));
    setCustomFieldErrors({});
    setShowCustomBuilder(false);
  }

  function resetForm() {
    setEditingId("");
    setForm(blankForm);
    setValidationErrors({});
    setShowForm(false);
    setLayoutMode(false);
    resetCustomFieldState();
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setNotice("");
    setFailure("");

    const errors = validateForm(allFields, form);
    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      setFailure("Corrija os campos destacados antes de salvar.");
      return;
    }

    setSaving(true);

    try {
      const basePayload = payloadFromForm(fields, form);
      const customPayload = dynamicFields.length > 0 ? payloadFromForm(dynamicFields, form) : {};
      const payload = transformPayload?.(basePayload) ?? basePayload;
      const path = isEditing ? `${endpoint}/${editingId}` : endpoint;

      const saved = await apiRequest<ApiRecord>(path, {
        method: isEditing ? "PUT" : "POST",
        body: payload,
      });

      const originId = String((isEditing ? editingId : saved.id) ?? "");
      if (customModule && originId && dynamicFields.length > 0) {
        await apiRequest(`/modulos-personalizados/${customModule.id}/registros/origem/${originId}`, {
          method: "PUT",
          body: { valores: customPayload },
        });
        setCustomReloadKey((key) => key + 1);
      }

      setNotice(isEditing ? "Registro atualizado com sucesso." : "Registro criado com sucesso.");
      resetForm();
      refresh();
      await onAfterChange?.();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function saveCustomField(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!customModule) return;

    const validation = validateForm(customFieldFormFields, customFieldForm);
    if (Object.keys(validation).length > 0) {
      setCustomFieldErrors(validation);
      setFailure("Corrija os dados do campo extra.");
      return;
    }

    const payload = payloadFromForm(customFieldFormFields, customFieldForm);
    const fieldTab = resolveFieldTab(payload.aba);
    const currentTabIndex = Math.max(
      0,
      tabItems.findIndex((tab) => tab.nome === fieldTab),
    );
    const nextIndexInTab = orderedFields.filter(
      (item) => normalizeTabName(item.layout.aba) === fieldTab,
    ).length;

    const layout = {
      ...defaultLayout(String(payload.nome ?? "campo_extra"), nextIndexInTab, fieldTab),
      ordem: orderForTab(currentTabIndex, nextIndexInTab),
    };

    const body = {
      nome: payload.nome,
      tipo: payload.tipo,
      obrigatorio: payload.obrigatorio,
      aba: fieldTab,
      linha: layout.linha,
      posicao: layout.posicao,
      ordem: layout.ordem,
      placeholder: null,
      valorPadrao: null,
      opcoes: String(customFieldForm.opcoesText ?? "")
        .split(/\r?\n/)
        .map((option) => option.trim())
        .filter(Boolean),
      exportarExcel: payload.exportarExcel !== false,
      exportarExcelResumo: payload.exportarExcelResumo === true,
      exportarPdf: payload.exportarPdf !== false,
      ativo: true,
    };

    try {
      if (editingCustomFieldId) {
        const { tipo: _tipo, ...updateBody } = body;
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingCustomFieldId}`, {
          method: "PUT",
          body: updateBody,
        });
        setNotice("Campo extra atualizado. O tipo foi preservado.");
      } else {
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos`, {
          method: "POST",
          body,
        });
        setNotice("Campo extra criado.");
      }

      setActiveTabId(normalizeTabId(fieldTab));
      resetCustomFieldState();
      setCustomReloadKey((key) => key + 1);
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function deleteCustomField() {
    if (!customModule || !editingCustomFieldId || !canManageCustomFields) return;
    if (!window.confirm("Excluir este campo extra? Os dados antigos deixam de aparecer nos cadastros.")) {
      return;
    }

    try {
      await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingCustomFieldId}`, {
        method: "DELETE",
      });
      resetCustomFieldState();
      setCustomReloadKey((key) => key + 1);
      setNotice("Campo extra excluído.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }


  async function moveTab(targetTabId: string) {
    if (!customModule || !draggingTabId || draggingTabId === targetTabId) return;

    const from = tabItems.findIndex((tab) => tab.id === draggingTabId);
    const to = tabItems.findIndex((tab) => tab.id === targetTabId);
    if (from < 0 || to < 0) return;

    const nextTabs = reorderList(tabItems, from, to).map((tab, index) => ({
      ...tab,
      ordem: index,
    }));

    const success = await persistLayout(nextTabs, "Ordem das abas atualizada.");
    if (success) {
      setDraggingTabId("");
    }
  }

  function openCreateTab() {
    setEditingTabId("");
    setNewTabName("");
    setShowTabBuilder(true);
  }

  function openEditTab(tab: TabItem) {
    if (tab.id === "principal") return;
    setEditingTabId(tab.id);
    setNewTabName(tab.nome);
    setShowTabBuilder(true);
  }

  function closeTabBuilder() {
    setEditingTabId("");
    setNewTabName("");
    setShowTabBuilder(false);
  }

  async function saveTab() {
    const rawLabel = String(newTabName ?? "").trim();
    if (!rawLabel) {
      setFailure("Informe um nome para a nova aba.");
      return;
    }

    const nome = normalizeTabName(rawLabel);
    const id = normalizeTabId(nome);
    const editingTab = tabItems.find((tab) => tab.id === editingTabId);

    const existing = tabItems.find((tab) => tab.id === id && tab.id !== editingTabId);
    if (existing) {
      setActiveTabId(existing.id);
      setNewTabName("");
      setFailure("Já existe uma aba com esse nome.");
      return;
    }

    if (editingTab) {
      const nextTabs = tabItems.map((tab) =>
        tab.id === editingTab.id
          ? {
              ...tab,
              id,
              nome,
            }
          : tab,
      );
      const overrides = new Map<string, string[]>();

      nextTabs.forEach((tab) => {
        if (tab.id === id) {
          overrides.set(nome, [...(orderedFieldNamesByTab.get(editingTab.nome) ?? [])]);
          return;
        }

        overrides.set(tab.nome, [...(orderedFieldNamesByTab.get(tab.nome) ?? [])]);
      });

      const success = await persistLayout(nextTabs, "Aba atualizada.", overrides);
      if (success) {
        setActiveTabId(id);
        setCustomFieldForm((current) => ({ ...current, aba: nome }));
        closeTabBuilder();
      }
      return;
    }

    const nextTabs = [
      ...tabItems,
      {
        id,
        nome,
        ordem: tabItems.length,
      },
    ];

    const success = await persistLayout(nextTabs, "Aba criada.");
    if (success) {
      setActiveTabId(id);
      setCustomFieldForm((current) => ({
        ...current,
        aba: nome,
      }));
      closeTabBuilder();
    }
  }

  function exportCsv(rows: ApiRecord[], fileName: string, selectedColumns = excelColumns) {
    const headers = selectedColumns.map((column) => column.label);
    const keys = selectedColumns.map((column) => column.key);

    const csv = [
      headers.join(";"),
      ...rows.map((row) =>
        keys.map((key) => `"${String(row[key] ?? "").replace(/"/g, '""')}"`).join(";"),
      ),
    ].join("\n");

    const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `${fileName}.csv`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  function printRecord(row: ApiRecord) {
    const html = pdfColumns
      .map(
        (column) =>
          `<dt>${escapeHtml(column.label)}</dt><dd>${escapeHtml(displayCellValue(row[column.key]))}</dd>`,
      )
      .join("");

    const popup = window.open("", "_blank", "width=900,height=700");
    if (!popup) return;

    popup.document.write(`
      <html>
        <head>
          <title>${escapeHtml(title)}</title>
          <style>
            body{font-family:Arial,sans-serif;padding:32px;color:#111827}
            h1{margin:0 0 18px}
            dl{display:grid;grid-template-columns:180px 1fr;gap:10px 18px}
            dt{font-weight:700;color:#475569}
            dd{margin:0;padding-bottom:8px;border-bottom:1px solid #e5e7eb}
          </style>
        </head>
        <body>
          <h1>${escapeHtml(title)}</h1>
          <dl>${html}</dl>
        </body>
      </html>
    `);

    popup.document.close();
    popup.focus();
    popup.print();
  }

  async function remove(row: ApiRecord) {
    const id = String(row.id ?? "");
    if (!id) return;

    const question =
      deleteMode === "inativar"
        ? "Deseja realmente inativar este registro?"
        : "Deseja realmente excluir este registro?";

    if (!window.confirm(question)) return;

    try {
      const path = deleteMode === "inativar" ? `${endpoint}/${id}/inativar` : `${endpoint}/${id}`;
      await apiRequest(path, { method: deleteMode === "inativar" ? "PATCH" : "DELETE" });

      setNotice(
        deleteMode === "inativar"
          ? "Registro inativado com sucesso."
          : "Registro excluído com sucesso.",
      );

      refresh();
      await onAfterChange?.();
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  const content = (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-600">
              <strong className="mr-1 text-slate-900">{filteredData.length}</strong>
              {filteredData.length === 1 ? "registro visível" : "registros visíveis"}
            </div>

            <label className="relative min-w-[260px] grow xl:max-w-md">
              <Search
                size={16}
                className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400"
              />
              <input
                className={`${inputClass} pl-11`}
                value={search}
                placeholder="Nome, documento, telefone, status..."
                onChange={(event) => setSearch(event.target.value)}
              />
            </label>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <button
              className={buttonClass("primary")}
              type="button"
              onClick={() => {
                if (showForm && !isEditing) {
                  resetForm();
                  return;
                }

                setEditingId("");
                setForm(blankForm);
                setValidationErrors({});
                setShowForm(true);
              }}
            >
              <Plus size={16} />
              {showForm && !isEditing ? "Fechar" : "Novo cadastro"}
            </button>

            <button
              className={buttonClass()}
              type="button"
              onClick={() => exportCsv(filteredData, title.toLowerCase().replace(/\s+/g, "-"))}
            >
              <Download size={16} />
              Excel
            </button>

            <button
              className={buttonClass()}
              type="button"
              onClick={() =>
                exportCsv(
                  filteredData,
                  `${title.toLowerCase().replace(/\s+/g, "-")}-resumido`,
                  excelResumoColumns,
                )
              }
            >
              <Download size={16} />
              Excel resumido
            </button>
          </div>
        </div>
      </div>

{showForm ? (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
    <div className="max-h-[92vh] w-full max-w-6xl overflow-y-auto rounded-3xl border border-slate-200 bg-white p-6 shadow-2xl">
      <form onSubmit={submit}>
        <div className="mb-5 flex flex-col gap-4 border-b border-slate-100 pb-5">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
            <div>
              <h2 className="text-xl font-bold tracking-tight text-slate-900">
                {isEditing ? `Editar ${title.toLowerCase()}` : `Novo ${title.toLowerCase()}`}
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                {isEditing
                  ? "Revise os dados e salve as alterações."
                  : "Preencha os campos para criar um novo registro."}
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              {(customModule && canManageCustomFields) ? (
                <>
                  <button
                    className={buttonClass()}
                    type="button"
                    onClick={() => {
                      setEditingCustomFieldId("");
                      setCustomFieldForm({
                        ...defaultForm(customFieldFormFields),
                        aba: activeTab,
                      });
                      setShowCustomBuilder(true);
                    }}
                  >
                    <Plus size={16} />
                    Adicionar campo
                  </button>

                  <button className={buttonClass()} type="button" onClick={openCreateTab}>
                    <Plus size={16} />
                    Criar aba
                  </button>

                  <button
                    className={buttonClass()}
                    type="button"
                    onClick={() => setLayoutMode((current) => !current)}
                  >
                    <SlidersHorizontal size={16} />
                    {layoutMode ? "Concluir organização" : "Organizar campos"}
                  </button>
                </>
              ) : null}

              <button className={buttonClass()} type="button" onClick={resetForm}>
                <X size={16} />
                Fechar
              </button>
            </div>
          </div>

          {customModule ? (
            <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex flex-col gap-4">
                <div className="flex flex-wrap gap-2">
                  {tabItems.map((tab) => (
                    <div
                      key={tab.id}
                      className={`inline-flex items-center overflow-hidden rounded-2xl text-sm font-medium transition ${
                        activeTabId === tab.id
                          ? "bg-slate-900 text-white"
                          : "border border-slate-200 bg-white text-slate-700 hover:bg-slate-100"
                      }`}
                    >
                      <button
                        type="button"
                        draggable={canManageCustomFields}
                        onDragStart={() => setDraggingTabId(tab.id)}
                        onDragOver={(event) => event.preventDefault()}
                        onDrop={() => void moveTab(tab.id)}
                        className="px-4 py-2.5"
                        onClick={() => setActiveTabId(tab.id)}
                      >
                        {tab.nome}
                      </button>

                      {canManageCustomFields && tab.id !== "principal" ? (
                        <button
                          type="button"
                          className={`border-l px-2.5 py-2.5 transition ${
                            activeTabId === tab.id
                              ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                              : "border-slate-200 text-slate-400 hover:bg-slate-50 hover:text-slate-700"
                          }`}
                          title="Editar nome da aba"
                          onClick={() => openEditTab(tab)}
                        >
                          <Pencil size={14} />
                        </button>
                      ) : null}

                      {canManageCustomFields && tab.id !== "principal" ? (
                        <button
                          type="button"
                          className={`border-l px-2.5 py-2.5 transition ${
                            activeTabId === tab.id
                              ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                              : "border-slate-200 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                          }`}
                          title="Excluir aba"
                          onClick={() => void deleteTab(tab)}
                        >
                          <X size={14} />
                        </button>
                      ) : null}
                    </div>
                  ))}
                </div>

                {layoutMode ? (
                  <Notice>
                    Modo de organização ativo. Arraste os campos dentro da aba atual para ajustar a
                    ordem e arraste as abas para reorganizar o topo.
                  </Notice>
                ) : null}
              </div>
            </div>
          ) : null}
        </div>

        <div className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {visibleOrderedFields.map(({ field }) => (
              <div key={field.name} className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}>
                <FieldRenderer
                  field={{ ...field, span: undefined }}
                  value={form[field.name]}
                  error={validationErrors[field.name]}
                  onChange={(name, value) => setField(name, value)}
                />
                {formFieldActions?.({ field, form, setField, isEditing })}
              </div>
            ))}
          </div>

          {showCustomBuilder ? (
            <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <div className="mb-4 flex items-center justify-between gap-3">
                <div>
                  <h3 className="text-base font-bold text-slate-900">
                    {editingCustomFieldId ? "Editar campo extra" : "Novo campo extra"}
                  </h3>
                  <p className="mt-1 text-sm text-slate-500">
                    Configure um campo adicional para este cadastro.
                  </p>
                </div>

                <button className={buttonClass()} type="button" onClick={resetCustomFieldState}>
                  Fechar
                </button>
              </div>

              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                {customFieldFormFields.map((field) => (
                  <FieldRenderer
                    key={field.name}
                    field={field}
                    value={customFieldForm[field.name]}
                    error={customFieldErrors[field.name]}
                    onChange={(value) =>
                      setCustomFieldForm((current) => ({ ...current, [field.name]: value }))
                    }
                  />
                ))}
              </div>

              <div className="mt-4 flex flex-wrap justify-end gap-3">
                {editingCustomFieldId ? (
                  <button className={buttonClass("danger")} type="button" onClick={() => void deleteCustomField()}>
                    <Trash2 size={16} />
                    Excluir campo
                  </button>
                ) : null}

                <button className={buttonClass("primary")} type="button" onClick={() => void saveCustomField(new Event("submit") as any)}>
                  <Plus size={16} />
                  Salvar campo
                </button>
              </div>
            </div>
          ) : null}
        </div>

        <div className="mt-6 flex flex-wrap justify-end gap-3 border-t border-slate-100 pt-5">
          <button className={buttonClass()} type="button" onClick={resetForm}>
            Cancelar
          </button>
          <button className={buttonClass("primary")} type="submit" disabled={saving}>
            {saving ? "Salvando..." : submitLabel}
          </button>
        </div>
      </form>
    </div>
  </div>
) : null}

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure || error ? <Notice type="error">{failure || error}</Notice> : null}

      <DataTable
        columns={columns}
        rows={filteredData}
        loading={loading}
        emptyText={
          search.trim()
            ? "Nenhum resultado encontrado para a busca informada."
            : emptyText
        }
        actions={
          allowEdit || allowDelete || rowActions
            ? (row) => (
                <div className="flex flex-wrap gap-2">
                  {rowActions?.(row, refresh)}

                  <button
                    className={buttonClass()}
                    type="button"
                    onClick={() => setViewingRow(row)}
                  >
                    <Eye size={15} />
                    Visualizar
                  </button>

                  <button
                    className={buttonClass()}
                    type="button"
                    onClick={() => printRecord(row)}
                  >
                    <FileText size={15} />
                    PDF
                  </button>

                  {allowEdit ? (
                    <button
                      className={buttonClass()}
                      type="button"
                      onClick={() => {
                        setEditingId(String(row.id ?? ""));
                        setForm(formFromRecord(allFields, row));
                        setValidationErrors({});
                        setShowForm(true);
                      }}
                    >
                      <Pencil size={15} />
                      Editar
                    </button>
                  ) : null}

                  {allowDelete ? (
                    <button
                      className={buttonClass("danger")}
                      type="button"
                      onClick={() => void remove(row)}
                    >
                      <Trash2 size={15} />
                      {deleteMode === "inativar" ? "Inativar" : "Excluir"}
                    </button>
                  ) : null}
                </div>
              )
            : undefined
        }
      />

      {viewingRow ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <section className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-[28px] border border-slate-200 bg-white p-6 shadow-2xl">
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h3 className="text-xl font-bold tracking-tight text-slate-900">{title}</h3>
                <p className="mt-1 text-sm text-slate-500">Visualização do cadastro.</p>
              </div>

              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
                onClick={() => setViewingRow(null)}
              >
                <X size={18} />
              </button>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {viewColumns.map((column) => (
                <div
                  key={column.key}
                  className="rounded-2xl border border-slate-200 bg-slate-50 p-4"
                >
                  <dt className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                    {column.label}
                  </dt>
                  <dd className="mt-2 text-sm text-slate-700">
                    {displayCellValue(viewingRow[column.key])}
                  </dd>
                </div>
              ))}
            </div>

            <div className="mt-6 flex flex-wrap justify-end gap-3">
              <button
                className={buttonClass()}
                type="button"
                onClick={() => setViewingRow(null)}
              >
                Fechar
              </button>

              <button
                className={buttonClass()}
                type="button"
                onClick={() => printRecord(viewingRow)}
              >
                <FileText size={15} />
                PDF
              </button>

              <button
                className={buttonClass("primary")}
                type="button"
                onClick={() => exportCsv([viewingRow], title.toLowerCase().replace(/\s+/g, "-"))}
              >
                <Download size={15} />
                Excel
              </button>
            </div>
          </section>
        </div>
      ) : null}

      {showTabBuilder ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <form
            className="w-full max-w-md rounded-[28px] border border-slate-200 bg-white p-6 shadow-2xl"
            onSubmit={(event) => {
              event.preventDefault();
              void saveTab();
            }}
          >
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h3 className="text-xl font-bold tracking-tight text-slate-900">
                  {editingTabId ? "Editar aba" : "Criar aba"}
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  Defina o nome da aba para organizar os campos deste cadastro.
                </p>
              </div>

              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
                onClick={closeTabBuilder}
              >
                <X size={18} />
              </button>
            </div>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Nome da aba</span>
              <input
                className={inputClass}
                value={newTabName}
                maxLength={80}
                autoFocus
                placeholder="Ex.: Dados adicionais"
                onChange={(event) => setNewTabName(event.target.value)}
              />
            </label>

            <div className="mt-6 flex flex-wrap justify-end gap-3">
              <button className={buttonClass()} type="button" onClick={closeTabBuilder}>
                Cancelar
              </button>
              <button className={buttonClass("primary")} type="submit">
                {editingTabId ? "Salvar nome" : "Criar aba"}
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {showCustomBuilder ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <form
            className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-[28px] border border-slate-200 bg-white p-6 shadow-2xl"
            onSubmit={saveCustomField}
          >
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h3 className="text-xl font-bold tracking-tight text-slate-900">
                  {editingCustomFieldId ? "Editar campo" : "Adicionar campo"}
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  {editingCustomFieldId
                    ? "O tipo fica bloqueado para preservar os dados já salvos."
                    : "Crie campos extras e organize a posição depois em Organizar campos."}
                </p>
              </div>

              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
                onClick={resetCustomFieldState}
              >
                <X size={18} />
              </button>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {customFieldFormFields
                .filter((field) => field.name !== "opcoesText" || customFieldForm.tipo === "select")
                .map((field) => (
                  <FieldRenderer
                    key={field.name}
                    field={
                      editingCustomFieldId && field.name === "tipo"
                        ? { ...field, disabled: true }
                        : field.name === "aba"
                          ? { ...field, type: "select", required: true, options: tabOptions }
                          : field
                    }
                    value={customFieldForm[field.name]}
                    error={customFieldErrors[field.name]}
                    onChange={(name, value) => {
                      setCustomFieldForm((current) => ({ ...current, [name]: value }));
                      setCustomFieldErrors((current) => {
                        const next = { ...current };
                        delete next[name];
                        return next;
                      });
                    }}
                  />
                ))}
            </div>

            <div className="mt-4">
              <Notice>Depois de criar, use “Organizar campos” dentro do formulário.</Notice>
            </div>

            <div className="mt-6 flex flex-wrap justify-end gap-3">
              {editingCustomFieldId ? (
                <button
                  className={buttonClass("danger")}
                  type="button"
                  onClick={() => void deleteCustomField()}
                >
                  <Trash2 size={16} />
                  Excluir campo
                </button>
              ) : null}

              <button
                className={buttonClass()}
                type="button"
                onClick={resetCustomFieldState}
              >
                Cancelar
              </button>

              <button className={buttonClass("primary")} type="submit">
                {editingCustomFieldId ? "Salvar campo" : "Criar campo"}
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {extraContent}
    </div>
  );

  if (embedded) return content;

  return (
    <PageFrame eyebrow={eyebrow} title={title} description={description}>
      {content}
    </PageFrame>
  );
}
