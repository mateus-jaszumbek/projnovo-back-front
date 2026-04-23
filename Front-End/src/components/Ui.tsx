import { AlertCircle, FileCheck2 } from "lucide-react";
import type { ChangeEvent, ReactNode } from "react";
import type { ApiRecord } from "../lib/api";
import {
  displayValue,
  formatFieldInput,
  inputModeForField,
  maxLengthForField,
} from "./uiHelpers";
import type { FieldMask } from "./uiHelpers";

export type Option = {
  value: string;
  label: string;
};

export type FieldType =
  | "text"
  | "email"
  | "password"
  | "number"
  | "currency"
  | "percentage"
  | "date"
  | "select"
  | "textarea"
  | "checkbox";

export type FieldConfig = {
  name: string;
  label: string;
  type?: FieldType;
  placeholder?: string;
  required?: boolean;
  options?: Option[];
  span?: "full";
  min?: number;
  max?: number;
  step?: string;
  helper?: string;
  disabled?: boolean;
  defaultValue?: unknown;
  minLength?: number;
  maxLength?: number;
  nullable?: boolean;
  mask?: FieldMask;
  strongPassword?: boolean;
  sameAs?: string;
  line?: number;
  position?: number;
};

export type ColumnConfig = {
  key: string;
  label: string;
  render?: (row: ApiRecord) => ReactNode;
};

type PageFrameProps = {
  eyebrow?: string;
  title: string;
  description: string;
  actions?: ReactNode;
  children: ReactNode;
};

type NoticeProps = {
  type?: "info" | "success" | "error";
  children: ReactNode;
};

type FieldRendererProps = {
  field: FieldConfig;
  value: unknown;
  error?: string;
  onChange: (name: string, value: unknown) => void;
};

type TextInputProps = {
  label: string;
  name: string;
  type?: string;
  value: string;
  placeholder?: string;
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  mask?: FieldMask;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
};

type TableProps = {
  columns: ColumnConfig[];
  rows: ApiRecord[];
  loading?: boolean;
  emptyText?: string;
  actions?: (row: ApiRecord) => ReactNode;
};

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 disabled:cursor-not-allowed disabled:bg-slate-100";

const textareaClass =
  "min-h-[120px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 disabled:cursor-not-allowed disabled:bg-slate-100 resize-y";

const labelClass = "mb-2 block text-sm font-medium text-slate-700";

function wrapperClass(field: FieldConfig) {
  return field.span === "full" ? "md:col-span-2 xl:col-span-3" : "";
}

export function PageFrame({
  eyebrow,
  title,
  description,
  actions,
  children,
}: PageFrameProps) {
  return (
    <section className="space-y-6">
      <div className="flex flex-col gap-4 rounded-3xl border border-slate-200 bg-white p-5 shadow-sm lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          {eyebrow ? (
            <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
              {eyebrow}
            </span>
          ) : null}

          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{title}</h1>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">{description}</p>
        </div>

        {actions ? (
          <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div>
        ) : null}
      </div>

      {children}
    </section>
  );
}

export function Notice({ type = "info", children }: NoticeProps) {
  const Icon = type === "success" ? FileCheck2 : AlertCircle;

  const classes =
    type === "success"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : type === "error"
        ? "border-rose-200 bg-rose-50 text-rose-700"
        : "border-sky-200 bg-sky-50 text-sky-700";

  return (
    <div
      className={`flex items-start gap-3 rounded-2xl border px-4 py-3 text-sm ${classes}`}
      role={type === "error" ? "alert" : "status"}
    >
      <Icon size={18} className="mt-0.5 shrink-0" />
      <span>{children}</span>
    </div>
  );
}

export function TextInput({
  label,
  name,
  type = "text",
  value,
  placeholder,
  required,
  minLength,
  maxLength,
  mask,
  onChange,
}: TextInputProps) {
  return (
    <label className="block" htmlFor={name}>
      <span className={labelClass}>{label}</span>
      <input
        id={name}
        name={name}
        className={inputClass}
        type={type}
        value={value}
        placeholder={placeholder}
        required={required}
        minLength={minLength}
        maxLength={maxLength ?? maxLengthForField({ name, label, mask })}
        inputMode={inputModeForField({ name, label, type: type as FieldType, mask })}
        onChange={(event) => {
          if (mask) {
            event.target.value = String(
              formatFieldInput({ name, label, mask, maxLength }, event.target.value),
            );
          }
          onChange(event);
        }}
      />
    </label>
  );
}

