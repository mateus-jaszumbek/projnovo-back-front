import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";

import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { defaultForm, errorMessage, payloadFromForm, validateForm } from "../components/uiHelpers";
import type { FieldConfig } from "../components/Ui";

export type CustomField = {
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

export type CustomModule = {
  id: string;
  nome: string;
  campos?: CustomField[];
};

export const customFieldTypes = [
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

export const customFieldFormFields: FieldConfig[] = [
  { name: "nome", label: "Nome do campo", required: true, maxLength: 100 },
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

function normalizeTabName(value: unknown) {
  const text = String(value ?? "").trim();
  return text || "Principal";
}

/**
 * Gerencia os campos personalizados de um modulo do sistema (aparelhos, clientes, tecnicos...)
 * para uso em formularios compactos de "cadastro rapido". Os campos criados aqui sao os MESMOS
 * campos do cadastro completo (mesma chave de modulo), entao aparecem e salvam certo nos dois lugares.
 */
export function useCustomModuleFields(moduleKey: string, moduleName: string, canManage: boolean) {
  const [customModule, setCustomModule] = useState<CustomModule | null>(null);
  const [moduleReloadKey, setModuleReloadKey] = useState(0);
  const [error, setError] = useState("");

  const [showBuilder, setShowBuilder] = useState(false);
  const [editingFieldId, setEditingFieldId] = useState("");
  const [builderForm, setBuilderForm] = useState<ApiRecord>(() => defaultForm(customFieldFormFields));
  const [builderErrors, setBuilderErrors] = useState<Record<string, string>>({});

  const [extraValues, setExtraValues] = useState<ApiRecord>({});
  const [extraErrors, setExtraErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!moduleKey) return;
    let active = true;

    async function ensureModule() {
      try {
        const module = await apiRequest<CustomModule>("/modulos-personalizados/sistema", {
          method: "POST",
          body: {
            chave: moduleKey,
            nome: moduleName,
            descricao: `Campos extras de ${moduleName}`,
          },
        });

        if (active) setCustomModule(module);
      } catch (err) {
        if (active) setError(errorMessage(err));
      }
    }

    void ensureModule();

    return () => {
      active = false;
    };
  }, [moduleKey, moduleName, moduleReloadKey]);

  const customFields = useMemo(
    () => [...(customModule?.campos ?? [])].sort((a, b) => a.ordem - b.ordem),
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
      })),
    [customFields],
  );

  const customFieldByName = useMemo(
    () => new Map(customFields.map((field) => [field.chave, field])),
    [customFields],
  );

  useEffect(() => {
    setExtraValues((current) => ({ ...defaultForm(dynamicFields), ...current }));
  }, [dynamicFields]);

  function setExtraValue(name: string, value: unknown) {
    setExtraValues((current) => ({ ...current, [name]: value }));
    setExtraErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
  }

  function resetExtraValues() {
    setExtraValues(defaultForm(dynamicFields));
    setExtraErrors({});
  }

  function validateExtraValues() {
    const errors = validateForm(dynamicFields, extraValues);
    setExtraErrors(errors);
    return errors;
  }

  function resetBuilder() {
    setEditingFieldId("");
    setBuilderForm(defaultForm(customFieldFormFields));
    setBuilderErrors({});
    setShowBuilder(false);
  }

  function openCreateField() {
    if (!canManage) return;
    setEditingFieldId("");
    setBuilderForm(defaultForm(customFieldFormFields));
    setBuilderErrors({});
    setShowBuilder(true);
  }

  function openEditField(field: CustomField) {
    if (!canManage) return;
    setEditingFieldId(field.id);
    setBuilderForm({
      ...defaultForm(customFieldFormFields),
      nome: field.nome,
      tipo: field.tipo,
      obrigatorio: field.obrigatorio,
      exportarExcel: field.exportarExcel !== false,
      exportarExcelResumo: field.exportarExcelResumo === true,
      exportarPdf: field.exportarPdf !== false,
      opcoesText: (field.opcoes ?? []).join("\n"),
    });
    setBuilderErrors({});
    setShowBuilder(true);
  }

  function setBuilderField(name: string, value: unknown) {
    setBuilderForm((current) => ({ ...current, [name]: value }));
    setBuilderErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
  }

  async function saveField(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!customModule) return false;

    const validation = validateForm(customFieldFormFields, builderForm);
    if (Object.keys(validation).length > 0) {
      setBuilderErrors(validation);
      return false;
    }

    const payload = payloadFromForm(customFieldFormFields, builderForm);
    const principalFields = customFields.filter((field) => normalizeTabName(field.aba) === "Principal");
    const indexInTab = principalFields.length;
    const maxOrdem = customFields.reduce((max, field) => Math.max(max, field.ordem ?? 0), 0);

    const body = {
      nome: payload.nome,
      tipo: payload.tipo,
      obrigatorio: payload.obrigatorio,
      aba: "Principal",
      linha: Math.floor(indexInTab / 3) + 1,
      posicao: (indexInTab % 3) + 1,
      ordem: maxOrdem + 1,
      placeholder: null,
      valorPadrao: null,
      opcoes: String(builderForm.opcoesText ?? "")
        .split(/\r?\n/)
        .map((option) => option.trim())
        .filter(Boolean),
      exportarExcel: payload.exportarExcel !== false,
      exportarExcelResumo: payload.exportarExcelResumo === true,
      exportarPdf: payload.exportarPdf !== false,
      ativo: true,
    };

    try {
      if (editingFieldId) {
        const { tipo: _tipo, ...updateBody } = body;
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingFieldId}`, {
          method: "PUT",
          body: updateBody,
        });
      } else {
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos`, {
          method: "POST",
          body,
        });
      }

      resetBuilder();
      setModuleReloadKey((key) => key + 1);
      return true;
    } catch (err) {
      setError(errorMessage(err));
      return false;
    }
  }

  async function deleteField() {
    if (!customModule || !editingFieldId) return;

    try {
      await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingFieldId}`, {
        method: "DELETE",
      });
      resetBuilder();
      setModuleReloadKey((key) => key + 1);
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  async function saveValuesForRecord(originId: string) {
    if (!customModule || dynamicFields.length === 0 || !originId) return;

    const valores = payloadFromForm(dynamicFields, extraValues);
    await apiRequest(`/modulos-personalizados/${customModule.id}/registros/origem/${originId}`, {
      method: "PUT",
      body: { valores },
    });
  }

  return {
    customModule,
    dynamicFields,
    customFieldByName,
    canManage,
    error,

    extraValues,
    extraErrors,
    setExtraValue,
    resetExtraValues,
    validateExtraValues,
    saveValuesForRecord,

    showBuilder,
    editingFieldId,
    builderForm,
    builderErrors,
    openCreateField,
    openEditField,
    closeBuilder: resetBuilder,
    setBuilderField,
    saveField,
    deleteField,
  };
}

export type UseCustomModuleFields = ReturnType<typeof useCustomModuleFields>;
