import type { FormEvent } from "react";
import { Plus, Trash2, X } from "lucide-react";

import type { ApiRecord } from "../lib/api";
import { FieldRenderer } from "./Ui";
import type { FieldConfig } from "./Ui";
import { customFieldFormFields } from "../hooks/useCustomModuleFields";

const buttonClass =
  "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50";
const primaryButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800";
const dangerButtonClass =
  "inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100";

type FieldsetProps = {
  dynamicFields: FieldConfig[];
  values: ApiRecord;
  errors: Record<string, string>;
  onChange: (name: string, value: unknown) => void;
  canManage: boolean;
  onAddField: () => void;
};

export function QuickCustomFieldsFieldset({
  dynamicFields,
  values,
  errors,
  onChange,
  canManage,
  onAddField,
}: FieldsetProps) {
  if (dynamicFields.length === 0 && !canManage) return null;

  return (
    <div className="space-y-3 border-t border-slate-100 pt-4">
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">
          Campos extras
        </span>
        {canManage ? (
          <button
            type="button"
            className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-600 transition hover:bg-slate-50"
            onClick={onAddField}
          >
            <Plus size={14} />
            Adicionar campo
          </button>
        ) : null}
      </div>

      {dynamicFields.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2">
          {dynamicFields.map((field) => (
            <FieldRenderer
              key={field.name}
              field={field}
              value={values[field.name]}
              error={errors[field.name]}
              onChange={onChange}
            />
          ))}
        </div>
      ) : (
        <p className="text-xs text-slate-400">Nenhum campo extra criado ainda.</p>
      )}
    </div>
  );
}

type BuilderModalProps = {
  open: boolean;
  editing: boolean;
  form: ApiRecord;
  errors: Record<string, string>;
  onChange: (name: string, value: unknown) => void;
  onClose: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onDelete: () => void;
};

export function QuickCustomFieldBuilderModal({
  open,
  editing,
  form,
  errors,
  onChange,
  onClose,
  onSubmit,
  onDelete,
}: BuilderModalProps) {
  if (!open) return null;

  const visibleFields = customFieldFormFields.filter(
    (field) => field.name !== "opcoesText" || form.tipo === "select",
  );

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/80 p-4 backdrop-blur-sm">
      <form
        className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-[28px] border border-slate-200 bg-white p-6 shadow-2xl"
        onSubmit={onSubmit}
      >
        <div className="mb-5 flex items-start justify-between gap-4">
          <div>
            <h3 className="text-xl font-bold tracking-tight text-slate-900">
              {editing ? "Editar campo" : "Adicionar campo"}
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              {editing
                ? "O tipo fica bloqueado para preservar os dados já salvos."
                : "Esse campo passa a existir também no cadastro completo deste módulo."}
            </p>
          </div>

          <button
            type="button"
            className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
            onClick={onClose}
          >
            <X size={18} />
          </button>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {visibleFields.map((field) => (
            <FieldRenderer
              key={field.name}
              field={editing && field.name === "tipo" ? { ...field, disabled: true } : field}
              value={form[field.name]}
              error={errors[field.name]}
              onChange={onChange}
            />
          ))}
        </div>

        <div className="mt-6 flex flex-wrap justify-end gap-3">
          {editing ? (
            <button type="button" className={dangerButtonClass} onClick={onDelete}>
              <Trash2 size={16} />
              Excluir campo
            </button>
          ) : null}

          <button type="button" className={buttonClass} onClick={onClose}>
            Cancelar
          </button>

          <button type="submit" className={primaryButtonClass}>
            {editing ? "Salvar campo" : "Criar campo"}
          </button>
        </div>
      </form>
    </div>
  );
}