export function FieldRenderer({ field, value, error, onChange }: FieldRendererProps) {
  const fieldType = field.type ?? "text";
  const invalid = Boolean(error);
  const helper = error ? (
    <small className="mt-2 block text-xs font-medium text-rose-600">{error}</small>
  ) : field.helper ? (
    <small className="mt-2 block text-xs text-slate-500">{field.helper}</small>
  ) : null;

  if (fieldType === "textarea") {
    return (
      <div className={wrapperClass(field)}>
        <label className="block">
          <span className={labelClass}>{field.label}</span>
          <textarea
            name={field.name}
            className={textareaClass}
            value={String(value ?? "")}
            placeholder={field.placeholder}
            required={field.required}
            disabled={field.disabled}
            maxLength={field.maxLength}
            aria-invalid={invalid}
            onChange={(event) => onChange(field.name, formatFieldInput(field, event.target.value))}
          />
          {helper}
        </label>
      </div>
    );
  }

  if (fieldType === "select") {
    return (
      <div className={wrapperClass(field)}>
        <label className="block">
          <span className={labelClass}>{field.label}</span>
          <select
            name={field.name}
            className={inputClass}
            value={String(value ?? "")}
            required={field.required}
            disabled={field.disabled}
            aria-invalid={invalid}
            onChange={(event) => onChange(field.name, event.target.value)}
          >
            <option value="">Selecione</option>
            {(field.options ?? []).map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          {helper}
        </label>
      </div>
    );
  }

  if (fieldType === "checkbox") {
    return (
      <div className={wrapperClass(field)}>
        <label className="flex min-h-[44px] items-center gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
          <input
            name={field.name}
            type="checkbox"
            className="h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-400"
            checked={Boolean(value)}
            disabled={field.disabled}
            aria-invalid={invalid}
            onChange={(event) => onChange(field.name, event.target.checked)}
          />
          <span className="font-medium text-slate-700">{field.label}</span>
        </label>
        {helper}
      </div>
    );
  }

  return (
    <div className={wrapperClass(field)}>
      <label className="block">
        <span className={labelClass}>{field.label}</span>
        <input
          name={field.name}
          className={inputClass}
          type={
            field.mask === "money" || fieldType === "currency" || fieldType === "percentage"
              ? "text"
              : fieldType
          }
          min={field.min}
          max={field.max}
          step={field.step}
          value={String(value ?? "")}
          placeholder={field.placeholder}
          required={field.required}
          disabled={field.disabled}
          maxLength={maxLengthForField(field)}
          minLength={field.minLength}
          inputMode={inputModeForField(field)}
          aria-invalid={invalid}
          onChange={(event) => onChange(field.name, formatFieldInput(field, event.target.value))}
        />
        {helper}
      </label>
    </div>
  );
}

export function DataTable({
  columns,
  rows,
  loading,
  emptyText = "Nenhum registro encontrado.",
  actions,
}: TableProps) {
  function renderCell(column: ColumnConfig, row: ApiRecord) {
    const rendered = column.render?.(row);
    if (rendered !== undefined && rendered !== null) return rendered;

    const value = row[column.key];
    if (column.key.toLowerCase().includes("status") || column.key === "ativo") {
      return <StatusBadge value={value} />;
    }

    return displayValue(value);
  }

  return (
    <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="min-w-full border-collapse">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50">
              {columns.map((column) => (
                <th
                  key={column.key}
                  className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.12em] text-slate-500"
                >
                  {column.label}
                </th>
              ))}
              {actions ? (
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.12em] text-slate-500">
                  Ações
                </th>
              ) : null}
            </tr>
          </thead>

          <tbody>
            {loading ? (
              <tr>
                <td
                  className="px-4 py-8 text-sm text-slate-500"
                  colSpan={columns.length + (actions ? 1 : 0)}
                >
                  Carregando...
                </td>
              </tr>
            ) : null}

            {!loading && rows.length === 0 ? (
              <tr>
                <td
                  className="px-4 py-8 text-sm text-slate-500"
                  colSpan={columns.length + (actions ? 1 : 0)}
                >
                  {emptyText}
                </td>
              </tr>
            ) : null}

            {!loading
              ? rows.map((row, index) => (
                  <tr
                    key={String(row.id ?? index)}
                    className="border-b border-slate-100 last:border-b-0 hover:bg-slate-50/70"
                  >
                    {columns.map((column) => (
                      <td key={column.key} className="px-4 py-3 text-sm text-slate-700">
                        {renderCell(column, row)}
                      </td>
                    ))}
                    {actions ? (
                      <td className="px-4 py-3 text-sm text-slate-700">{actions(row)}</td>
                    ) : null}
                  </tr>
                ))
              : null}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function StatusBadge({ value }: { value: unknown }) {
  const label = displayValue(value);
  const normalized = String(value ?? "").toLowerCase();

  const tone =
    normalized === "true" ||
    ["ativo", "aberta", "aprovada", "pronta", "entregue", "finalizada", "pago", "emitida"].includes(
      normalized,
    )
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : ["false", "inativo", "cancelada", "rejeitada", "vencida"].includes(normalized)
        ? "border-rose-200 bg-rose-50 text-rose-700"
        : "border-slate-200 bg-slate-100 text-slate-700";

  return (
    <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${tone}`}>
      {label}
    </span>
  );
}