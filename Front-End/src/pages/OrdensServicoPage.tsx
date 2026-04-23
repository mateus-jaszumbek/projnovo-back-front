import { useEffect, useMemo, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import {
  ArrowRight,
  ClipboardList,
  FileSpreadsheet,
  FileText,
  GripVertical,
  ImageUp,
  Mail,
  MessageCircle,
  Package,
  Pencil,
  Plus,
  RefreshCw,
  Trash2,
  Wrench,
  X,
} from "lucide-react";

import { useAuth } from "../auth/AuthContext";
import { DataTable, FieldRenderer, Notice, PageFrame } from "../components/Ui";
import type { ColumnConfig, FieldConfig } from "../components/Ui";
import { useList, useOptions } from "../hooks/useApi";
import { apiAbsoluteResourceUrl, apiRequest, apiResourceUrl, apiUpload } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import {
  defaultForm,
  errorMessage,
  formatCurrency,
  formatDate,
  payloadFromForm,
  validateForm,
} from "../components/uiHelpers";

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

type DetailTab = "resumo" | "itens" | "fotos";
type CreateStep = "dados" | "fotos" | "itens";

type FieldLayout = {
  campoChave: string;
  aba: string;
  linha: number;
  posicao: number;
  ordem: number;
};

type OrdemServicoFoto = {
  id: string;
  nomeArquivo: string;
  contentType: string;
  tamanhoBytes: number;
  descricao?: string | null;
  dataUrl: string;
  createdAt: string;
};

const statusOptions = [
  { value: "ABERTA", label: "Aberta" },
  { value: "APROVADA", label: "Aprovada" },
  { value: "EM_EXECUCAO", label: "Em execução" },
  { value: "PRONTA", label: "Pronta" },
  { value: "ENTREGUE", label: "Entregue" },
  { value: "CANCELADA", label: "Cancelada" },
];

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

function buttonClass(variant: "primary" | "secondary" | "danger" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800";
  }

  if (variant === "danger") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50";
}

function statusTone(status: string) {
  switch (status) {
    case "ABERTA":
      return "border-sky-200 bg-sky-50 text-sky-700";
    case "APROVADA":
      return "border-amber-200 bg-amber-50 text-amber-700";
    case "EM_EXECUCAO":
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

function tabClass(active: boolean) {
  return [
    "rounded-xl px-4 py-2 text-sm font-medium transition",
    active ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900",
  ].join(" ");
}

function InfoCard({
  icon,
  title,
  value,
  helper,
}: {
  icon: ReactNode;
  title: string;
  value: string;
  helper: string;
}) {
  return (
    <article className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
          {icon}
        </div>
        <div className="min-w-0">
          <span className="block text-sm text-slate-500">{title}</span>
          <strong className="mt-1 block text-2xl font-bold tracking-tight text-slate-900">
            {value}
          </strong>
          <small className="mt-1 block text-xs text-slate-400">{helper}</small>
        </div>
      </div>
    </article>
  );
}

function InfoRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
      <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
        {label}
      </span>
      <div className="mt-2 text-sm text-slate-700">{value}</div>
    </div>
  );
}

