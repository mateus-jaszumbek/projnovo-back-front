import type { ApiRecord } from "../lib/api";
import { LEGAL_CONFIG } from "../legal/legalConfig";

function textValue(value: unknown) {
  return typeof value === "string" ? value.trim() : "";
}

function digitsOnly(value: string) {
  return value.replace(/\D/g, "");
}

export function resolveSupportEmail(company?: ApiRecord | null) {
  return LEGAL_CONFIG.supportEmail || textValue(company?.email);
}

export function resolveSupportPhone(company?: ApiRecord | null) {
  return LEGAL_CONFIG.supportWhatsApp || textValue(company?.telefone);
}

export function formatSupportPhone(value?: string | null) {
  const digits = digitsOnly(String(value ?? ""));

  if (!digits) return "";

  const localDigits =
    digits.length > 11 && digits.startsWith("55")
      ? digits.slice(2)
      : digits;

  if (localDigits.length === 11) {
    return `(${localDigits.slice(0, 2)}) ${localDigits.slice(2, 7)}-${localDigits.slice(7)}`;
  }

  if (localDigits.length === 10) {
    return `(${localDigits.slice(0, 2)}) ${localDigits.slice(2, 6)}-${localDigits.slice(6)}`;
  }

  return textValue(value);
}

export function buildSupportWhatsAppUrl(phone?: string | null, message?: string) {
  const digits = digitsOnly(String(phone ?? ""));
  if (!digits) return "";

  const normalized = digits.length <= 11 ? `55${digits}` : digits;
  const baseUrl = `https://wa.me/${normalized}`;

  return message?.trim()
    ? `${baseUrl}?text=${encodeURIComponent(message.trim())}`
    : baseUrl;
}