function OsPhotoPanel({
  title,
  helper,
  emptyText,
  uploadLabel,
  photos,
  description,
  imageUrl,
  photoLoading,
  onDescriptionChange,
  onImageUrlChange,
  onUpload,
  onUploadFromUrl,
  onDelete,
}: {
  title: string;
  helper: string;
  emptyText: string;
  uploadLabel: string;
  photos: OrdemServicoFoto[];
  description: string;
  imageUrl: string;
  photoLoading: boolean;
  onDescriptionChange: (value: string) => void;
  onImageUrlChange: (value: string) => void;
  onUpload: (file?: File | null) => void;
  onUploadFromUrl: () => void;
  onDelete: (fotoId: string) => void;
}) {
  return (
    <div className="space-y-5">
      <div className="rounded-3xl border border-slate-200 bg-slate-50 p-5">
        <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
        <p className="mt-1 text-sm text-slate-500">{helper}</p>

        <div className="mt-4 grid gap-3 lg:grid-cols-[1fr_auto]">
          <input
            className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
            value={description}
            maxLength={120}
            placeholder="Descricao opcional da foto"
            onChange={(event) => onDescriptionChange(event.target.value)}
          />
          <label className="inline-flex cursor-pointer items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800">
            <ImageUp size={16} />
            {photoLoading ? "Enviando..." : uploadLabel}
            <input
              className="sr-only"
              type="file"
              accept="image/png,image/jpeg,image/webp"
              disabled={photoLoading}
              onChange={(event) => {
                onUpload(event.target.files?.[0]);
                event.target.value = "";
              }}
            />
          </label>
        </div>

        <div className="mt-3 grid gap-3 lg:grid-cols-[1fr_auto]">
          <input
            className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
            value={imageUrl}
            placeholder="Ou cole a URL da imagem"
            onChange={(event) => onImageUrlChange(event.target.value)}
          />
          <button
            type="button"
            className={buttonClass()}
            disabled={photoLoading}
            onClick={onUploadFromUrl}
          >
            <ImageUp size={16} />
            Usar URL
          </button>
        </div>
      </div>

      {photos.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {photos.map((foto) => (
            <article
              key={foto.id}
              className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm"
            >
              <img
                src={apiResourceUrl(foto.dataUrl)}
                alt={foto.descricao || foto.nomeArquivo}
                className="h-56 w-full object-cover"
              />
              <div className="space-y-3 p-4">
                <div>
                  <strong className="block text-sm text-slate-900">
                    {foto.descricao || foto.nomeArquivo || "Foto da OS"}
                  </strong>
                  <span className="mt-1 block text-xs text-slate-500">
                    {Math.round(Number(foto.tamanhoBytes ?? 0) / 1024)} KB
                  </span>
                </div>
                <button
                  type="button"
                  className={buttonClass("danger")}
                  disabled={photoLoading}
                  onClick={() => onDelete(foto.id)}
                >
                  <Trash2 size={15} />
                  Excluir foto
                </button>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <div className="rounded-3xl border border-dashed border-slate-200 bg-slate-50 p-8 text-center text-sm text-slate-500">
          {emptyText}
        </div>
      )}
    </div>
  );
}

function normalizeTabName(value: unknown) {
  const text = String(value ?? "").trim();
  return text || "Principal";
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
  return `__tab__${normalizeTabName(tab).toLowerCase().replace(/[^a-z0-9]+/g, "_")}`;
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

function osExportText(row?: ApiRecord) {
  if (!row) return "";

  return [
    `OS: ${String(row.numeroOs ?? "-")}`,
    `Cliente: ${String(row.clienteNome ?? "-")}`,
    `Aparelho: ${String(row.aparelhoDescricao ?? "-")}`,
    `Técnico: ${String(row.tecnicoNome ?? "-")}`,
    `Status: ${String(row.status ?? "-")}`,
    `Entrada: ${formatDate(row.dataEntrada)}`,
    `Previsão: ${formatDate(row.dataPrevisao)}`,
    `Total: ${formatCurrency(row.valorTotal)}`,
    `Defeito relatado: ${String(row.defeitoRelatado ?? "-")}`,
    `Diagnóstico: ${String(row.diagnostico ?? "-")}`,
    `Laudo técnico: ${String(row.laudoTecnico ?? "-")}`,
    `Observações para o cliente: ${String(row.observacoesCliente ?? "-")}`,
    `Observações internas: ${String(row.observacoesInternas ?? "-")}`,
  ].join("\n");
}

function exportOsPdfResumo(row?: ApiRecord, customFields: CustomField[] = []) {
  if (!row) return;

  const popup = window.open("", "_blank", "width=900,height=700");
  if (!popup) return;

  const extraRows = customFields
    .filter((field) => field.exportarPdf !== false && field.exportarExcelResumo === true)
    .map(
      (field) =>
        `<div><strong>${escapeExportHtml(field.nome)}</strong><span>${escapeExportHtml(row[field.chave])}</span></div>`,
    )
    .join("");

  popup.document.write(`
    <html>
      <head>
        <title>OS ${escapeExportHtml(row.numeroOs)} - resumo</title>
        <style>
          * { box-sizing: border-box; }
          body { font-family: Arial, sans-serif; margin: 0; padding: 18px; color: #0f172a; font-size: 12px; }
          header { display: flex; justify-content: space-between; gap: 16px; border-bottom: 2px solid #0f172a; padding-bottom: 12px; margin-bottom: 14px; }
          .brand { display: flex; align-items: center; gap: 12px; }
          .company-logo { width: 74px; height: 74px; object-fit: contain; border: 1px solid #e2e8f0; border-radius: 8px; padding: 6px; }
          h1 { margin: 0; font-size: 22px; }
          .muted { color: #64748b; }
          .total { border: 1px solid #0f172a; border-radius: 8px; padding: 10px 14px; text-align: right; min-width: 150px; }
          .total span { display: block; font-size: 11px; color: #64748b; }
          .total strong { display: block; font-size: 20px; margin-top: 3px; }
          .grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; }
          .grid div { border: 1px solid #e2e8f0; border-radius: 8px; padding: 8px; min-height: 48px; }
          strong { display: block; font-size: 10px; text-transform: uppercase; color: #475569; margin-bottom: 4px; }
          .wide { grid-column: span 2; }
          .full { grid-column: 1 / -1; }
          @media print { body { padding: 10mm; } .grid { gap: 6px; } }
        </style>
      </head>
      <body>
        <header>
          <div class="brand">
            ${osLogoHtml(row)}
            <div>
              <h1>OS ${escapeExportHtml(row.numeroOs)}</h1>
              <div class="muted">Resumo para impressao</div>
            </div>
          </div>
          <div class="total"><span>Total</span><strong>${formatCurrency(row.valorTotal)}</strong></div>
        </header>
        <div class="grid">
          <div class="wide"><strong>Cliente</strong><span>${escapeExportHtml(row.clienteNome)}</span></div>
          <div class="wide"><strong>Aparelho</strong><span>${escapeExportHtml(row.aparelhoDescricao)}</span></div>
          <div><strong>Status</strong><span>${escapeExportHtml(row.status)}</span></div>
          <div><strong>Tecnico</strong><span>${escapeExportHtml(row.tecnicoNome)}</span></div>
          <div><strong>Entrada</strong><span>${formatDate(row.dataEntrada)}</span></div>
          <div><strong>Previsao</strong><span>${formatDate(row.dataPrevisao)}</span></div>
          <div class="full"><strong>Defeito relatado</strong><span>${escapeExportHtml(row.defeitoRelatado)}</span></div>
          <div class="wide"><strong>Diagnostico</strong><span>${escapeExportHtml(row.diagnostico)}</span></div>
          <div class="wide"><strong>Laudo tecnico</strong><span>${escapeExportHtml(row.laudoTecnico)}</span></div>
          ${extraRows}
        </div>
      </body>
    </html>
  `);

  popup.document.close();
  popup.focus();
  popup.print();
}

function exportOsExcel(row?: ApiRecord) {
  if (!row) return;

  const headers = [
    "OS",
    "Cliente",
    "Aparelho",
    "Técnico",
    "Status",
    "Entrada",
    "Previsão",
    "Total",
    "Defeito relatado",
    "Diagnóstico",
    "Laudo técnico",
    "Obs. cliente",
    "Obs. internas",
  ];

  const values = [
    String(row.numeroOs ?? "-"),
    String(row.clienteNome ?? "-"),
    String(row.aparelhoDescricao ?? "-"),
    String(row.tecnicoNome ?? "-"),
    String(row.status ?? "-"),
    formatDate(row.dataEntrada),
    formatDate(row.dataPrevisao),
    formatCurrency(row.valorTotal),
    String(row.defeitoRelatado ?? "-"),
    String(row.diagnostico ?? "-"),
    String(row.laudoTecnico ?? "-"),
    String(row.observacoesCliente ?? "-"),
    String(row.observacoesInternas ?? "-"),
  ];

  const csv = [
    headers.map((item) => `"${item.replace(/"/g, '""')}"`).join(";"),
    values.map((item) => `"${item.replace(/"/g, '""')}"`).join(";"),
  ].join("\n");

  const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `os-${String(row.numeroOs ?? "sem-numero")}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

function sendOsWhatsApp(row?: ApiRecord) {
  if (!row) return;
  const text = encodeURIComponent(osExportText(row));
  window.open(`https://wa.me/?text=${text}`, "_blank");
}

function sendOsEmail(row?: ApiRecord) {
  if (!row) return;
  const subject = encodeURIComponent(`Ordem de Serviço ${String(row.numeroOs ?? "-")}`);
  const body = encodeURIComponent(osExportText(row));
  window.location.href = `mailto:?subject=${subject}&body=${body}`;
}

function displayExportValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "boolean") return value ? "Sim" : "Nao";
  return String(value);
}

function escapeExportHtml(value: unknown) {
  return displayExportValue(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function osLogoHtml(row?: ApiRecord) {
  const logo = apiAbsoluteResourceUrl(String(row?.empresaLogoUrl ?? ""));
  if (!logo) return "";
  return `<img class="company-logo" src="${escapeExportHtml(logo)}" alt="Logo" />`;
}

function osFotos(row?: ApiRecord): OrdemServicoFoto[] {
  return Array.isArray(row?.fotos) ? (row.fotos as OrdemServicoFoto[]) : [];
}

function osFotosHtml(row?: ApiRecord) {
  const fotos = osFotos(row);
  if (fotos.length === 0) return "";

  return `
    <h2>Fotos do aparelho</h2>
    <div class="photos">
      ${fotos
        .map(
          (foto) => `
            <figure>
              <img src="${escapeExportHtml(apiAbsoluteResourceUrl(foto.dataUrl))}" alt="${escapeExportHtml(foto.descricao || foto.nomeArquivo)}" />
              <figcaption>${escapeExportHtml(foto.descricao || foto.nomeArquivo)}</figcaption>
            </figure>
          `,
        )
        .join("")}
    </div>
  `;
}

function csvCell(value: unknown) {
  return `"${displayExportValue(value).replace(/"/g, '""')}"`;
}

function downloadOsCsv(row: ApiRecord, suffix: string, csv: string) {
  const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `os-${String(row.numeroOs ?? "sem-numero")}${suffix}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

function osExportTextCompleto(
  row?: ApiRecord,
  customFields: CustomField[] = [],
  itens: ApiRecord[] = [],
  itemCustomFields: CustomField[] = [],
) {
  if (!row) return "";

  const linhas = [
    `OS: ${displayExportValue(row.numeroOs)}`,
    `Cliente: ${displayExportValue(row.clienteNome)}`,
    `Aparelho: ${displayExportValue(row.aparelhoDescricao)}`,
    `Tecnico: ${displayExportValue(row.tecnicoNome)}`,
    `Status: ${displayExportValue(row.status)}`,
    `Entrada: ${formatDate(row.dataEntrada)}`,
    `Previsao: ${formatDate(row.dataPrevisao)}`,
    `Total: ${formatCurrency(row.valorTotal)}`,
    `Defeito relatado: ${displayExportValue(row.defeitoRelatado)}`,
    `Diagnostico: ${displayExportValue(row.diagnostico)}`,
    `Laudo tecnico: ${displayExportValue(row.laudoTecnico)}`,
    `Observacoes para o cliente: ${displayExportValue(row.observacoesCliente)}`,
    `Observacoes internas: ${displayExportValue(row.observacoesInternas)}`,
  ];

  const extras = customFields.filter(
    (field) => field.exportarPdf !== false || field.exportarExcel !== false,
  );

  if (extras.length > 0) {
    linhas.push("", "Campos extras:");
    extras.forEach((field) => {
      linhas.push(`${field.nome}: ${displayExportValue(row[field.chave])}`);
    });
  }

  if (itens.length > 0) {
    const itemExtras = itemCustomFields.filter(
      (field) => field.exportarPdf !== false || field.exportarExcel !== false,
    );

    linhas.push("", "Itens:");
    itens.forEach((item, index) => {
      linhas.push(
        `${index + 1}. ${displayExportValue(item.tipoItem)} - ${displayExportValue(item.descricao)} | Qtd: ${displayExportValue(item.quantidade)} | Unit.: ${formatCurrency(item.valorUnitario)} | Total: ${formatCurrency(item.valorTotal)}`,
      );
      itemExtras.forEach((field) => {
        linhas.push(`   ${field.nome}: ${displayExportValue(item[field.chave])}`);
      });
    });
  }

  return linhas.join("\n");
}

function exportOsPdfCompleto(
  row?: ApiRecord,
  customFields: CustomField[] = [],
  itens: ApiRecord[] = [],
  itemCustomFields: CustomField[] = [],
) {
  if (!row) return;

  const popup = window.open("", "_blank", "width=900,height=700");
  if (!popup) return;

  const totalItens = itens.reduce((total, item) => total + Number(item.valorTotal ?? 0), 0);

  const extraRows = customFields
    .filter((field) => field.exportarPdf !== false)
    .map(
      (field) =>
        `<div class="label">${escapeExportHtml(field.nome)}</div><div class="value">${escapeExportHtml(row[field.chave])}</div>`,
    )
    .join("");

  const itemExtraFields = itemCustomFields.filter((field) => field.exportarPdf !== false);
  const itemExtraHeaders = itemExtraFields
    .map((field) => `<th>${escapeExportHtml(field.nome)}</th>`)
    .join("");

  const itemRows = itens
    .map(
      (item, index) => `
        <tr>
          <td>${index + 1}</td>
          <td>${escapeExportHtml(item.tipoItem)}</td>
          <td>${escapeExportHtml(item.descricao)}</td>
          <td>${escapeExportHtml(item.quantidade)}</td>
          <td>${escapeExportHtml(formatCurrency(item.valorUnitario))}</td>
          <td>${escapeExportHtml(formatCurrency(item.valorTotal))}</td>
          ${itemExtraFields.map((field) => `<td>${escapeExportHtml(item[field.chave])}</td>`).join("")}
        </tr>
      `,
    )
    .join("");

  popup.document.write(`
    <html>
      <head>
        <title>OS ${escapeExportHtml(row.numeroOs)}</title>
        <style>
          * { box-sizing: border-box; }
          body { font-family: Arial, sans-serif; padding: 28px; color: #0f172a; font-size: 13px; }
          header { display: flex; justify-content: space-between; gap: 20px; border-bottom: 2px solid #0f172a; padding-bottom: 14px; margin-bottom: 20px; }
          .brand { display: flex; align-items: center; gap: 14px; }
          .company-logo { width: 82px; height: 82px; object-fit: contain; border: 1px solid #e2e8f0; border-radius: 8px; padding: 6px; }
          h1 { margin: 0; font-size: 26px; }
          h2 { margin: 24px 0 10px; font-size: 17px; border-bottom: 1px solid #e2e8f0; padding-bottom: 6px; }
          .muted { color: #64748b; }
          .summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; min-width: 280px; }
          .summary div { border: 1px solid #e2e8f0; border-radius: 8px; padding: 8px; text-align: right; }
          .summary span { display: block; color: #64748b; font-size: 11px; }
          .summary strong { display: block; margin-top: 4px; font-size: 16px; }
          .grid { display: grid; grid-template-columns: 170px 1fr; gap: 9px 16px; }
          .label { font-weight: 700; color: #475569; }
          .value { border-bottom: 1px solid #e5e7eb; padding-bottom: 8px; }
          table { width: 100%; border-collapse: collapse; margin-top: 8px; }
          th, td { border-bottom: 1px solid #e5e7eb; padding: 8px; text-align: left; font-size: 13px; }
          th { color: #475569; }
          tfoot td { font-weight: 700; border-top: 2px solid #cbd5e1; }
          .photos { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-top: 10px; }
          figure { margin: 0; border: 1px solid #e2e8f0; border-radius: 8px; padding: 8px; break-inside: avoid; }
          figure img { width: 100%; height: 150px; object-fit: cover; border-radius: 6px; }
          figcaption { margin-top: 6px; color: #475569; font-size: 11px; }
          @media print { body { padding: 16px; } }
        </style>
      </head>
      <body>
        <header>
          <div class="brand">
            ${osLogoHtml(row)}
            <div>
              <h1>Ordem de Servico ${escapeExportHtml(row.numeroOs)}</h1>
              <div class="muted">Relatorio completo com campos extras, pecas e servicos.</div>
            </div>
          </div>
          <div class="summary">
            <div><span>Mao de obra</span><strong>${formatCurrency(row.valorMaoObra)}</strong></div>
            <div><span>Pecas</span><strong>${formatCurrency(row.valorPecas ?? totalItens)}</strong></div>
            <div><span>Total</span><strong>${formatCurrency(row.valorTotal)}</strong></div>
          </div>
        </header>
        <h2>Dados principais</h2>
        <div class="grid">
          <div class="label">Cliente</div><div class="value">${escapeExportHtml(row.clienteNome)}</div>
          <div class="label">Aparelho</div><div class="value">${escapeExportHtml(row.aparelhoDescricao)}</div>
          <div class="label">Tecnico</div><div class="value">${escapeExportHtml(row.tecnicoNome)}</div>
          <div class="label">Status</div><div class="value">${escapeExportHtml(row.status)}</div>
          <div class="label">Entrada</div><div class="value">${formatDate(row.dataEntrada)}</div>
          <div class="label">Previsao</div><div class="value">${formatDate(row.dataPrevisao)}</div>
          <div class="label">Total</div><div class="value">${formatCurrency(row.valorTotal)}</div>
          <div class="label">Defeito relatado</div><div class="value">${escapeExportHtml(row.defeitoRelatado)}</div>
          <div class="label">Diagnostico</div><div class="value">${escapeExportHtml(row.diagnostico)}</div>
          <div class="label">Laudo tecnico</div><div class="value">${escapeExportHtml(row.laudoTecnico)}</div>
          <div class="label">Obs. cliente</div><div class="value">${escapeExportHtml(row.observacoesCliente)}</div>
          <div class="label">Obs. internas</div><div class="value">${escapeExportHtml(row.observacoesInternas)}</div>
          ${extraRows}
        </div>
        ${osFotosHtml(row)}
        <h2>Itens</h2>
        <table>
          <thead>
            <tr><th>#</th><th>Tipo</th><th>Descricao</th><th>Qtd.</th><th>Unitario</th><th>Total</th>${itemExtraHeaders}</tr>
          </thead>
          <tbody>${itemRows || `<tr><td colspan="${6 + itemExtraFields.length}">Nenhum item adicionado.</td></tr>`}</tbody>
          <tfoot>
            <tr><td colspan="${5 + itemExtraFields.length}">Total dos itens</td><td>${escapeExportHtml(formatCurrency(totalItens))}</td></tr>
          </tfoot>
        </table>
      </body>
    </html>
  `);

  popup.document.close();
  popup.focus();
  popup.print();
}

function exportOsExcelCompleto(
  row?: ApiRecord,
  customFields: CustomField[] = [],
  itens: ApiRecord[] = [],
  itemCustomFields: CustomField[] = [],
) {
  if (!row) return;
  if (customFields.length === 0 && itens.length === 0 && itemCustomFields.length === 0) {
    exportOsExcel(row);
    return;
  }

  const extraFields = customFields.filter((field) => field.exportarExcel !== false);
  const itemExtraFields = itemCustomFields.filter((field) => field.exportarExcel !== false);
  const headers = [
    "OS",
    "Cliente",
    "Aparelho",
    "Tecnico",
    "Status",
    "Entrada",
    "Previsao",
    "Total",
    "Defeito relatado",
    "Diagnostico",
    "Laudo tecnico",
    "Obs. cliente",
    "Obs. internas",
    ...extraFields.map((field) => field.nome),
  ];

  const values = [
    displayExportValue(row.numeroOs),
    displayExportValue(row.clienteNome),
    displayExportValue(row.aparelhoDescricao),
    displayExportValue(row.tecnicoNome),
    displayExportValue(row.status),
    formatDate(row.dataEntrada),
    formatDate(row.dataPrevisao),
    formatCurrency(row.valorTotal),
    displayExportValue(row.defeitoRelatado),
    displayExportValue(row.diagnostico),
    displayExportValue(row.laudoTecnico),
    displayExportValue(row.observacoesCliente),
    displayExportValue(row.observacoesInternas),
    ...extraFields.map((field) => displayExportValue(row[field.chave])),
  ];

  const csv = [
    headers.map(csvCell).join(";"),
    values.map(csvCell).join(";"),
    "",
    ["Item", "Tipo", "Descricao", "Quantidade", "Unitario", "Total", ...itemExtraFields.map((field) => field.nome)]
      .map(csvCell)
      .join(";"),
    ...itens.map((item, index) =>
      [
        String(index + 1),
        displayExportValue(item.tipoItem),
        displayExportValue(item.descricao),
        displayExportValue(item.quantidade),
        formatCurrency(item.valorUnitario),
        formatCurrency(item.valorTotal),
        ...itemExtraFields.map((field) => displayExportValue(item[field.chave])),
      ]
        .map(csvCell)
        .join(";"),
    ),
  ].join("\n");

  downloadOsCsv(row, "", csv);
}

function exportOsExcelResumo(row?: ApiRecord, customFields: CustomField[] = []) {
  if (!row) return;

  const extraFields = customFields.filter((field) => field.exportarExcelResumo === true);
  const headers = [
    "OS",
    "Cliente",
    "Aparelho",
    "Tecnico",
    "Status",
    "Entrada",
    "Previsao",
    "Total",
    ...extraFields.map((field) => field.nome),
  ];

  const values = [
    displayExportValue(row.numeroOs),
    displayExportValue(row.clienteNome),
    displayExportValue(row.aparelhoDescricao),
    displayExportValue(row.tecnicoNome),
    displayExportValue(row.status),
    formatDate(row.dataEntrada),
    formatDate(row.dataPrevisao),
    formatCurrency(row.valorTotal),
    ...extraFields.map((field) => displayExportValue(row[field.chave])),
  ];

  const csv = [
    headers.map(csvCell).join(";"),
    values.map(csvCell).join(";"),
  ].join("\n");

  downloadOsCsv(row, "-resumido", csv);
}

function sendOsWhatsAppCompleto(
  row?: ApiRecord,
  customFields: CustomField[] = [],
  itens: ApiRecord[] = [],
  itemCustomFields: CustomField[] = [],
) {
  if (!row) return;
  if (customFields.length === 0 && itens.length === 0 && itemCustomFields.length === 0) {
    sendOsWhatsApp(row);
    return;
  }
  const text = encodeURIComponent(osExportTextCompleto(row, customFields, itens, itemCustomFields));
  window.open(`https://wa.me/?text=${text}`, "_blank");
}

function sendOsEmailCompleto(
  row?: ApiRecord,
  customFields: CustomField[] = [],
  itens: ApiRecord[] = [],
  itemCustomFields: CustomField[] = [],
) {
  if (!row) return;
  if (customFields.length === 0 && itens.length === 0 && itemCustomFields.length === 0) {
    sendOsEmail(row);
    return;
  }
  const subject = encodeURIComponent(`Ordem de Servico ${String(row.numeroOs ?? "-")}`);
  const body = encodeURIComponent(osExportTextCompleto(row, customFields, itens, itemCustomFields));
  window.location.href = `mailto:?subject=${subject}&body=${body}`;
}

export function OrdensServicoPage() {
  const { session } = useAuth();

  const userRole = String(session?.perfil ?? "").toLowerCase();
  const canManageCustomFields = Boolean(
    session?.isSuperAdmin ||
      ["owner", "admin", "administrador", "super-admin", "superadmin"].includes(userRole),
  );

  const [osOptionsReloadKey, setOsOptionsReloadKey] = useState(0);

  const clientesBase = useList("/clientes", osOptionsReloadKey);
  const aparelhosBase = useList("/aparelhos", osOptionsReloadKey);
  const tecnicosBase = useList("/tecnicos", osOptionsReloadKey);
  const servicos = useOptions(
    "/servicos-catalogo",
    (item) => `${item.nome ?? "Serviço"} - ${formatCurrency(item.valorPadrao)}`,
  );
  const pecas = useOptions(
    "/pecas",
    (item) => `${item.nome ?? "Peça"} - ${formatCurrency(item.precoVenda)}`,
  );

  const [reloadKey, setReloadKey] = useState(0);
  const [itemsReloadKey, setItemsReloadKey] = useState(0);
  const [selectedOsId, setSelectedOsId] = useState("");
  const [status, setStatus] = useState("APROVADA");
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [osPhotos, setOsPhotos] = useState<OrdemServicoFoto[]>([]);
  const [photoDescription, setPhotoDescription] = useState("");
  const [photoUrl, setPhotoUrl] = useState("");
  const [photoLoading, setPhotoLoading] = useState(false);
  const [osErrors, setOsErrors] = useState<Record<string, string>>({});
  const [itemErrors, setItemErrors] = useState<Record<string, string>>({});
  const [editingItemId, setEditingItemId] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [createStep, setCreateStep] = useState<CreateStep>("dados");
  const [detailTab, setDetailTab] = useState<DetailTab>("resumo");
  const [createdOs, setCreatedOs] = useState<ApiRecord | null>(null);

  const [quickClienteOpen, setQuickClienteOpen] = useState(false);
  const [quickAparelhoOpen, setQuickAparelhoOpen] = useState(false);
  const [quickTecnicoOpen, setQuickTecnicoOpen] = useState(false);

  const [quickClienteForm, setQuickClienteForm] = useState({
    nome: "",
    telefone: "",
    email: "",
  });

  const [quickAparelhoForm, setQuickAparelhoForm] = useState({
    marca: "",
    modelo: "",
    cor: "",
    imei: "",
    serialNumber: "",
  });
  const [quickImeiLoading, setQuickImeiLoading] = useState(false);
  const [quickImeiMessage, setQuickImeiMessage] = useState("");

  const [quickTecnicoForm, setQuickTecnicoForm] = useState({
    nome: "",
    telefone: "",
    email: "",
    especialidade: "",
  });

  const [customModule, setCustomModule] = useState<CustomModule | null>(null);
  const [showCustomBuilder, setShowCustomBuilder] = useState(false);
  const [editingCustomFieldId, setEditingCustomFieldId] = useState("");
  const [customFieldForm, setCustomFieldForm] = useState<ApiRecord>(() =>
    defaultForm(customFieldFormFields),
  );
  const [customFieldErrors, setCustomFieldErrors] = useState<Record<string, string>>({});
  const [customReloadKey, setCustomReloadKey] = useState(0);

  const [itemCustomModule, setItemCustomModule] = useState<CustomModule | null>(null);
  const [showItemCustomBuilder, setShowItemCustomBuilder] = useState(false);
  const [editingItemCustomFieldId, setEditingItemCustomFieldId] = useState("");
  const [itemCustomFieldForm, setItemCustomFieldForm] = useState<ApiRecord>(() =>
    defaultForm(customFieldFormFields),
  );
  const [itemCustomFieldErrors, setItemCustomFieldErrors] = useState<Record<string, string>>({});
  const [itemCustomReloadKey, setItemCustomReloadKey] = useState(0);

  const [osLayoutMode, setOsLayoutMode] = useState(false);
  const [itemLayoutMode, setItemLayoutMode] = useState(false);
  const [draggingOsFieldName, setDraggingOsFieldName] = useState("");
  const [draggingOsTabName, setDraggingOsTabName] = useState("");
  const [draggingItemFieldName, setDraggingItemFieldName] = useState("");
  const [draggingItemTabName, setDraggingItemTabName] = useState("");
  const [draggingItemId, setDraggingItemId] = useState("");
  const [osActiveTab, setOsActiveTab] = useState("Principal");
  const [osNewTabName, setOsNewTabName] = useState("");
  const [showOsCreateTabForm, setShowOsCreateTabForm] = useState(false);
  const [editingOsTabName, setEditingOsTabName] = useState("");
  const [itemActiveTab, setItemActiveTab] = useState("Principal");
  const [itemNewTabName, setItemNewTabName] = useState("");
  const [showItemCreateTabForm, setShowItemCreateTabForm] = useState(false);
  const [editingItemTabName, setEditingItemTabName] = useState("");

  const osFields: FieldConfig[] = [
    { name: "clienteId", label: "Cliente", type: "select", required: true, options: [] },
    { name: "aparelhoId", label: "Aparelho", type: "select", required: true, options: [] },
    { name: "tecnicoId", label: "Técnico", type: "select", options: [] },
    {
      name: "dataPrevisao",
      label: "Previsão",
      type: "date",
    },
    {
      name: "valorMaoObra",
      label: "Mão de obra",
      type: "number",
      min: 0,
      step: "0.01",
      defaultValue: 0,
      mask: "money",
    },
    {
      name: "desconto",
      label: "Desconto",
      type: "number",
      min: 0,
      step: "0.01",
      defaultValue: 0,
      mask: "money",
    },
    {
      name: "garantiaDias",
      label: "Garantia em dias",
      type: "number",
      min: 0,
      defaultValue: 0,
    },
    {
      name: "defeitoRelatado",
      label: "Defeito relatado",
      type: "textarea",
      required: true,
      span: "full",
      maxLength: 1000,
    },
    {
      name: "diagnostico",
      label: "Diagnóstico",
      type: "textarea",
      span: "full",
      maxLength: 1000,
    },
    {
      name: "laudoTecnico",
      label: "Laudo técnico",
      type: "textarea",
      span: "full",
      maxLength: 1000,
    },
    {
      name: "observacoesCliente",
      label: "Observações para o cliente",
      type: "textarea",
      span: "full",
      maxLength: 1000,
    },
    {
      name: "observacoesInternas",
      label: "Observações internas",
      type: "textarea",
      span: "full",
      maxLength: 1000,
    },
  ];

  const itemFields: FieldConfig[] = [
    {
      name: "tipoItem",
      label: "Tipo",
      type: "select",
      required: true,
      defaultValue: "SERVICO",
      options: [
        { value: "SERVICO", label: "Serviço" },
        { value: "PECA", label: "Peça" },
      ],
    },
    { name: "servicoCatalogoId", label: "Serviço", type: "select", options: servicos },
    { name: "pecaId", label: "Peça", type: "select", options: pecas },
    { name: "descricao", label: "Descrição manual", maxLength: 200 },
    {
      name: "quantidade",
      label: "Quantidade",
      type: "number",
      min: 0,
      step: "0.001",
      defaultValue: 1,
    },
    {
      name: "valorUnitario",
      label: "Valor unitário",
      type: "number",
      min: 0,
      step: "0.01",
      nullable: true,
      mask: "money",
    },
    {
      name: "desconto",
      label: "Desconto",
      type: "number",
      min: 0,
      step: "0.01",
      defaultValue: 0,
      mask: "money",
    },
  ];

  const [osForm, setOsForm] = useState<ApiRecord>(() => defaultForm(osFields));
  const [itemForm, setItemForm] = useState<ApiRecord>(() => defaultForm(itemFields));

  const clienteSelecionadoId = String(osForm.clienteId ?? "").trim();
  const clienteSelecionado = Boolean(clienteSelecionadoId);

  const clienteOptions = useMemo(
    () =>
      (clientesBase.data ?? []).map((item: any) => ({
        value: String(item.id ?? ""),
        label: String(item.nome ?? ""),
      })),
    [clientesBase.data],
  );

  const tecnicoOptions = useMemo(
    () =>
      (tecnicosBase.data ?? []).map((item: any) => ({
        value: String(item.id ?? ""),
        label: String(item.nome ?? ""),
      })),
    [tecnicosBase.data],
  );

  const aparelhoOptions = useMemo(
    () =>
      (aparelhosBase.data ?? [])
        .filter((item: any) =>
          !clienteSelecionadoId || String(item.clienteId ?? "") === clienteSelecionadoId,
        )
        .map((item: any) => ({
          value: String(item.id ?? ""),
          label: `${String(item.marca ?? "")} ${String(item.modelo ?? "")}`.trim(),
        })),
    [aparelhosBase.data, clienteSelecionadoId],
  );

  const ordens = useList("/ordens-servico", reloadKey);
  const itens = useList(selectedOsId ? `/ordens-servico/${selectedOsId}/itens` : "", itemsReloadKey);

  const customRecords = useList(
    customModule ? `/modulos-personalizados/${customModule.id}/registros` : "",
    customReloadKey,
  );

  const osLayoutList = useList(
    customModule ? `/modulos-personalizados/${customModule.id}/layout` : "",
    customReloadKey,
  );

  const itemCustomRecords = useList(
    itemCustomModule ? `/modulos-personalizados/${itemCustomModule.id}/registros` : "",
    itemCustomReloadKey,
  );

  const itemLayoutList = useList(
    itemCustomModule ? `/modulos-personalizados/${itemCustomModule.id}/layout` : "",
    itemCustomReloadKey,
  );

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

  const itemCustomFields = useMemo(
    () =>
      [...(itemCustomModule?.campos ?? [])].sort(
        (a, b) =>
          normalizeTabName(a.aba).localeCompare(normalizeTabName(b.aba)) ||
          a.linha - b.linha ||
          a.posicao - b.posicao ||
          a.ordem - b.ordem,
      ),
    [itemCustomModule],
  );

  const dynamicOsFields = useMemo<FieldConfig[]>(
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

  const dynamicItemFields = useMemo<FieldConfig[]>(
    () =>
      itemCustomFields.map((field) => ({
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
    [itemCustomFields],
  );

  const allOsFields = useMemo(() => [...osFields, ...dynamicOsFields], [dynamicOsFields]);
  const allItemFields = useMemo(() => [...itemFields, ...dynamicItemFields], [dynamicItemFields]);

  const customFieldByName = useMemo(
    () => new Map(customFields.map((field) => [field.chave, field])),
    [customFields],
  );

  const itemCustomFieldByName = useMemo(
    () => new Map(itemCustomFields.map((field) => [field.chave, field])),
    [itemCustomFields],
  );

  useEffect(() => {
    setOsForm((current) => ({ ...defaultForm(allOsFields), ...current }));
  }, [allOsFields]);

  useEffect(() => {
    setItemForm((current) => ({ ...defaultForm(allItemFields), ...current }));
  }, [allItemFields]);

  const osLayoutByField = useMemo(() => {
    const map = new Map<string, FieldLayout>();

    osLayoutList.data.forEach((item) => {
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
  }, [osLayoutList.data]);

  const itemLayoutByField = useMemo(() => {
    const map = new Map<string, FieldLayout>();

    itemLayoutList.data.forEach((item) => {
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
  }, [itemLayoutList.data]);

  const orderedOsCreateFields = useMemo(
    () =>
      allOsFields
        .map((field, index) => ({
          field,
          layout:
            osLayoutByField.get(field.name) ??
            osLayoutByField.get(field.name.toLowerCase()) ??
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
    [allOsFields, customFieldByName, osLayoutByField],
  );

  const orderedItemFields = useMemo(
    () =>
      allItemFields
        .map((field, index) => ({
          field,
          layout:
            itemLayoutByField.get(field.name) ??
            itemLayoutByField.get(field.name.toLowerCase()) ??
            defaultLayout(
              field.name,
              index,
              normalizeTabName(itemCustomFieldByName.get(field.name)?.aba),
            ),
        }))
        .sort(
          (a, b) =>
            a.layout.ordem - b.layout.ordem ||
            a.layout.aba.localeCompare(b.layout.aba) ||
            a.layout.linha - b.layout.linha ||
            a.layout.posicao - b.layout.posicao,
        ),
    [allItemFields, itemCustomFieldByName, itemLayoutByField],
  );

  const osTabs = useMemo(() => {
    const order = new Map<string, number>();
    const explicitTabs = new Set<string>();
    osLayoutList.data.forEach((item) => {
      if (!isTabMarker(item.campoChave)) return;
      const tab = normalizeTabName(item.aba);
      explicitTabs.add(tab);
      order.set(tab, Number(item.ordem ?? Number.MAX_SAFE_INTEGER));
    });
    if (!order.has("Principal")) order.set("Principal", Number.MAX_SAFE_INTEGER - 2);
    if (!order.has(osActiveTab)) order.set(osActiveTab, Number.MAX_SAFE_INTEGER - 1);
    orderedOsCreateFields.forEach((item) => {
      const tab = normalizeTabName(item.layout.aba);
      if (explicitTabs.has(tab)) return;
      order.set(tab, Math.min(order.get(tab) ?? Number.MAX_SAFE_INTEGER, item.layout.ordem));
    });
    customFields.forEach((field) => {
      const tab = normalizeTabName(field.aba);
      if (explicitTabs.has(tab)) return;
      order.set(tab, Math.min(order.get(tab) ?? Number.MAX_SAFE_INTEGER, field.ordem));
    });
    return [...order.entries()].sort((a, b) => a[1] - b[1] || a[0].localeCompare(b[0])).map(([tab]) => tab);
  }, [customFields, orderedOsCreateFields, osActiveTab, osLayoutList.data]);

  const osTabOptions = useMemo(
    () => osTabs.map((tab) => ({ value: tab, label: tab })),
    [osTabs],
  );

  const itemTabs = useMemo(() => {
    const order = new Map<string, number>();
    const explicitTabs = new Set<string>();
    itemLayoutList.data.forEach((item) => {
      if (!isTabMarker(item.campoChave)) return;
      const tab = normalizeTabName(item.aba);
      explicitTabs.add(tab);
      order.set(tab, Number(item.ordem ?? Number.MAX_SAFE_INTEGER));
    });
    if (!order.has("Principal")) order.set("Principal", Number.MAX_SAFE_INTEGER - 2);
    if (!order.has(itemActiveTab)) order.set(itemActiveTab, Number.MAX_SAFE_INTEGER - 1);
    orderedItemFields.forEach((item) => {
      const tab = normalizeTabName(item.layout.aba);
      if (explicitTabs.has(tab)) return;
      order.set(tab, Math.min(order.get(tab) ?? Number.MAX_SAFE_INTEGER, item.layout.ordem));
    });
    itemCustomFields.forEach((field) => {
      const tab = normalizeTabName(field.aba);
      if (explicitTabs.has(tab)) return;
      order.set(tab, Math.min(order.get(tab) ?? Number.MAX_SAFE_INTEGER, field.ordem));
    });
    return [...order.entries()].sort((a, b) => a[1] - b[1] || a[0].localeCompare(b[0])).map(([tab]) => tab);
  }, [itemActiveTab, itemCustomFields, itemLayoutList.data, orderedItemFields]);

  const itemTabOptions = useMemo(
    () => itemTabs.map((tab) => ({ value: tab, label: tab })),
    [itemTabs],
  );

  const visibleOrderedOsCreateFields = useMemo(
    () => orderedOsCreateFields.filter((item) => normalizeTabName(item.layout.aba) === osActiveTab),
    [orderedOsCreateFields, osActiveTab],
  );

  const visibleOrderedItemFields = useMemo(
    () => orderedItemFields.filter((item) => normalizeTabName(item.layout.aba) === itemActiveTab),
    [itemActiveTab, orderedItemFields],
  );

  const itemCustomValuesByOrigin = useMemo(() => {
    const map = new Map<string, ApiRecord>();

    itemCustomRecords.data.forEach((record) => {
      const originId = String(record.origemId ?? "");
      if (!originId) return;
      map.set(originId, (record.valores as ApiRecord | undefined) ?? {});
    });

    return map;
  }, [itemCustomRecords.data]);

  const itemRows = useMemo(
    () =>
      itens.data.map((item) => ({
        ...item,
        ...(itemCustomValuesByOrigin.get(String(item.id ?? "")) ?? {}),
      })),
    [itemCustomValuesByOrigin, itens.data],
  );

  const selectedOs = useMemo(() => {
    const row = ordens.data.find((ordem) => String(ordem.id ?? "") === selectedOsId);
    if (!row) return row;

    const custom = customRecords.data.find(
      (record) => String(record.origemId ?? "") === selectedOsId,
    );

    return {
      ...row,
      ...((custom?.valores as ApiRecord | undefined) ?? {}),
    };
  }, [customRecords.data, ordens.data, selectedOsId]);

  const selectedOsWithPhotos = useMemo(
    () => (selectedOs ? { ...selectedOs, fotos: osPhotos } : null),
    [osPhotos, selectedOs],
  );

  const ordensAbertas = ordens.data.filter((item) =>
    ["ABERTA", "APROVADA", "EM_EXECUCAO", "PRONTA"].includes(String(item.status ?? "")),
  ).length;

  const ordensEmAndamento = ordens.data.filter((item) =>
    ["APROVADA", "EM_EXECUCAO"].includes(String(item.status ?? "")),
  ).length;

  const totalReceita = ordens.data.reduce(
    (total, row) => total + Number(row.valorTotal ?? 0),
    0,
  );

  useEffect(() => {
    let active = true;

    async function ensureModule() {
      try {
        const module = await apiRequest<CustomModule>("/modulos-personalizados/sistema", {
          method: "POST",
          body: {
            chave: "ordem_servico",
            nome: "Ordem de serviço",
            descricao: "Campos extras da ordem de serviço",
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
  }, [customReloadKey]);

  useEffect(() => {
    let active = true;

    async function ensureItemModule() {
      try {
        const module = await apiRequest<CustomModule>("/modulos-personalizados/sistema", {
          method: "POST",
          body: {
            chave: "ordem_servico_itens",
            nome: "Itens da ordem de serviÃ§o",
            descricao: "Campos extras dos serviÃ§os e peÃ§as da ordem de serviÃ§o",
          },
        });

        if (active) setItemCustomModule(module);
      } catch (err) {
        if (active) setFailure(errorMessage(err));
      }
    }

    void ensureItemModule();

    return () => {
      active = false;
    };
  }, [itemCustomReloadKey]);

  useEffect(() => {
    if (selectedOs?.status) {
      setStatus(String(selectedOs.status));
    }
  }, [selectedOs?.status]);

  useEffect(() => {
    let active = true;

    async function loadPhotos() {
      if (!selectedOsId) {
        setOsPhotos([]);
        return;
      }

      try {
        const result = await apiRequest<OrdemServicoFoto[]>(`/ordens-servico/${selectedOsId}/fotos`);
        if (active) setOsPhotos(result);
      } catch {
        if (active) setOsPhotos(osFotos(selectedOs));
      }
    }

    void loadPhotos();

    return () => {
      active = false;
    };
  }, [selectedOsId, selectedOs]);

  useEffect(() => {
    if (!osTabs.includes(osActiveTab)) {
      setOsActiveTab(osTabs[0] ?? "Principal");
    }
  }, [osActiveTab, osTabs]);

  useEffect(() => {
    if (!itemTabs.includes(itemActiveTab)) {
      setItemActiveTab(itemTabs[0] ?? "Principal");
    }
  }, [itemActiveTab, itemTabs]);

  useEffect(() => {
    setOsForm((current) => {
      const currentAparelhoId = String(current.aparelhoId ?? "").trim();
      if (!currentAparelhoId) return current;

      const aparelhoAindaValido = (aparelhosBase.data ?? []).some(
        (item: any) =>
          String(item.id ?? "") === currentAparelhoId &&
          String(item.clienteId ?? "") === clienteSelecionadoId,
      );

      if (aparelhoAindaValido) return current;
      return { ...current, aparelhoId: "" };
    });
  }, [clienteSelecionadoId, aparelhosBase.data]);

  async function submitQuickCliente(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");
    setNotice("");

    if (!quickClienteForm.nome.trim()) {
      setFailure("Informe o nome do cliente.");
      return;
    }

    try {
      const novoCliente = await apiRequest<any>("/clientes", {
        method: "POST",
        body: {
          nome: quickClienteForm.nome.trim(),
          telefone: quickClienteForm.telefone.trim() || null,
          email: quickClienteForm.email.trim() || null,
          tipoPessoa: "FISICA",
        },
      });

      setOsOptionsReloadKey((key) => key + 1);
      setOsForm((current) => ({
        ...current,
        clienteId: String(novoCliente.id ?? ""),
        aparelhoId: "",
      }));
      resetQuickClienteModal();
      setNotice("Cliente cadastrado com sucesso.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function submitQuickAparelho(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");
    setNotice("");

    if (!clienteSelecionadoId) {
      setFailure("Selecione ou cadastre um cliente antes de criar o aparelho.");
      return;
    }

    if (!quickAparelhoForm.marca.trim() || !quickAparelhoForm.modelo.trim()) {
      setFailure("Informe a marca e o modelo do aparelho.");
      return;
    }

    try {
      const novoAparelho = await apiRequest<any>("/aparelhos", {
        method: "POST",
        body: {
          clienteId: clienteSelecionadoId,
          marca: quickAparelhoForm.marca.trim(),
          modelo: quickAparelhoForm.modelo.trim(),
          cor: quickAparelhoForm.cor.trim() || null,
          imei: quickAparelhoForm.imei.trim() || null,
          serialNumber: quickAparelhoForm.serialNumber.trim() || null,
        },
      });

      setOsOptionsReloadKey((key) => key + 1);
      setOsForm((current) => ({
        ...current,
        aparelhoId: String(novoAparelho.id ?? ""),
      }));
      resetQuickAparelhoModal();
      setNotice("Aparelho cadastrado e selecionado com sucesso.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function consultarQuickImei() {
    const imei = String(quickAparelhoForm.imei ?? "").replace(/\D/g, "");
    setQuickImeiMessage("");

    if (imei.length !== 15) {
      setQuickImeiMessage("Informe um IMEI com 15 digitos.");
      return;
    }

    setQuickImeiLoading(true);

    try {
      const result = await apiRequest<ApiRecord>(`/aparelhos/imei/${imei}`);

      const marca = String(
        result.marca ??
        result.brand ??
        result.Brand ??
        ""
      ).trim();

      const nomeComercial = String(
        result.nomeComercial ??
        result.modelName ??
        result["Model Name"] ??
        ""
      ).trim();

      const modelo = String(
        result.modelo ??
        result.model ??
        result.Model ??
        ""
      ).trim();

      const cor = String(
        result.cor ??
        result.color ??
        result.Color ??
        ""
      ).trim();

      setQuickAparelhoForm((current) => ({
        ...current,
        marca: marca ? (marca === "Apple Inc" ? "Apple" : marca) : current.marca,
        modelo: nomeComercial
          ? nomeComercial
          : modelo
            ? modelo
            : current.modelo,
        cor: cor || current.cor,
      }));
      setQuickImeiMessage(String(result.mensagem ?? "Consulta finalizada."));
    } catch (error) {
      setQuickImeiMessage(error instanceof Error ? error.message : "Nao foi possivel consultar o IMEI.");
    } finally {
      setQuickImeiLoading(false);
    }
  }

  async function submitQuickTecnico(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");
    setNotice("");

    if (!quickTecnicoForm.nome.trim()) {
      setFailure("Informe o nome do técnico.");
      return;
    }

    try {
      const novoTecnico = await apiRequest<any>("/tecnicos", {
        method: "POST",
        body: {
          nome: quickTecnicoForm.nome.trim(),
          telefone: quickTecnicoForm.telefone.trim() || null,
          email: quickTecnicoForm.email.trim() || null,
          especialidade: quickTecnicoForm.especialidade.trim() || null,
        },
      });

      setOsOptionsReloadKey((key) => key + 1);
      setOsForm((current) => ({
        ...current,
        tecnicoId: String(novoTecnico.id ?? ""),
      }));
      resetQuickTecnicoModal();
      setNotice("Técnico cadastrado com sucesso.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function resetQuickClienteModal() {
    setQuickClienteOpen(false);
    setQuickClienteForm({ nome: "", telefone: "", email: "" });
  }

  function resetQuickAparelhoModal() {
    setQuickAparelhoOpen(false);
    setQuickAparelhoForm({ marca: "", modelo: "", cor: "", imei: "", serialNumber: "" });
    setQuickImeiMessage("");
    setQuickImeiLoading(false);
  }

  function resetQuickTecnicoModal() {
    setQuickTecnicoOpen(false);
    setQuickTecnicoForm({ nome: "", telefone: "", email: "", especialidade: "" });
  }

  function resetCreateFlow() {
    setCreateOpen(false);
    setCreateStep("dados");
    setCreatedOs(null);
    setPhotoDescription("");
    setPhotoUrl("");
    setPhotoLoading(false);
    setOsForm(defaultForm(allOsFields));
    setItemForm(defaultForm(allItemFields));
    setEditingItemId("");
    setOsErrors({});
    setItemErrors({});
    setShowCustomBuilder(false);
    setEditingCustomFieldId("");
    setCustomFieldForm(defaultForm(customFieldFormFields));
    setCustomFieldErrors({});
    setOsLayoutMode(false);
    setOsActiveTab("Principal");
    setOsNewTabName("");
    setShowOsCreateTabForm(false);
    setEditingOsTabName("");
    setItemLayoutMode(false);
    setDraggingItemFieldName("");
    setShowItemCustomBuilder(false);
    setEditingItemCustomFieldId("");
    setItemCustomFieldForm(defaultForm(customFieldFormFields));
    setItemCustomFieldErrors({});
    setItemActiveTab("Principal");
    setItemNewTabName("");
    setShowItemCreateTabForm(false);
    setEditingItemTabName("");
    resetQuickClienteModal();
    resetQuickAparelhoModal();
    resetQuickTecnicoModal();
  }

  function updateOs(name: string, value: unknown) {
    if (name === "aparelhoId" && !clienteSelecionado) {
      setFailure("Selecione ou cadastre um cliente antes de escolher um aparelho.");
      return;
    }

    setOsForm((current) => ({ ...current, [name]: value }));
    setOsErrors((current) => {
      const next = { ...current };
      delete next[name];
      return next;
    });
  }

  function updateItem(name: string, value: unknown) {
    setItemForm((current) => {
      if (name !== "tipoItem") return { ...current, [name]: value };

      return {
        ...current,
        tipoItem: value,
        servicoCatalogoId: "",
        pecaId: "",
        descricao: "",
        valorUnitario: "",
      };
    });

    setItemErrors((current) => {
      const next = { ...current };
      delete next[name];
      if (name === "tipoItem") {
        delete next.servicoCatalogoId;
        delete next.pecaId;
        delete next.descricao;
        delete next.valorUnitario;
      }
      return next;
    });
  }

  function validateItemForm() {
    const errors = validateForm(allItemFields, itemForm);
    const tipoItem = String(itemForm.tipoItem ?? "").toUpperCase();
    const descricao = String(itemForm.descricao ?? "").trim();
    const servicoId = String(itemForm.servicoCatalogoId ?? "").trim();
    const pecaId = String(itemForm.pecaId ?? "").trim();

    if (tipoItem === "SERVICO" && !servicoId && !descricao) {
      errors.servicoCatalogoId = "Escolha um serviço ou informe uma descrição manual.";
      errors.descricao = "Informe uma descrição manual se não escolher um serviço.";
    }

    if (tipoItem === "PECA" && !pecaId) {
      errors.pecaId = "Escolha a peça que será adicionada.";
    }

    return errors;
  }

  function tabWithFirstError(
    errors: Record<string, string>,
    layouts: Array<{ field: FieldConfig; layout: FieldLayout }>,
  ) {
    const firstErrorName = Object.keys(errors)[0];
    if (!firstErrorName) return "";

    return normalizeTabName(
      layouts.find((item) => item.field.name === firstErrorName)?.layout.aba,
    );
  }

  async function submitOs(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");
    setNotice("");

    const errors = validateForm(allOsFields, osForm);
    if (Object.keys(errors).length > 0) {
      setOsErrors(errors);
      const tab = tabWithFirstError(errors, orderedOsCreateFields);
      if (tab) setOsActiveTab(tab);
      setFailure("Corrija os campos da OS antes de salvar.");
      return;
    }

    try {
      const saved = await apiRequest<ApiRecord>("/ordens-servico", {
        method: "POST",
        body: payloadFromForm(osFields, osForm),
      });

      const originId = String(saved.id ?? "");
      if (customModule && originId && dynamicOsFields.length > 0) {
        await apiRequest(`/modulos-personalizados/${customModule.id}/registros/origem/${originId}`, {
          method: "PUT",
          body: { valores: payloadFromForm(dynamicOsFields, osForm) },
        });
      }

      setOsErrors({});
      setCreatedOs(saved);
      setSelectedOsId(originId);
      setStatus(String(saved.status ?? "APROVADA"));
      setItemsReloadKey((key) => key + 1);
      setCustomReloadKey((key) => key + 1);
      setCreateStep("fotos");
      setReloadKey((key) => key + 1);
      setNotice("OS criada. Agora voce pode adicionar fotos e itens.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function submitItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedOsId) return;

    setFailure("");
    setNotice("");

    const errors = validateItemForm();
    if (Object.keys(errors).length > 0) {
      setItemErrors(errors);
      const tab = tabWithFirstError(errors, orderedItemFields);
      if (tab) setItemActiveTab(tab);
      setFailure("Corrija os campos do item antes de salvar.");
      return;
    }

    try {
      const saved = await apiRequest<ApiRecord>(
        editingItemId
          ? `/ordens-servico/${selectedOsId}/itens/${editingItemId}`
          : `/ordens-servico/${selectedOsId}/itens`,
        {
          method: editingItemId ? "PUT" : "POST",
          body: payloadFromForm(itemFields, itemForm),
        },
      );

      const originId = editingItemId || String(saved.id ?? "");
      if (itemCustomModule && originId && dynamicItemFields.length > 0) {
        await apiRequest(`/modulos-personalizados/${itemCustomModule.id}/registros/origem/${originId}`, {
          method: "PUT",
          body: { valores: payloadFromForm(dynamicItemFields, itemForm) },
        });
      }

      setItemForm(defaultForm(allItemFields));
      setEditingItemId("");
      setItemErrors({});
      setItemsReloadKey((key) => key + 1);
      setItemCustomReloadKey((key) => key + 1);
      setReloadKey((key) => key + 1);
      setNotice(editingItemId ? "Item atualizado." : "Item adicionado.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function alterarStatus() {
    if (!selectedOsId) return;

    setFailure("");
    setNotice("");

    try {
      await apiRequest(`/ordens-servico/${selectedOsId}/status`, {
        method: "PATCH",
        body: { status },
      });
      setReloadKey((key) => key + 1);
      setNotice("Status atualizado.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function cancelar(row: ApiRecord) {
    setFailure("");
    setNotice("");

    try {
      await apiRequest(`/ordens-servico/${row.id}/cancelar`, { method: "PATCH" });
      setReloadKey((key) => key + 1);
      setNotice("OS cancelada.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function uploadOsPhoto(file?: File | null) {
    if (!selectedOsId || !file) return;

    setFailure("");
    setNotice("");
    setPhotoLoading(true);

    try {
      const formData = new FormData();
      formData.append("arquivo", file);
      if (photoDescription.trim()) formData.append("descricao", photoDescription.trim());

      const fotos = await apiUpload<OrdemServicoFoto[]>(
        `/ordens-servico/${selectedOsId}/fotos`,
        formData,
      );
      setOsPhotos(fotos);
      setPhotoDescription("");
      setPhotoUrl("");
      setReloadKey((key) => key + 1);
      setNotice("Foto adicionada na OS.");
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setPhotoLoading(false);
    }
  }

  async function uploadOsPhotoByUrl() {
    if (!selectedOsId) return;

    const normalizedUrl = photoUrl.trim();
    if (!normalizedUrl) {
      setFailure("Informe a URL da imagem.");
      return;
    }

    setFailure("");
    setNotice("");
    setPhotoLoading(true);

    try {
      const formData = new FormData();
      formData.append("url", normalizedUrl);
      if (photoDescription.trim()) formData.append("descricao", photoDescription.trim());

      const fotos = await apiUpload<OrdemServicoFoto[]>(
        `/ordens-servico/${selectedOsId}/fotos`,
        formData,
      );
      setOsPhotos(fotos);
      setPhotoDescription("");
      setPhotoUrl("");
      setReloadKey((key) => key + 1);
      setNotice("Foto adicionada na OS.");
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setPhotoLoading(false);
    }
  }

  async function deleteOsPhoto(fotoId: string) {
    if (!selectedOsId || !fotoId) return;
    if (!window.confirm("Excluir esta foto da OS?")) return;

    setFailure("");
    setNotice("");
    setPhotoLoading(true);

    try {
      await apiRequest(`/ordens-servico/${selectedOsId}/fotos/${fotoId}`, { method: "DELETE" });
      setOsPhotos((current) => current.filter((foto) => foto.id !== fotoId));
      setReloadKey((key) => key + 1);
      setNotice("Foto excluida da OS.");
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setPhotoLoading(false);
    }
  }

  async function saveCustomField(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!customModule || !canManageCustomFields) return;

    const validation = validateForm(customFieldFormFields, customFieldForm);
    if (Object.keys(validation).length > 0) {
      setCustomFieldErrors(validation);
      setFailure("Corrija os dados do campo extra.");
      return;
    }

    const payload = payloadFromForm(customFieldFormFields, customFieldForm);
    const nextIndex = allOsFields.length + 1;
    const fieldTab = resolveFieldTab(payload.aba);
    const tabIndex = orderedOsCreateFields.filter(
      (item) => normalizeTabName(item.layout.aba) === fieldTab,
    ).length;
    const layout = {
      ...defaultLayout(String(payload.nome ?? `campo_${nextIndex}`), tabIndex, fieldTab),
      ordem: orderForTab(Math.max(0, osTabs.indexOf(fieldTab)), tabIndex),
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
        setNotice("Campo extra atualizado.");
      } else {
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos`, {
          method: "POST",
          body,
        });
        setNotice("Campo extra criado.");
      }

      setEditingCustomFieldId("");
      setCustomFieldForm(defaultForm(customFieldFormFields));
      setCustomFieldErrors({});
      setShowCustomBuilder(false);
      setCustomReloadKey((key) => key + 1);
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function editOsCustomField(field: CustomField) {
    if (!canManageCustomFields) return;

    setEditingCustomFieldId(field.id);
    setShowCustomBuilder(true);
    setCustomFieldForm({
      nome: field.nome,
      aba: normalizeTabName(field.aba),
      tipo: field.tipo,
      obrigatorio: field.obrigatorio,
      exportarExcel: field.exportarExcel !== false,
      exportarExcelResumo: field.exportarExcelResumo === true,
      exportarPdf: field.exportarPdf !== false,
      opcoesText: (field.opcoes ?? []).join("\n"),
    });
  }

  async function deleteOsCustomField() {
    if (!customModule || !editingCustomFieldId || !canManageCustomFields) return;
    if (!window.confirm("Excluir este campo extra?")) return;

    try {
      await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingCustomFieldId}`, {
        method: "DELETE",
      });
      setEditingCustomFieldId("");
      setCustomFieldForm(defaultForm(customFieldFormFields));
      setShowCustomBuilder(false);
      setCustomReloadKey((key) => key + 1);
      setNotice("Campo extra excluído.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function moveOsField(targetName: string) {
    if (!customModule || !draggingOsFieldName || draggingOsFieldName === targetName) return;

    const names = visibleOrderedOsCreateFields.map((item) => item.field.name);
    const from = names.indexOf(draggingOsFieldName);
    const to = names.indexOf(targetName);
    if (from < 0 || to < 0) return;

    const next = [...names];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);

    const overrides = new Map<string, string>();
    next.forEach((name) => overrides.set(name, osActiveTab));

    await saveOsTabOrder(osTabs, "Layout dos campos da OS atualizado.", overrides, new Map([[osActiveTab, next]]));
    setDraggingOsFieldName("");
  }

  async function createOsTab() {
    const rawName = String(osNewTabName ?? "").trim();
    if (!rawName) {
      setFailure("Informe um nome para a nova aba da OS.");
      return;
    }

    const name = normalizeTabName(rawName);
    if (osTabs.includes(name)) {
      setOsActiveTab(name);
      setOsNewTabName("");
      setShowOsCreateTabForm(false);
      setFailure("Já existe uma aba com esse nome.");
      return;
    }

    const nextTabs = [...osTabs, name];

    setOsActiveTab(name);
    if (!editingCustomFieldId) {
      setCustomFieldForm((current) => ({ ...current, aba: name }));
    }
    setOsNewTabName("");

    await saveOsTabOrder(nextTabs, "Aba da OS criada.");
    setShowOsCreateTabForm(false);
  }

  function openEditOsTab(tab: string) {
    if (tab === "Principal") return;
    setEditingOsTabName(tab);
    setOsNewTabName(tab);
    setShowOsCreateTabForm(true);
  }

  async function saveOsTabName() {
    if (!editingOsTabName) {
      await createOsTab();
      return;
    }

    const rawName = String(osNewTabName ?? "").trim();
    if (!rawName) {
      setFailure("Informe um nome para a aba da OS.");
      return;
    }

    const name = normalizeTabName(rawName);
    if (name !== editingOsTabName && osTabs.includes(name)) {
      setFailure("Ja existe uma aba com esse nome.");
      return;
    }

    const nextTabs = osTabs.map((tab) => (tab === editingOsTabName ? name : tab));
    const fieldOverrides = new Map<string, string>();
    orderedOsCreateFields
      .filter((item) => normalizeTabName(item.layout.aba) === editingOsTabName)
      .forEach((item) => fieldOverrides.set(item.field.name, name));

    await saveOsTabOrder(nextTabs, "Aba da OS atualizada.", fieldOverrides);
    setOsActiveTab(name);
    setCustomFieldForm((current) => ({ ...current, aba: name }));
    setEditingOsTabName("");
    setOsNewTabName("");
    setShowOsCreateTabForm(false);
  }

  async function moveOsTab(targetTab: string) {
    if (!customModule || !draggingOsTabName || draggingOsTabName === targetTab) return;

    const from = osTabs.indexOf(draggingOsTabName);
    const to = osTabs.indexOf(targetTab);
    if (from < 0 || to < 0) return;

    const nextTabs = [...osTabs];
    const [moved] = nextTabs.splice(from, 1);
    nextTabs.splice(to, 0, moved);

    await saveOsTabOrder(nextTabs, "Ordem das abas da OS atualizada.");
    setDraggingOsTabName("");
  }

  async function saveOsTabOrder(
    nextTabs: string[],
    successMessage: string,
    fieldTabOverrides?: Map<string, string>,
    fieldOrderOverrides?: Map<string, string[]>,
  ) {
    if (!customModule) return;

    const tabIndexes = new Map(nextTabs.map((tab, index) => [tab, index]));
    const counters = new Map<string, number>();
    const handledFields = new Set<string>();
    const body: ApiRecord[] = nextTabs.map((tab, index) => tabMarkerLayout(tab, index) as ApiRecord);

    nextTabs.forEach((tab) => {
      const orderedNames = fieldOrderOverrides?.get(tab) ?? orderedOsCreateFields
        .filter((item) => normalizeTabName(fieldTabOverrides?.get(item.field.name) ?? item.layout.aba) === tab)
        .map((item) => item.field.name);

      orderedNames.forEach((fieldName) => {
        const fieldIndex = counters.get(tab) ?? 0;
        counters.set(tab, fieldIndex + 1);
        handledFields.add(fieldName);

        body.push({
          campoChave: fieldName,
          aba: tab,
          linha: Math.floor(fieldIndex / 3) + 1,
          posicao: (fieldIndex % 3) + 1,
          ordem: orderForTab(tabIndexes.get(tab) ?? 0, fieldIndex),
        });
      });
    });

    orderedOsCreateFields.forEach((item) => {
      if (handledFields.has(item.field.name)) return;
      const tab = normalizeTabName(fieldTabOverrides?.get(item.field.name) ?? item.layout.aba);
      const fieldIndex = counters.get(tab) ?? 0;
      counters.set(tab, fieldIndex + 1);

      body.push({
        campoChave: item.layout.campoChave,
        aba: tab,
        linha: Math.floor(fieldIndex / 3) + 1,
        posicao: (fieldIndex % 3) + 1,
        ordem: orderForTab(tabIndexes.get(tab) ?? 0, fieldIndex),
      });
    });

    osLayoutList.setData(body as ApiRecord[]);

    try {
      await apiRequest(`/modulos-personalizados/${customModule.id}/layout`, {
        method: "PATCH",
        body,
      });

      setCustomReloadKey((key) => key + 1);
      setNotice(successMessage);
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function deleteOsTab(tab: string) {
    if (!customModule || !canManageCustomFields || tab === "Principal") return;
    const fieldsToMove = orderedOsCreateFields.filter(
      (item) => normalizeTabName(item.layout.aba) === tab,
    );
    const message =
      fieldsToMove.length > 0
        ? `Excluir a aba "${tab}"? Os campos dela serao movidos para a aba Principal.`
        : `Excluir a aba "${tab}"?`;

    if (!window.confirm(message)) return;

    const nextTabs = osTabs.filter((item) => item !== tab);
    const overrides = new Map<string, string>();
    fieldsToMove.forEach((item) => overrides.set(item.field.name, "Principal"));

    await saveOsTabOrder(nextTabs, "Aba da OS excluida.", overrides);
    setShowOsCreateTabForm(false);
    if (osActiveTab === tab) {
      setOsActiveTab("Principal");
    }
  }

  async function saveItemCustomField(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!itemCustomModule || !canManageCustomFields) return;

    const validation = validateForm(customFieldFormFields, itemCustomFieldForm);
    if (Object.keys(validation).length > 0) {
      setItemCustomFieldErrors(validation);
      setFailure("Corrija os dados do campo extra do item.");
      return;
    }

    const payload = payloadFromForm(customFieldFormFields, itemCustomFieldForm);
    const nextIndex = allItemFields.length + 1;
    const fieldTab = resolveFieldTab(payload.aba);
    const tabIndex = orderedItemFields.filter(
      (item) => normalizeTabName(item.layout.aba) === fieldTab,
    ).length;
    const layout = {
      ...defaultLayout(String(payload.nome ?? `campo_item_${nextIndex}`), tabIndex, fieldTab),
      ordem: orderForTab(Math.max(0, itemTabs.indexOf(fieldTab)), tabIndex),
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
      opcoes: String(itemCustomFieldForm.opcoesText ?? "")
        .split(/\r?\n/)
        .map((option) => option.trim())
        .filter(Boolean),
      exportarExcel: payload.exportarExcel !== false,
      exportarExcelResumo: payload.exportarExcelResumo === true,
      exportarPdf: payload.exportarPdf !== false,
      ativo: true,
    };

    try {
      if (editingItemCustomFieldId) {
        const { tipo: _tipo, ...updateBody } = body;
        await apiRequest(
          `/modulos-personalizados/${itemCustomModule.id}/campos/${editingItemCustomFieldId}`,
          {
            method: "PUT",
            body: updateBody,
          },
        );
        setNotice("Campo extra do item atualizado.");
      } else {
        await apiRequest(`/modulos-personalizados/${itemCustomModule.id}/campos`, {
          method: "POST",
          body,
        });
        setNotice("Campo extra do item criado.");
      }

      setEditingItemCustomFieldId("");
      setItemCustomFieldForm(defaultForm(customFieldFormFields));
      setItemCustomFieldErrors({});
      setShowItemCustomBuilder(false);
      setItemCustomReloadKey((key) => key + 1);
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function editItemCustomField(field: CustomField) {
    if (!canManageCustomFields) return;

    setEditingItemCustomFieldId(field.id);
    setShowItemCustomBuilder(true);
    setItemCustomFieldForm({
      nome: field.nome,
      aba: normalizeTabName(field.aba),
      tipo: field.tipo,
      obrigatorio: field.obrigatorio,
      exportarExcel: field.exportarExcel !== false,
      exportarExcelResumo: field.exportarExcelResumo === true,
      exportarPdf: field.exportarPdf !== false,
      opcoesText: (field.opcoes ?? []).join("\n"),
    });
  }

  async function deleteItemCustomField() {
    if (!itemCustomModule || !editingItemCustomFieldId || !canManageCustomFields) return;
    if (!window.confirm("Excluir este campo extra do item?")) return;

    try {
      await apiRequest(
        `/modulos-personalizados/${itemCustomModule.id}/campos/${editingItemCustomFieldId}`,
        {
          method: "DELETE",
        },
      );
      setEditingItemCustomFieldId("");
      setItemCustomFieldForm(defaultForm(customFieldFormFields));
      setShowItemCustomBuilder(false);
      setItemCustomReloadKey((key) => key + 1);
      setNotice("Campo extra do item excluÃ­do.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function moveItemField(targetName: string) {
    if (!itemCustomModule || !draggingItemFieldName || draggingItemFieldName === targetName) return;

    const names = visibleOrderedItemFields.map((item) => item.field.name);
    const from = names.indexOf(draggingItemFieldName);
    const to = names.indexOf(targetName);
    if (from < 0 || to < 0) return;

    const next = [...names];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);

    const overrides = new Map<string, string>();
    next.forEach((name) => overrides.set(name, itemActiveTab));

    await saveItemTabOrder(itemTabs, "Layout dos itens da OS atualizado.", overrides, new Map([[itemActiveTab, next]]));
    setDraggingItemFieldName("");
  }

  async function createItemTab() {
    const rawName = String(itemNewTabName ?? "").trim();
    if (!rawName) {
      setFailure("Informe um nome para a nova aba dos itens.");
      return;
    }

    const name = normalizeTabName(rawName);
    if (itemTabs.includes(name)) {
      setItemActiveTab(name);
      setItemNewTabName("");
      setShowItemCreateTabForm(false);
      setFailure("Já existe uma aba com esse nome.");
      return;
    }

    const nextTabs = [...itemTabs, name];

    setItemActiveTab(name);
    if (!editingItemCustomFieldId) {
      setItemCustomFieldForm((current) => ({ ...current, aba: name }));
    }
    setItemNewTabName("");

    await saveItemTabOrder(nextTabs, "Aba dos itens criada.");
    setShowItemCreateTabForm(false);
  }

  function openEditItemTab(tab: string) {
    if (tab === "Principal") return;
    setEditingItemTabName(tab);
    setItemNewTabName(tab);
    setShowItemCreateTabForm(true);
  }

  async function saveItemTabName() {
    if (!editingItemTabName) {
      await createItemTab();
      return;
    }

    const rawName = String(itemNewTabName ?? "").trim();
    if (!rawName) {
      setFailure("Informe um nome para a aba dos itens.");
      return;
    }

    const name = normalizeTabName(rawName);
    if (name !== editingItemTabName && itemTabs.includes(name)) {
      setFailure("Ja existe uma aba com esse nome.");
      return;
    }

    const nextTabs = itemTabs.map((tab) => (tab === editingItemTabName ? name : tab));
    const fieldOverrides = new Map<string, string>();
    orderedItemFields
      .filter((item) => normalizeTabName(item.layout.aba) === editingItemTabName)
      .forEach((item) => fieldOverrides.set(item.field.name, name));

    await saveItemTabOrder(nextTabs, "Aba dos itens atualizada.", fieldOverrides);
    setItemActiveTab(name);
    setItemCustomFieldForm((current) => ({ ...current, aba: name }));
    setEditingItemTabName("");
    setItemNewTabName("");
    setShowItemCreateTabForm(false);
  }

  async function moveItemTab(targetTab: string) {
    if (!itemCustomModule || !draggingItemTabName || draggingItemTabName === targetTab) return;

    const from = itemTabs.indexOf(draggingItemTabName);
    const to = itemTabs.indexOf(targetTab);
    if (from < 0 || to < 0) return;

    const nextTabs = [...itemTabs];
    const [moved] = nextTabs.splice(from, 1);
    nextTabs.splice(to, 0, moved);

    await saveItemTabOrder(nextTabs, "Ordem das abas dos itens atualizada.");
    setDraggingItemTabName("");
  }

  async function saveItemTabOrder(
    nextTabs: string[],
    successMessage: string,
    fieldTabOverrides?: Map<string, string>,
    fieldOrderOverrides?: Map<string, string[]>,
  ) {
    if (!itemCustomModule) return;

    const tabIndexes = new Map(nextTabs.map((tab, index) => [tab, index]));
    const counters = new Map<string, number>();
    const handledFields = new Set<string>();
    const body: ApiRecord[] = nextTabs.map((tab, index) => tabMarkerLayout(tab, index) as ApiRecord);

    nextTabs.forEach((tab) => {
      const orderedNames = fieldOrderOverrides?.get(tab) ?? orderedItemFields
        .filter((item) => normalizeTabName(fieldTabOverrides?.get(item.field.name) ?? item.layout.aba) === tab)
        .map((item) => item.field.name);

      orderedNames.forEach((fieldName) => {
        const fieldIndex = counters.get(tab) ?? 0;
        counters.set(tab, fieldIndex + 1);
        handledFields.add(fieldName);

        body.push({
          campoChave: fieldName,
          aba: tab,
          linha: Math.floor(fieldIndex / 3) + 1,
          posicao: (fieldIndex % 3) + 1,
          ordem: orderForTab(tabIndexes.get(tab) ?? 0, fieldIndex),
        });
      });
    });

    orderedItemFields.forEach((item) => {
      if (handledFields.has(item.field.name)) return;
      const tab = normalizeTabName(fieldTabOverrides?.get(item.field.name) ?? item.layout.aba);
      const fieldIndex = counters.get(tab) ?? 0;
      counters.set(tab, fieldIndex + 1);

      body.push({
        campoChave: item.layout.campoChave,
        aba: tab,
        linha: Math.floor(fieldIndex / 3) + 1,
        posicao: (fieldIndex % 3) + 1,
        ordem: orderForTab(tabIndexes.get(tab) ?? 0, fieldIndex),
      });
    });

    itemLayoutList.setData(body as ApiRecord[]);

    try {
      await apiRequest(`/modulos-personalizados/${itemCustomModule.id}/layout`, {
        method: "PATCH",
        body,
      });

      setItemCustomReloadKey((key) => key + 1);
      setNotice(successMessage);
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function deleteItemTab(tab: string) {
    if (!itemCustomModule || !canManageCustomFields || tab === "Principal") return;
    const fieldsToMove = orderedItemFields.filter(
      (item) => normalizeTabName(item.layout.aba) === tab,
    );
    const message =
      fieldsToMove.length > 0
        ? `Excluir a aba "${tab}"? Os campos dela serao movidos para a aba Principal.`
        : `Excluir a aba "${tab}"?`;

    if (!window.confirm(message)) return;

    const nextTabs = itemTabs.filter((item) => item !== tab);
    const overrides = new Map<string, string>();
    fieldsToMove.forEach((item) => overrides.set(item.field.name, "Principal"));

    await saveItemTabOrder(nextTabs, "Aba dos itens excluida.", overrides);
    setShowItemCreateTabForm(false);
    if (itemActiveTab === tab) {
      setItemActiveTab("Principal");
    }
  }

  async function moveItem(targetId: string) {
    if (!selectedOsId || !draggingItemId || draggingItemId === targetId) return;

    const ids = itens.data.map((item) => String(item.id ?? ""));
    const from = ids.indexOf(draggingItemId);
    const to = ids.indexOf(targetId);
    if (from < 0 || to < 0) return;

    const next = [...ids];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);

    try {
      await apiRequest(`/ordens-servico/${selectedOsId}/itens/ordem`, {
        method: "PATCH",
        body: next.map((id, index) => ({ id, ordem: index + 1 })),
      });
      setDraggingItemId("");
      setItemsReloadKey((key) => key + 1);
      setNotice("Ordem dos itens atualizada.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function editItem(row: ApiRecord) {
    const customValues = itemCustomValuesByOrigin.get(String(row.id ?? "")) ?? {};

    setEditingItemId(String(row.id ?? ""));
    setItemForm({
      ...defaultForm(allItemFields),
      tipoItem: row.tipoItem ?? "SERVICO",
      servicoCatalogoId: row.servicoCatalogoId ?? "",
      pecaId: row.pecaId ?? "",
      descricao: row.descricao ?? "",
      quantidade: row.quantidade ?? 1,
      valorUnitario: row.valorUnitario ?? "",
      desconto: row.desconto ?? 0,
      ...customValues,
    });
    setItemErrors({});
    setCreateStep("itens");
    setCreateOpen(true);
    setCreatedOs(selectedOs ?? null);
  }

  const columns: ColumnConfig[] = [
    { key: "numeroOs", label: "OS" },
    { key: "clienteNome", label: "Cliente" },
    { key: "aparelhoDescricao", label: "Aparelho" },
    { key: "tecnicoNome", label: "Técnico" },
    {
      key: "status",
      label: "Status",
      render: (row) => (
        <span
          className={[
            "inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
            statusTone(String(row.status ?? "")),
          ].join(" ")}
        >
          {String(row.status ?? "-")}
        </span>
      ),
    },
    {
      key: "valorTotal",
      label: "Total",
      render: (row) => formatCurrency(row.valorTotal),
    },
    {
      key: "dataEntrada",
      label: "Entrada",
      render: (row) => formatDate(row.dataEntrada),
    },
  ];

  return (
    <PageFrame
      eyebrow="Operação"
      title="Ordens de serviço"
      description="Cadastre ordens com campos em grade livre, organize o layout e monte os itens no mesmo fluxo."
      actions={
        <div className="flex flex-wrap gap-2">
          <button className={buttonClass()} type="button" onClick={() => setReloadKey((k) => k + 1)}>
            <RefreshCw size={16} />
            Atualizar
          </button>

          <button
            type="button"
            className={buttonClass("primary")}
            onClick={() => {
              setFailure("");
              setNotice("");
              setCreateStep("dados");
              setCreatedOs(null);
              setSelectedOsId("");
              setPhotoDescription("");
              setPhotoUrl("");
              setOsForm(defaultForm(allOsFields));
              setItemForm(defaultForm(allItemFields));
              setOsErrors({});
              setItemErrors({});
              setCreateOpen(true);
            }}
          >
            <Plus size={16} />
            Nova OS
          </button>
        </div>
      }
    >
      <div className="space-y-6">
        {notice ? <Notice type="success">{notice}</Notice> : null}
        {failure || ordens.error || itens.error ? (
          <Notice type="error">{failure || ordens.error || itens.error}</Notice>
        ) : null}

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <InfoCard
            icon={<ClipboardList size={20} />}
            title="OS abertas"
            value={String(ordensAbertas)}
            helper="Aguardando conclusão"
          />
          <InfoCard
            icon={<Wrench size={20} />}
            title="Em andamento"
            value={String(ordensEmAndamento)}
            helper="Execução técnica"
          />
          <InfoCard
            icon={<Package size={20} />}
            title="Receita potencial"
            value={formatCurrency(totalReceita)}
            helper="Total acumulado"
          />
        </div>

        <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 border-b border-slate-100 pb-5">
            <h2 className="text-xl font-bold tracking-tight text-slate-900">Lista de ordens</h2>
            <p className="mt-1 text-sm text-slate-500">
              Selecione uma OS para ver resumo, alterar status e gerenciar itens.
            </p>
          </div>

          <DataTable
            columns={columns}
            rows={ordens.data}
            loading={ordens.loading}
            emptyText="Nenhuma OS aberta."
            actions={(row) => (
              <div className="flex flex-wrap gap-2">
                <button
                  className={buttonClass()}
                  type="button"
                  onClick={() => {
                    setSelectedOsId(String(row.id ?? ""));
                    setStatus(String(row.status ?? "APROVADA"));
                    setDetailTab("resumo");
                  }}
                >
                  Abrir
                </button>

                <button
                  className={buttonClass("danger")}
                  type="button"
                  onClick={() => void cancelar(row)}
                >
                  Cancelar
                </button>
              </div>
            )}
          />
        </section>

        {selectedOs ? (
          <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-5 flex flex-col gap-4 border-b border-slate-100 pb-5 lg:flex-row lg:items-start lg:justify-between">
              <div>
                <h2 className="text-xl font-bold tracking-tight text-slate-900">
                  OS {String(selectedOs.numeroOs ?? "-")}
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  {String(selectedOs.clienteNome ?? "-")} • {String(selectedOs.aparelhoDescricao ?? "-")}
                </p>
              </div>

              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  className={buttonClass("primary")}
                  onClick={() => {
                    setCreateOpen(true);
                    setCreateStep("itens");
                    setCreatedOs(selectedOs);
                  }}
                >
                  <Plus size={16} />
                  Adicionar item
                </button>

                <button
                  type="button"
                  className={buttonClass()}
                  onClick={() => exportOsPdfResumo(selectedOsWithPhotos ?? selectedOs, customFields)}
                >
                  <FileText size={16} />
                  PDF resumido
                </button>

                <button
                  type="button"
                  className={buttonClass()}
                  onClick={() => exportOsPdfCompleto(selectedOsWithPhotos ?? selectedOs, customFields, itemRows, itemCustomFields)}
                >
                  <FileText size={16} />
                  PDF completo
                </button>

                <button
                  type="button"
                  className={buttonClass()}
                  onClick={() => exportOsExcelCompleto(selectedOsWithPhotos ?? selectedOs, customFields, itemRows, itemCustomFields)}
                >
                  <FileSpreadsheet size={16} />
                  Excel
                </button>

                <button
                  type="button"
                  className={buttonClass()}
                  onClick={() => exportOsExcelResumo(selectedOsWithPhotos ?? selectedOs, customFields)}
                >
                  <FileSpreadsheet size={16} />
                  Excel resumido
                </button>

                <button
                  type="button"
                  className={buttonClass()}
                  onClick={() => sendOsWhatsAppCompleto(selectedOsWithPhotos ?? selectedOs, customFields, itemRows, itemCustomFields)}
                >
                  <MessageCircle size={16} />
                  WhatsApp
                </button>

                <button
                  type="button"
                  className={buttonClass()}
                  onClick={() => sendOsEmailCompleto(selectedOsWithPhotos ?? selectedOs, customFields, itemRows, itemCustomFields)}
                >
                  <Mail size={16} />
                  E-mail
                </button>
              </div>
            </div>

            <div className="mb-5 inline-flex rounded-2xl bg-slate-100 p-1">
              <button
                type="button"
                className={tabClass(detailTab === "resumo")}
                onClick={() => setDetailTab("resumo")}
              >
                Resumo
              </button>
              <button
                type="button"
                className={tabClass(detailTab === "itens")}
                onClick={() => setDetailTab("itens")}
              >
                Itens
              </button>
              <button
                type="button"
                className={tabClass(detailTab === "fotos")}
                onClick={() => setDetailTab("fotos")}
              >
                Fotos
              </button>
            </div>

            {detailTab === "resumo" ? (
              <div className="space-y-5">
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                  <InfoRow label="Cliente" value={String(selectedOs.clienteNome ?? "-")} />
                  <InfoRow label="Aparelho" value={String(selectedOs.aparelhoDescricao ?? "-")} />
                  <InfoRow label="Técnico" value={String(selectedOs.tecnicoNome ?? "-")} />
                  <InfoRow label="Entrada" value={formatDate(selectedOs.dataEntrada)} />
                  <InfoRow label="Previsão" value={formatDate(selectedOs.dataPrevisao)} />
                  <InfoRow label="Total" value={formatCurrency(selectedOs.valorTotal)} />
                  <InfoRow label="Defeito relatado" value={String(selectedOs.defeitoRelatado ?? "-")} />
                  <InfoRow label="Diagnóstico" value={String(selectedOs.diagnostico ?? "-")} />
                  <InfoRow label="Laudo técnico" value={String(selectedOs.laudoTecnico ?? "-")} />
                  <InfoRow label="Obs. cliente" value={String(selectedOs.observacoesCliente ?? "-")} />
                  <InfoRow label="Obs. internas" value={String(selectedOs.observacoesInternas ?? "-")} />
                  <InfoRow
                    label="Status"
                    value={
                      <span
                        className={[
                          "inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
                          statusTone(String(selectedOs.status ?? "")),
                        ].join(" ")}
                      >
                        {String(selectedOs.status ?? "-")}
                      </span>
                    }
                  />
                </div>

                <div className="rounded-3xl border border-slate-200 bg-slate-50 p-5">
                  <h3 className="text-sm font-semibold text-slate-900">Atualizar status</h3>

                  <div className="mt-4 flex flex-col gap-3 sm:flex-row">
                    <select
                      value={status}
                      onChange={(event) => setStatus(event.target.value)}
                      className="h-11 rounded-2xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-slate-400"
                    >
                      {statusOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>

                    <button
                      type="button"
                      className={buttonClass("primary")}
                      onClick={() => void alterarStatus()}
                    >
                      Atualizar status
                    </button>
                  </div>
                </div>
              </div>
            ) : detailTab === "itens" ? (
              <DataTable
                columns={[
                  { key: "tipoItem", label: "Tipo" },
                  { key: "descricao", label: "Descrição" },
                  { key: "quantidade", label: "Qtd." },
                  {
                    key: "valorUnitario",
                    label: "Unitário",
                    render: (row) => formatCurrency(row.valorUnitario),
                  },
                  {
                    key: "valorTotal",
                    label: "Total",
                    render: (row) => formatCurrency(row.valorTotal),
                  },
                ]}
                rows={itens.data}
                loading={itens.loading}
                emptyText="Nenhum item nesta OS."
                actions={(row) => (
                  <button
                    className={buttonClass()}
                    type="button"
                    onClick={() => editItem(row)}
                  >
                    Editar
                  </button>
                )}
              />
            ) : (
              <OsPhotoPanel
                title="Fotos do aparelho"
                helper="Registre o estado do celular na entrada, durante o reparo e na entrega."
                emptyText="Nenhuma foto adicionada nesta OS."
                uploadLabel="Adicionar foto"
                photos={osPhotos}
                description={photoDescription}
                imageUrl={photoUrl}
                photoLoading={photoLoading}
                onDescriptionChange={setPhotoDescription}
                onImageUrlChange={setPhotoUrl}
                onUpload={(file) => {
                  void uploadOsPhoto(file);
                }}
                onUploadFromUrl={() => {
                  void uploadOsPhotoByUrl();
                }}
                onDelete={(fotoId) => {
                  void deleteOsPhoto(fotoId);
                }}
              />
            )}
          </section>
        ) : null}

        {quickClienteOpen ? (
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
            <div className="w-full max-w-xl rounded-[28px] border border-slate-200 bg-white shadow-2xl">
              <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
                <div>
                  <h3 className="text-lg font-bold tracking-tight text-slate-900">Cadastro rápido de cliente</h3>
                  <p className="mt-1 text-sm text-slate-500">Cadastre só o básico para continuar a OS.</p>
                </div>
                <button type="button" className={buttonClass()} onClick={resetQuickClienteModal}>
                  <X size={16} />
                  Fechar
                </button>
              </div>
              <form onSubmit={submitQuickCliente} className="space-y-4 px-6 py-5">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="md:col-span-2">
                    <label className="mb-2 block text-sm font-medium text-slate-700">Nome</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickClienteForm.nome} onChange={(e) => setQuickClienteForm((c) => ({ ...c, nome: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">Telefone</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickClienteForm.telefone} onChange={(e) => setQuickClienteForm((c) => ({ ...c, telefone: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">E-mail</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickClienteForm.email} onChange={(e) => setQuickClienteForm((c) => ({ ...c, email: e.target.value }))} />
                  </div>
                </div>
                <div className="flex justify-end gap-3">
                  <button type="button" className={buttonClass()} onClick={resetQuickClienteModal}>Cancelar</button>
                  <button type="submit" className={buttonClass("primary")}>Salvar cliente</button>
                </div>
              </form>
            </div>
          </div>
        ) : null}

        {quickAparelhoOpen ? (
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
            <div className="w-full max-w-xl rounded-[28px] border border-slate-200 bg-white shadow-2xl">
              <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
                <div>
                  <h3 className="text-lg font-bold tracking-tight text-slate-900">Cadastro rápido de aparelho</h3>
                  <p className="mt-1 text-sm text-slate-500">Esse aparelho será vinculado ao cliente selecionado.</p>
                </div>
                <button type="button" className={buttonClass()} onClick={resetQuickAparelhoModal}>
                  <X size={16} />
                  Fechar
                </button>
              </div>
              <form onSubmit={submitQuickAparelho} className="space-y-4 px-6 py-5">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">Marca</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickAparelhoForm.marca} onChange={(e) => setQuickAparelhoForm((c) => ({ ...c, marca: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">Modelo</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickAparelhoForm.modelo} onChange={(e) => setQuickAparelhoForm((c) => ({ ...c, modelo: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">Cor</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickAparelhoForm.cor} onChange={(e) => setQuickAparelhoForm((c) => ({ ...c, cor: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">IMEI</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickAparelhoForm.imei} onChange={(e) => setQuickAparelhoForm((c) => ({ ...c, imei: e.target.value }))} />
                    <div className="mt-2 flex flex-col gap-2">
                      <button
                        type="button"
                        className="inline-flex w-fit items-center justify-center rounded-2xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                        disabled={quickImeiLoading}
                        onClick={() => void consultarQuickImei()}
                      >
                        {quickImeiLoading ? "Consultando..." : "Buscar pelo IMEI"}
                      </button>
                      {quickImeiMessage ? (
                        <small className="text-xs text-slate-500">{quickImeiMessage}</small>
                      ) : null}
                    </div>
                  </div>
                  <div className="md:col-span-2">
                    <label className="mb-2 block text-sm font-medium text-slate-700">Serial number</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickAparelhoForm.serialNumber} onChange={(e) => setQuickAparelhoForm((c) => ({ ...c, serialNumber: e.target.value }))} />
                  </div>
                </div>
                <div className="flex justify-end gap-3">
                  <button type="button" className={buttonClass()} onClick={resetQuickAparelhoModal}>Cancelar</button>
                  <button type="submit" className={buttonClass("primary")}>Salvar aparelho</button>
                </div>
              </form>
            </div>
          </div>
        ) : null}

        {quickTecnicoOpen ? (
          <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
            <div className="w-full max-w-xl rounded-[28px] border border-slate-200 bg-white shadow-2xl">
              <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
                <div>
                  <h3 className="text-lg font-bold tracking-tight text-slate-900">Cadastro rápido de técnico</h3>
                  <p className="mt-1 text-sm text-slate-500">Cadastre o técnico sem sair da OS.</p>
                </div>
                <button type="button" className={buttonClass()} onClick={resetQuickTecnicoModal}>
                  <X size={16} />
                  Fechar
                </button>
              </div>
              <form onSubmit={submitQuickTecnico} className="space-y-4 px-6 py-5">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="md:col-span-2">
                    <label className="mb-2 block text-sm font-medium text-slate-700">Nome</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickTecnicoForm.nome} onChange={(e) => setQuickTecnicoForm((c) => ({ ...c, nome: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">Telefone</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickTecnicoForm.telefone} onChange={(e) => setQuickTecnicoForm((c) => ({ ...c, telefone: e.target.value }))} />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-700">E-mail</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickTecnicoForm.email} onChange={(e) => setQuickTecnicoForm((c) => ({ ...c, email: e.target.value }))} />
                  </div>
                  <div className="md:col-span-2">
                    <label className="mb-2 block text-sm font-medium text-slate-700">Especialidade</label>
                    <input className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60" value={quickTecnicoForm.especialidade} onChange={(e) => setQuickTecnicoForm((c) => ({ ...c, especialidade: e.target.value }))} />
                  </div>
                </div>
                <div className="flex justify-end gap-3">
                  <button type="button" className={buttonClass()} onClick={resetQuickTecnicoModal}>Cancelar</button>
                  <button type="submit" className={buttonClass("primary")}>Salvar técnico</button>
                </div>
              </form>
            </div>
          </div>
        ) : null}

        {createOpen ? (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
            <div className="max-h-[90vh] w-full max-w-6xl overflow-hidden rounded-[28px] border border-slate-200 bg-white shadow-2xl">
              <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
                <div>
                  <h2 className="text-xl font-bold tracking-tight text-slate-900">
                    Nova ordem de serviço
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Organize os campos do seu jeito, em linhas e colunas.
                  </p>
                </div>

                <button
                  type="button"
                  className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
                  onClick={resetCreateFlow}
                  aria-label="Fechar"
                >
                  <X size={18} />
                </button>
              </div>

              <div className="max-h-[calc(90vh-88px)] overflow-y-auto px-6 py-5">
                <div className="mb-5 flex flex-wrap items-center gap-2 rounded-2xl bg-slate-100 p-1">
                  <span className={tabClass(createStep === "dados")}>1. Dados da OS</span>
                  <span className={tabClass(createStep === "fotos")}>2. Fotos</span>
                  <span className={tabClass(createStep === "itens")}>3. Itens</span>
                  {createdOs ? (
                    <span className="ml-auto rounded-xl bg-white px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm">
                      OS {String(createdOs.numeroOs ?? "")}
                    </span>
                  ) : null}
                </div>

                {createStep === "dados" ? (
                  <div className="space-y-5">
                    <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
                      <div>
                        <h3 className="text-sm font-semibold text-slate-900">Dados da OS</h3>
                        <p className="mt-1 text-sm text-slate-500">
                          Grade única com liberdade para organizar os campos como quiser.
                        </p>
                      </div>

                      {customModule && canManageCustomFields ? (
                        <div className="flex w-full flex-col gap-3 xl:w-auto xl:min-w-[620px]">
                          <div className="flex flex-wrap items-center gap-2 xl:justify-end">
                            <button
                              className={buttonClass()}
                              type="button"
                              onClick={() => {
                                if (showCustomBuilder && !editingCustomFieldId) {
                                  setShowCustomBuilder(false);
                                  setCustomFieldForm(defaultForm(customFieldFormFields));
                                  setCustomFieldErrors({});
                                  return;
                                }

                                setEditingCustomFieldId("");
                                setCustomFieldForm({
                                  ...defaultForm(customFieldFormFields),
                                  aba: osActiveTab,
                                });
                                setCustomFieldErrors({});
                                setShowCustomBuilder(true);
                                setShowOsCreateTabForm(false);
                              }}
                            >
                              <Plus size={15} />
                              {showCustomBuilder && !editingCustomFieldId ? "Fechar campo" : "Adicionar campo"}
                            </button>

                            <button
                              className={buttonClass()}
                              type="button"
                              onClick={() => {
                                setOsLayoutMode((current) => !current);
                                setShowOsCreateTabForm(false);
                              }}
                            >
                              <GripVertical size={15} />
                              {osLayoutMode ? "Concluir layout" : "Organizar campos"}
                            </button>

                            <button
                              className={buttonClass()}
                              type="button"
                              onClick={() => {
                                setShowOsCreateTabForm((current) => !current);
                                setEditingOsTabName("");
                                setOsNewTabName("");
                              }}
                            >
                              <Plus size={15} />
                              {showOsCreateTabForm ? "Fechar aba" : "Adicionar aba"}
                            </button>
                          </div>

                          {showOsCreateTabForm ? (
                            <div className="grid gap-2 sm:grid-cols-[minmax(220px,320px)_auto] xl:justify-end">
                              <input
                                className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
                                value={osNewTabName}
                                maxLength={80}
                                placeholder={editingOsTabName ? "Novo nome da aba" : "Nova aba"}
                                onChange={(event) => setOsNewTabName(event.target.value)}
                                onKeyDown={(event) => {
                                  if (event.key === "Enter") {
                                    event.preventDefault();
                                    void saveOsTabName();
                                  }
                                }}
                              />
                              <button className={buttonClass()} type="button" onClick={() => void saveOsTabName()}>
                                <Plus size={15} />
                                {editingOsTabName ? "Salvar nome" : "Criar aba"}
                              </button>
                            </div>
                          ) : null}
                        </div>
                      ) : null}
                    </div>

                    {osLayoutMode ? (
                      <Notice type="info">
                        Modo de organização ativo. Arraste os campos para mudar a posição e arraste as abas para reorganizar o topo.
                      </Notice>
                    ) : null}

                    {customModule ? (
                      <div className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-3">
                        <div className="flex flex-wrap gap-2">
                          {osTabs.map((tab) => (
                            <div
                              key={tab}
                              className={`inline-flex items-center overflow-hidden rounded-xl text-sm font-medium transition ${
                                osActiveTab === tab
                                  ? "bg-slate-900 text-white"
                                  : "border border-slate-200 bg-white text-slate-700 hover:bg-slate-100"
                              }`}
                            >
                              <button
                                type="button"
                                draggable={canManageCustomFields}
                                onDragStart={() => setDraggingOsTabName(tab)}
                                onDragOver={(event) => event.preventDefault()}
                                onDrop={() => void moveOsTab(tab)}
                                className="px-3 py-2"
                                onClick={() => setOsActiveTab(tab)}
                              >
                                {tab}
                              </button>

                              {canManageCustomFields && tab !== "Principal" ? (
                                <button
                                  type="button"
                                  className={`border-l px-2 py-2 transition ${
                                    osActiveTab === tab
                                      ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                                      : "border-slate-200 text-slate-400 hover:bg-slate-50 hover:text-slate-700"
                                  }`}
                                  title="Editar nome da aba"
                                  onClick={() => openEditOsTab(tab)}
                                >
                                  <Pencil size={13} />
                                </button>
                              ) : null}

                              {canManageCustomFields && tab !== "Principal" ? (
                                <button
                                  type="button"
                                  className={`border-l px-2 py-2 transition ${
                                    osActiveTab === tab
                                      ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                                      : "border-slate-200 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                                  }`}
                                  title="Excluir aba"
                                  onClick={() => void deleteOsTab(tab)}
                                >
                                  <X size={13} />
                                </button>
                              ) : null}
                            </div>
                          ))}
                        </div>
                      </div>
                    ) : null}

                    {osLayoutMode && customFields.length > 0 ? (
                      <div className="space-y-2">
                        <p className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                          Campos criados
                        </p>
                        <div className="flex flex-wrap gap-2">
                          {customFields.map((field) => (
                            <button
                              key={field.id}
                              type="button"
                              className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700 transition hover:bg-slate-100"
                              onClick={() => editOsCustomField(field)}
                            >
                              <GripVertical size={14} className="text-slate-400" />
                              <strong className="text-slate-900">{field.nome}</strong>
                              <span className="text-slate-400">•</span>
                              <span>{normalizeTabName(field.aba)}</span>
                            </button>
                          ))}
                        </div>
                      </div>
                    ) : null}

                    {showCustomBuilder ? (
                      <div className="rounded-3xl border border-slate-200 bg-slate-50 p-5">
                        <div className="mb-5 flex flex-col gap-3 border-b border-slate-200 pb-5 xl:flex-row xl:items-start xl:justify-between">
                          <div>
                            <h3 className="text-lg font-bold text-slate-900">
                              {editingCustomFieldId ? "Editar campo" : "Adicionar campo"}
                            </h3>
                            <p className="mt-1 text-sm text-slate-500">
                              {editingCustomFieldId
                                ? "O tipo do campo fica bloqueado para preservar os dados já salvos."
                                : "Crie campos extras e depois arraste no formulário para ajustar a posição."}
                            </p>
                          </div>

                          <button
                            type="button"
                            className={buttonClass()}
                            onClick={() => {
                              setEditingCustomFieldId("");
                              setShowCustomBuilder(false);
                              setCustomFieldForm(defaultForm(customFieldFormFields));
                              setCustomFieldErrors({});
                            }}
                          >
                            <X size={16} />
                            Fechar
                          </button>
                        </div>

                        <form onSubmit={saveCustomField}>
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
                                        ? { ...field, type: "select", required: true, options: osTabOptions }
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

                          <div className="mt-6 flex flex-wrap justify-end gap-3">
                            {editingCustomFieldId ? (
                              <button
                                className={buttonClass("danger")}
                                type="button"
                                onClick={() => void deleteOsCustomField()}
                              >
                                Excluir campo
                              </button>
                            ) : null}

                            <button
                              className={buttonClass()}
                              type="button"
                              onClick={() => {
                                setEditingCustomFieldId("");
                                setShowCustomBuilder(false);
                                setCustomFieldForm(defaultForm(customFieldFormFields));
                                setCustomFieldErrors({});
                              }}
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

                    <form
                      onSubmit={submitOs}
                      className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm"
                    >
                      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                        {visibleOrderedOsCreateFields.map(({ field }) => (
                          <div
                            key={field.name}
                            className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}
                            draggable={osLayoutMode}
                            onDragStart={() => setDraggingOsFieldName(field.name)}
                            onDragOver={(event) => event.preventDefault()}
                            onDrop={() => void moveOsField(field.name)}
                          >
                            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
                              <div className="mb-3 flex items-center justify-between gap-3">
                                {osLayoutMode ? (
                                  <span className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-500">
                                    <GripVertical size={14} />
                                    Arrastar
                                  </span>
                                ) : (
                                  <span />
                                )}

                                {canManageCustomFields && customFieldByName.has(field.name) ? (
                                  <button
                                    type="button"
                                    className="inline-flex items-center justify-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-600 transition hover:border-slate-300 hover:bg-slate-50 hover:text-slate-900"
                                    onClick={() => editOsCustomField(customFieldByName.get(field.name)!)}
                                  >
                                    Editar campo
                                  </button>
                                ) : null}
                              </div>

                              {field.name === "clienteId" ? (
                                <div className="space-y-3">
                                  <label className="block text-sm font-medium text-slate-700">Cliente</label>
                                  <select
                                    className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
                                    value={String(osForm.clienteId ?? "")}
                                    onChange={(event) => {
                                      const value = event.target.value;
                                      setOsForm((current) => ({ ...current, clienteId: value, aparelhoId: "" }));
                                      setOsErrors((current) => {
                                        const next = { ...current };
                                        delete next.clienteId;
                                        delete next.aparelhoId;
                                        return next;
                                      });
                                    }}
                                  >
                                    <option value="">Selecione</option>
                                    {clienteOptions.map((item) => (
                                      <option key={String(item.value)} value={String(item.value)}>{String(item.label)}</option>
                                    ))}
                                  </select>
                                  {osErrors[field.name] ? <p className="text-sm text-rose-600">{osErrors[field.name]}</p> : null}
                                  <div className="flex justify-end">
                                    <button type="button" className={buttonClass()} onClick={() => setQuickClienteOpen(true)}>
                                      <Plus size={14} />
                                      Novo cliente
                                    </button>
                                  </div>
                                </div>
                              ) : field.name === "aparelhoId" ? (
                                <div className="space-y-3">
                                  <label className="block text-sm font-medium text-slate-700">Aparelho</label>
                                  <select
                                    className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 disabled:bg-slate-100 disabled:text-slate-400"
                                    value={String(osForm.aparelhoId ?? "")}
                                    disabled={!clienteSelecionado}
                                    onChange={(event) => updateOs("aparelhoId", event.target.value)}
                                  >
                                    <option value="">{clienteSelecionado ? "Selecione" : "Selecione um cliente primeiro"}</option>
                                    {aparelhoOptions.map((item) => (
                                      <option key={String(item.value)} value={String(item.value)}>{String(item.label)}</option>
                                    ))}
                                  </select>
                                  {osErrors[field.name] ? <p className="text-sm text-rose-600">{osErrors[field.name]}</p> : null}
                                  {!clienteSelecionado ? (
                                    <div className="rounded-2xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                                      Selecione ou cadastre um cliente antes de escolher ou cadastrar um aparelho.
                                    </div>
                                  ) : null}
                                  {clienteSelecionado && aparelhoOptions.length === 0 ? (
                                    <p className="text-xs text-slate-500">Esse cliente ainda não possui aparelhos cadastrados.</p>
                                  ) : (
                                    <p className="text-xs text-slate-500">O aparelho sempre precisa estar vinculado ao cliente selecionado.</p>
                                  )}
                                  <div className="flex justify-end">
                                    <button
                                      type="button"
                                      className={buttonClass()}
                                      disabled={!clienteSelecionado}
                                      onClick={() => {
                                        if (!clienteSelecionado) {
                                          setFailure("Selecione ou cadastre um cliente antes de criar um aparelho.");
                                          return;
                                        }

                                        resetQuickAparelhoModal();
                                        setQuickAparelhoOpen(true);
                                      }}
                                    >
                                      <Plus size={14} />
                                      Novo aparelho
                                    </button>
                                  </div>
                                </div>
                              ) : field.name === "tecnicoId" ? (
                                <div className="space-y-3">
                                  <label className="block text-sm font-medium text-slate-700">Técnico</label>
                                  <select
                                    className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
                                    value={String(osForm.tecnicoId ?? "")}
                                    onChange={(event) => updateOs("tecnicoId", event.target.value)}
                                  >
                                    <option value="">Selecione</option>
                                    {tecnicoOptions.map((item) => (
                                      <option key={String(item.value)} value={String(item.value)}>{String(item.label)}</option>
                                    ))}
                                  </select>
                                  {osErrors[field.name] ? <p className="text-sm text-rose-600">{osErrors[field.name]}</p> : null}
                                  <div className="flex justify-end">
                                    <button type="button" className={buttonClass()} onClick={() => setQuickTecnicoOpen(true)}>
                                      <Plus size={14} />
                                      Novo técnico
                                    </button>
                                  </div>
                                </div>
                              ) : (
                                <FieldRenderer
                                  field={field}
                                  value={osForm[field.name]}
                                  error={osErrors[field.name]}
                                  onChange={updateOs}
                                />
                              )}
                            </div>
                          </div>
                        ))}
                      </div>

                      <div className="mt-6 flex justify-end gap-3">
                        <button type="button" className={buttonClass()} onClick={resetCreateFlow}>
                          Cancelar
                        </button>

                        <button type="submit" className={buttonClass("primary")}>
                          <ArrowRight size={16} />
                          Salvar e continuar
                        </button>
                      </div>
                    </form>
                  </div>
                ) : createStep === "fotos" ? (
                  <div className="space-y-5">
                    <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
                      <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <h3 className="text-sm font-semibold text-slate-900">Fotos da ordem de servico</h3>
                          <p className="mt-1 text-sm text-slate-500">
                            Adicione as fotos agora, se quiser que elas ja fiquem vinculadas a esta OS.
                          </p>
                        </div>
                        <span className="rounded-2xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs font-medium text-emerald-700">
                          As fotos adicionadas aqui ficam disponiveis na OS, no acompanhamento e nas exportacoes.
                        </span>
                      </div>

                      <OsPhotoPanel
                        title="Galeria da OS"
                        helper="Use esta etapa para registrar a entrada do aparelho, detalhes do defeito ou qualquer evidencia importante."
                        emptyText="Nenhuma foto adicionada ainda. Se preferir, voce pode seguir sem fotos."
                        uploadLabel="Adicionar foto"
                        photos={osPhotos}
                        description={photoDescription}
                        imageUrl={photoUrl}
                        photoLoading={photoLoading}
                        onDescriptionChange={setPhotoDescription}
                        onImageUrlChange={setPhotoUrl}
                        onUpload={(file) => {
                          void uploadOsPhoto(file);
                        }}
                        onUploadFromUrl={() => {
                          void uploadOsPhotoByUrl();
                        }}
                        onDelete={(fotoId) => {
                          void deleteOsPhoto(fotoId);
                        }}
                      />

                      <div className="mt-6 flex justify-end gap-3">
                        <button
                          className={buttonClass("primary")}
                          type="button"
                          onClick={() => setCreateStep("itens")}
                        >
                          {osPhotos.length > 0 ? "Continuar para itens" : "Pular fotos"}
                        </button>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="space-y-5">
                    <div className="rounded-3xl border border-slate-200 bg-slate-50 p-5">
                      <h3 className="text-sm font-semibold text-slate-900">Adicionar item</h3>
                      <p className="mt-1 text-sm text-slate-500">
                        Lance serviços e peças na ordem criada.
                      </p>

                      <div className="mt-4 flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
                        <div className="flex flex-wrap gap-2">
                          {itemTabs.map((tab) => (
                            <div
                              key={tab}
                              className={`inline-flex items-center overflow-hidden rounded-xl text-sm font-medium transition ${
                                itemActiveTab === tab
                                  ? "bg-slate-900 text-white"
                                  : "border border-slate-200 bg-white text-slate-700 hover:bg-slate-100"
                              }`}
                            >
                              <button
                                type="button"
                                draggable={canManageCustomFields}
                                onDragStart={() => setDraggingItemTabName(tab)}
                                onDragOver={(event) => event.preventDefault()}
                                onDrop={() => void moveItemTab(tab)}
                                className="px-3 py-2"
                                onClick={() => setItemActiveTab(tab)}
                              >
                                {tab}
                              </button>

                              {canManageCustomFields && tab !== "Principal" ? (
                                <button
                                  type="button"
                                  className={`border-l px-2 py-2 transition ${
                                    itemActiveTab === tab
                                      ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                                      : "border-slate-200 text-slate-400 hover:bg-slate-50 hover:text-slate-700"
                                  }`}
                                  title="Editar nome da aba"
                                  onClick={() => openEditItemTab(tab)}
                                >
                                  <Pencil size={13} />
                                </button>
                              ) : null}

                              {canManageCustomFields && tab !== "Principal" ? (
                                <button
                                  type="button"
                                  className={`border-l px-2 py-2 transition ${
                                    itemActiveTab === tab
                                      ? "border-white/20 text-white/80 hover:bg-white/10 hover:text-white"
                                      : "border-slate-200 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                                  }`}
                                  title="Excluir aba"
                                  onClick={() => void deleteItemTab(tab)}
                                >
                                  <X size={13} />
                                </button>
                              ) : null}
                            </div>
                          ))}
                        </div>

                        {itemCustomModule && canManageCustomFields ? (
                          <div className="flex w-full flex-col gap-3 xl:w-auto xl:min-w-[620px]">
                            <div className="flex flex-wrap items-center gap-2 xl:justify-end">
                              <button
                                className={buttonClass()}
                                type="button"
                                onClick={() => {
                                  if (showItemCustomBuilder && !editingItemCustomFieldId) {
                                    setShowItemCustomBuilder(false);
                                    setItemCustomFieldForm(defaultForm(customFieldFormFields));
                                    setItemCustomFieldErrors({});
                                    return;
                                  }

                                  setEditingItemCustomFieldId("");
                                  setItemCustomFieldForm({
                                    ...defaultForm(customFieldFormFields),
                                    aba: itemActiveTab,
                                  });
                                  setItemCustomFieldErrors({});
                                  setShowItemCustomBuilder(true);
                                  setShowItemCreateTabForm(false);
                                }}
                              >
                                <Plus size={15} />
                                {showItemCustomBuilder && !editingItemCustomFieldId
                                  ? "Fechar campo"
                                  : "Adicionar campo"}
                              </button>

                              <button
                                className={buttonClass()}
                                type="button"
                                onClick={() => {
                                  setItemLayoutMode((current) => !current);
                                  setShowItemCreateTabForm(false);
                                }}
                              >
                                <GripVertical size={15} />
                                {itemLayoutMode ? "Concluir layout" : "Organizar campos"}
                              </button>

                              <button
                                className={buttonClass()}
                                type="button"
                                onClick={() => {
                                  setShowItemCreateTabForm((current) => !current);
                                  setEditingItemTabName("");
                                  setItemNewTabName("");
                                }}
                              >
                                <Plus size={15} />
                                {showItemCreateTabForm ? "Fechar aba" : "Adicionar aba"}
                              </button>
                            </div>

                            {showItemCreateTabForm ? (
                              <div className="grid gap-2 sm:grid-cols-[minmax(220px,320px)_auto] xl:justify-end">
                                <input
                                  className="h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
                                  value={itemNewTabName}
                                  maxLength={80}
                                  placeholder={editingItemTabName ? "Novo nome da aba dos itens" : "Nova aba dos itens"}
                                  onChange={(event) => setItemNewTabName(event.target.value)}
                                  onKeyDown={(event) => {
                                    if (event.key === "Enter") {
                                      event.preventDefault();
                                      void saveItemTabName();
                                    }
                                  }}
                                />
                                <button className={buttonClass()} type="button" onClick={() => void saveItemTabName()}>
                                  <Plus size={15} />
                                  {editingItemTabName ? "Salvar nome" : "Criar aba"}
                                </button>
                              </div>
                            ) : null}
                          </div>
                        ) : null}
                      </div>

                      {itemLayoutMode ? (
                        <Notice type="info">
                          Modo de organizacao ativo. Arraste os campos dos itens para mudar a posicao e arraste as abas para reorganizar o topo.
                        </Notice>
                      ) : null}

                      {itemLayoutMode && itemCustomFields.length > 0 && canManageCustomFields ? (
                        <div className="mt-4 space-y-2">
                          <p className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Campos criados
                          </p>
                          <div className="flex flex-wrap gap-2">
                            {itemCustomFields.map((field) => (
                              <button
                                key={field.id}
                                type="button"
                                className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 transition hover:bg-slate-100"
                                onClick={() => editItemCustomField(field)}
                              >
                                <GripVertical size={14} className="text-slate-400" />
                                <strong className="text-slate-900">{field.nome}</strong>
                                <span className="text-slate-400">•</span>
                                <span>{normalizeTabName(field.aba)}</span>
                              </button>
                            ))}
                          </div>
                        </div>
                      ) : null}

                      {showItemCustomBuilder ? (
                        <div className="mt-4 rounded-3xl border border-slate-200 bg-white p-5">
                          <div className="mb-5 flex flex-col gap-3 border-b border-slate-200 pb-5 xl:flex-row xl:items-start xl:justify-between">
                            <div>
                              <h3 className="text-lg font-bold text-slate-900">
                                {editingItemCustomFieldId ? "Editar campo do item" : "Adicionar campo do item"}
                              </h3>
                              <p className="mt-1 text-sm text-slate-500">
                                {editingItemCustomFieldId
                                  ? "O tipo do campo fica bloqueado para preservar os itens ja salvos."
                                  : "Crie campos extras para pecas e servicos e organize por abas."}
                              </p>
                            </div>

                            <button
                              type="button"
                              className={buttonClass()}
                              onClick={() => {
                                setEditingItemCustomFieldId("");
                                setShowItemCustomBuilder(false);
                                setItemCustomFieldForm(defaultForm(customFieldFormFields));
                                setItemCustomFieldErrors({});
                              }}
                            >
                              <X size={16} />
                              Fechar
                            </button>
                          </div>

                          <form onSubmit={saveItemCustomField}>
                            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                              {customFieldFormFields
                                .filter(
                                  (field) =>
                                    field.name !== "opcoesText" || itemCustomFieldForm.tipo === "select",
                                )
                                .map((field) => (
                                  <FieldRenderer
                                    key={field.name}
                                    field={
                                      editingItemCustomFieldId && field.name === "tipo"
                                        ? { ...field, disabled: true }
                                        : field.name === "aba"
                                          ? { ...field, type: "select", required: true, options: itemTabOptions }
                                        : field
                                    }
                                    value={itemCustomFieldForm[field.name]}
                                    error={itemCustomFieldErrors[field.name]}
                                    onChange={(name, value) => {
                                      setItemCustomFieldForm((current) => ({ ...current, [name]: value }));
                                      setItemCustomFieldErrors((current) => {
                                        const next = { ...current };
                                        delete next[name];
                                        return next;
                                      });
                                    }}
                                  />
                                ))}
                            </div>

                            <div className="mt-6 flex flex-wrap justify-end gap-3">
                              {editingItemCustomFieldId ? (
                                <button
                                  className={buttonClass("danger")}
                                  type="button"
                                  onClick={() => void deleteItemCustomField()}
                                >
                                  Excluir campo
                                </button>
                              ) : null}

                              <button
                                className={buttonClass()}
                                type="button"
                                onClick={() => {
                                  setEditingItemCustomFieldId("");
                                  setShowItemCustomBuilder(false);
                                  setItemCustomFieldForm(defaultForm(customFieldFormFields));
                                  setItemCustomFieldErrors({});
                                }}
                              >
                                Cancelar
                              </button>

                              <button className={buttonClass("primary")} type="submit">
                                {editingItemCustomFieldId ? "Salvar campo" : "Criar campo"}
                              </button>
                            </div>
                          </form>
                        </div>
                      ) : null}

                      <form onSubmit={submitItem} className="mt-4 space-y-5">
                        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                          {visibleOrderedItemFields.map(({ field }) => (
                            <div
                              key={field.name}
                              className={field.span === "full" ? "md:col-span-2 xl:col-span-3" : ""}
                              draggable={itemLayoutMode}
                              onDragStart={() => setDraggingItemFieldName(field.name)}
                              onDragOver={(event) => event.preventDefault()}
                              onDrop={() => void moveItemField(field.name)}
                            >
                              <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
                                <div className="mb-3 flex items-center justify-between gap-3">
                                  {itemLayoutMode ? (
                                    <span className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-medium text-slate-500">
                                      <GripVertical size={14} />
                                      Arrastar
                                    </span>
                                  ) : (
                                    <span />
                                  )}

                                  {canManageCustomFields && itemCustomFieldByName.has(field.name) ? (
                                    <button
                                      type="button"
                                      className="inline-flex items-center justify-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-600 transition hover:border-slate-300 hover:bg-slate-50 hover:text-slate-900"
                                      onClick={() => editItemCustomField(itemCustomFieldByName.get(field.name)!)}
                                    >
                                      Editar campo
                                    </button>
                                  ) : null}
                                </div>

                                <FieldRenderer
                                  field={field}
                                  value={itemForm[field.name]}
                                  error={itemErrors[field.name]}
                                  onChange={updateItem}
                                />
                              </div>
                            </div>
                          ))}
                        </div>

                        <div className="flex justify-end">
                          <button type="submit" className={buttonClass("primary")}>
                            <Plus size={16} />
                            {editingItemId ? "Salvar item" : "Adicionar item"}
                          </button>
                        </div>
                      </form>
                    </div>

                    {itemRows.length > 0 ? (
                      <div className="grid gap-2">
                        {itemRows.map((item) => (
                          <div
                            key={String(item.id ?? "")}
                            draggable
                            onDragStart={() => setDraggingItemId(String(item.id ?? ""))}
                            onDragOver={(event) => event.preventDefault()}
                            onDrop={() => void moveItem(String(item.id ?? ""))}
                            className="flex cursor-grab items-center gap-3 rounded-2xl border border-slate-200 bg-white p-3 text-sm text-slate-700 shadow-sm"
                          >
                            <GripVertical size={16} className="text-slate-400" />
                            <strong className="text-slate-900">{String(item.descricao ?? "-")}</strong>
                            <span className="ml-auto">{formatCurrency(item.valorTotal)}</span>
                          </div>
                        ))}
                      </div>
                    ) : null}

                    <DataTable
                      columns={[
                        { key: "tipoItem", label: "Tipo" },
                        { key: "descricao", label: "Descrição" },
                        { key: "quantidade", label: "Qtd." },
                        {
                          key: "valorUnitario",
                          label: "Unitário",
                          render: (row) => formatCurrency(row.valorUnitario),
                        },
                        ...itemCustomFields.map((field) => ({
                          key: field.chave,
                          label: field.nome,
                          render: (row: ApiRecord) => displayExportValue(row[field.chave]),
                        })),
                        {
                          key: "valorTotal",
                          label: "Total",
                          render: (row) => formatCurrency(row.valorTotal),
                        },
                      ]}
                      rows={itemRows}
                      loading={itens.loading}
                      emptyText="Nenhum item nesta OS."
                      actions={(row) => (
                        <button className={buttonClass()} type="button" onClick={() => editItem(row)}>
                          Editar
                        </button>
                      )}
                    />

                    <div className="flex justify-end gap-3">
                      <button className={buttonClass()} type="button" onClick={() => setCreateStep("fotos")}>
                        Voltar
                      </button>
                      <button className={buttonClass("primary")} type="button" onClick={resetCreateFlow}>
                        Finalizar
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        ) : null}
      </div>
    </PageFrame>
  );
}
