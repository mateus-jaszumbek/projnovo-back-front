import type { ApiRecord } from "../lib/api";
import type { FieldConfig } from "./Ui";

export type FieldMask =
  | "cpf"
  | "cpfCnpj"
  | "cnpj"
  | "phone"
  | "cep"
  | "money"
  | "percentage"
  | "uf"
  | "ncm"
  | "cest"
  | "cfop"
  | "cstCsosn"
  | "origem"
  | "unit"
  | "digits";

export function onlyDigits(value: unknown) {
  return String(value ?? "").replace(/\D/g, "");
}

function isRepeatedDigits(value: string) {
  return /^(\d)\1+$/.test(value);
}

export function isValidCpf(value: unknown) {
  const cpf = onlyDigits(value);
  if (cpf.length !== 11 || isRepeatedDigits(cpf)) return false;

  const calculateDigit = (base: string, factor: number) => {
    const total = base
      .split("")
      .reduce((sum, digit) => sum + Number(digit) * factor--, 0);
    const rest = (total * 10) % 11;
    return rest === 10 ? 0 : rest;
  };

  const firstDigit = calculateDigit(cpf.slice(0, 9), 10);
  const secondDigit = calculateDigit(cpf.slice(0, 10), 11);
  return firstDigit === Number(cpf[9]) && secondDigit === Number(cpf[10]);
}

export function isValidCnpj(value: unknown) {
  const cnpj = onlyDigits(value);
  if (cnpj.length !== 14 || isRepeatedDigits(cnpj)) return false;

  const calculateDigit = (base: string, weights: number[]) => {
    const total = base
      .split("")
      .reduce((sum, digit, index) => sum + Number(digit) * weights[index], 0);
    const rest = total % 11;
    return rest < 2 ? 0 : 11 - rest;
  };

  const firstDigit = calculateDigit(cnpj.slice(0, 12), [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  const secondDigit = calculateDigit(cnpj.slice(0, 13), [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  return firstDigit === Number(cnpj[12]) && secondDigit === Number(cnpj[13]);
}

export function passwordChecks(value: unknown) {
  const password = String(value ?? "");

  return {
    minLength: password.length >= 7,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    number: /\d/.test(password),
    special: /[^A-Za-z0-9]/.test(password),
  };
}

export function passwordStrength(value: unknown) {
  const checks = passwordChecks(value);
  const score = Object.values(checks).filter(Boolean).length;

  if (score <= 2) return { score, label: "Fraca", tone: "weak" as const };
  if (score <= 4) return { score, label: "Boa", tone: "good" as const };
  return { score, label: "Forte", tone: "strong" as const };
}

export function isStrongPassword(value: unknown) {
  const checks = passwordChecks(value);
  return checks.minLength && checks.uppercase && checks.lowercase && checks.number;
}

function formatCpf(value: string) {
  return value
    .slice(0, 11)
    .replace(/^(\d{3})(\d)/, "$1.$2")
    .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/\.(\d{3})(\d)/, ".$1-$2");
}

function formatCnpj(value: string) {
  return value
    .slice(0, 14)
    .replace(/^(\d{2})(\d)/, "$1.$2")
    .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/\.(\d{3})(\d)/, ".$1/$2")
    .replace(/(\d{4})(\d)/, "$1-$2");
}

function formatMoney(value: unknown) {
  const digits = onlyDigits(value);
  const cents = Number(digits || 0);

  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(cents / 100);
}

function parseMoney(value: unknown) {
  const digits = onlyDigits(value);
  return Number(digits || 0) / 100;
}

function applyMask(mask: FieldMask | undefined, value: unknown, maxLength?: number) {
  const raw = String(value ?? "");
  const digits = onlyDigits(raw);

  switch (mask) {
    case "cpf":
      return formatCpf(digits);
    case "cpfCnpj":
      return digits.length <= 11 ? formatCpf(digits) : formatCnpj(digits);
    case "cnpj":
      return formatCnpj(digits);
    case "phone":
      return digits
        .slice(0, 11)
        .replace(/^(\d{2})(\d)/, "($1) $2")
        .replace(/(\d{4,5})(\d{4})$/, "$1-$2");
    case "cep":
      return digits.slice(0, 8).replace(/^(\d{5})(\d)/, "$1-$2");
    case "money":
      return formatMoney(raw);
    case "percentage":
      return `${raw.replace(/[^\d,.]/g, "").slice(0, 8)}${raw ? "%" : ""}`;
    case "uf":
      return raw.replace(/[^a-z]/gi, "").toUpperCase().slice(0, 2);
    case "ncm":
      return digits.slice(0, 8);
    case "cest":
      return digits.slice(0, 7);
    case "cfop":
      return digits.slice(0, 4);
    case "cstCsosn":
      return digits.slice(0, 3);
    case "origem":
      return digits.slice(0, 1);
    case "unit":
      return raw.replace(/[^a-z0-9]/gi, "").toUpperCase().slice(0, 10);
    case "digits":
      return digits.slice(0, maxLength);
    default:
      return maxLength ? raw.slice(0, maxLength) : raw;
  }
}

function normalizeFieldValue(field: FieldConfig, value: unknown) {
  if (typeof value === "string" && value.trim() === "") return "";

  switch (field.mask) {
    case "cpfCnpj":
    case "cpf":
    case "cnpj":
    case "phone":
    case "cep":
    case "ncm":
    case "cest":
    case "cfop":
    case "cstCsosn":
    case "origem":
    case "digits":
      return onlyDigits(value);
    case "money":
      return parseMoney(value);
    case "percentage":
      return Number(String(value ?? "").replace("%", "").replace(",", ".").replace(/[^\d.]/g, ""));
    case "uf":
      return String(value ?? "").replace(/[^a-z]/gi, "").toUpperCase().slice(0, 2);
    case "unit":
      return String(value ?? "").replace(/[^a-z0-9]/gi, "").toUpperCase().slice(0, 10);
    default:
      return typeof value === "string" ? value.trim() : value;
  }
}

export function formatFieldInput(field: FieldConfig, value: unknown) {
  if (field.type === "currency") return applyMask("money", value, field.maxLength);
  if (field.type === "percentage") return applyMask("percentage", value, field.maxLength);
  return applyMask(field.mask, value, field.maxLength);
}

export function maxLengthForField(field: FieldConfig) {
  if (field.maxLength) return field.maxLength;

  switch (field.mask) {
    case "cpfCnpj":
      return 18;
    case "cpf":
      return 14;
    case "cnpj":
      return 18;
    case "phone":
      return 15;
    case "cep":
      return 9;
    case "money":
      return 18;
    case "uf":
      return 2;
    case "ncm":
      return 8;
    case "cest":
      return 7;
    case "cfop":
      return 4;
    case "cstCsosn":
      return 3;
    case "origem":
      return 1;
    case "unit":
      return 10;
    default:
      return undefined;
  }
}

export function inputModeForField(field: FieldConfig) {
  if (field.type === "email") return "email";
  if (field.type === "number" || field.type === "currency" || field.type === "percentage") return "decimal";

  switch (field.mask) {
    case "cpfCnpj":
    case "cpf":
    case "cnpj":
    case "phone":
    case "cep":
    case "ncm":
    case "cest":
    case "cfop":
    case "cstCsosn":
    case "origem":
    case "digits":
    case "money":
    case "percentage":
      return "numeric";
    default:
      return undefined;
  }
}

export function defaultForm(fields: FieldConfig[]) {
  return fields.reduce<ApiRecord>((form, field) => {
    const value = field.defaultValue ?? (field.type === "checkbox" ? false : "");
    form[field.name] = field.mask === "money" && value !== "" ? formatFieldInput(field, value) : value;
    return form;
  }, {});
}

export function formFromRecord(fields: FieldConfig[], record: ApiRecord) {
  return fields.reduce<ApiRecord>((form, field) => {
    const value = record[field.name];
    form[field.name] =
      field.type === "checkbox"
        ? Boolean(value)
        : formatFieldInput(field, String(value ?? "").slice(0, field.type === "date" ? 10 : undefined));
    return form;
  }, {});
}

export function payloadFromForm(fields: FieldConfig[], form: ApiRecord) {
  return fields.reduce<ApiRecord>((payload, field) => {
    const value = form[field.name];

    if (field.type === "number" || field.type === "currency" || field.type === "percentage") {
      if (field.mask === "money") {
        payload[field.name] = value === "" && field.nullable ? null : parseMoney(value);
        return payload;
      }

      if (field.type === "currency") {
        payload[field.name] = value === "" && field.nullable ? null : parseMoney(value);
        return payload;
      }

      if (field.type === "percentage") {
        payload[field.name] = value === "" && field.nullable ? null : Number(String(value ?? "").replace("%", "").replace(",", ".").replace(/[^\d.]/g, ""));
        return payload;
      }

      payload[field.name] =
        value === "" || value === null || value === undefined
          ? field.nullable
            ? null
            : 0
          : Number(value);
      return payload;
    }

    if (field.type === "checkbox") {
      payload[field.name] = Boolean(value);
      return payload;
    }

    if ((field.type === "select" || field.type === "date") && value === "") {
      payload[field.name] = field.required ? "" : null;
      return payload;
    }

    const normalized = normalizeFieldValue(field, value);
    payload[field.name] = normalized === "" && !field.required ? null : normalized;
    return payload;
  }, {});
}

export function validateForm(fields: FieldConfig[], form: ApiRecord) {
  return fields.reduce<Record<string, string>>((errors, field) => {
    if (field.disabled) return errors;

    const rawValue = form[field.name];
    const normalized = normalizeFieldValue(field, rawValue);
    const empty = normalized === "" || normalized === null || normalized === undefined;

    if (field.required && empty) {
      errors[field.name] = `${field.label} Ã© obrigatÃ³rio.`;
      return errors;
    }

    if (empty) return errors;

    if (field.type === "email") {
      const validEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(normalized));
      if (!validEmail) errors[field.name] = "Informe um e-mail vÃ¡lido.";
    }

    if (field.type === "number" || field.type === "currency" || field.type === "percentage") {
      const numberValue = field.mask === "money" || field.type === "currency"
        ? parseMoney(rawValue)
        : field.type === "percentage"
          ? Number(String(rawValue ?? "").replace("%", "").replace(",", ".").replace(/[^\d.]/g, ""))
          : Number(rawValue);
      if (!Number.isFinite(numberValue)) {
        errors[field.name] = `${field.label} deve ser um nÃºmero vÃ¡lido.`;
      } else if (field.min !== undefined && numberValue < field.min) {
        errors[field.name] = `${field.label} deve ser maior ou igual a ${field.min}.`;
      } else if (field.max !== undefined && numberValue > field.max) {
        errors[field.name] = `${field.label} deve ser menor ou igual a ${field.max}.`;
      }
    }

    const stringValue = String(normalized);
    if (field.minLength && stringValue.length < field.minLength) {
      errors[field.name] = `${field.label} deve ter pelo menos ${field.minLength} caracteres.`;
    }

    if (field.maxLength && stringValue.length > field.maxLength) {
      errors[field.name] = `${field.label} deve ter no mÃ¡ximo ${field.maxLength} caracteres.`;
    }

    if (field.type === "date" && !/^\d{4}-\d{2}-\d{2}$/.test(stringValue)) {
      errors[field.name] = `${field.label} deve ter uma data vÃ¡lida.`;
    }

    if (field.mask === "cpf" && !isValidCpf(stringValue)) {
      errors[field.name] = `${field.label} deve ter um CPF vÃ¡lido.`;
    }
    if (field.mask === "cpfCnpj") {
      const document = onlyDigits(stringValue);
      const validDocument = document.length === 11 ? isValidCpf(document) : isValidCnpj(document);
      if (![11, 14].includes(document.length) || !validDocument) {
        errors[field.name] = `${field.label} deve ter CPF ou CNPJ vÃ¡lido.`;
      }
    }
    if (field.mask === "cnpj" && !isValidCnpj(stringValue)) {
      errors[field.name] = `${field.label} deve ter um CNPJ vÃ¡lido.`;
    }
    if (field.mask === "phone" && ![10, 11].includes(onlyDigits(stringValue).length)) {
      errors[field.name] = `${field.label} deve ter DDD e nÃºmero.`;
    }
    if (field.mask === "cep" && onlyDigits(stringValue).length !== 8) {
      errors[field.name] = `${field.label} deve ter 8 dÃ­gitos.`;
    }
    if (field.mask === "uf" && stringValue.length !== 2) {
      errors[field.name] = `${field.label} deve ter 2 letras.`;
    }
    if (field.mask === "ncm" && onlyDigits(stringValue).length !== 8) {
      errors[field.name] = `${field.label} deve ter 8 dÃ­gitos.`;
    }
    if (field.mask === "cest" && onlyDigits(stringValue).length !== 7) {
      errors[field.name] = `${field.label} deve ter 7 dÃ­gitos.`;
    }
    if (field.mask === "cfop" && onlyDigits(stringValue).length !== 4) {
      errors[field.name] = `${field.label} deve ter 4 dÃ­gitos.`;
    }
    if (field.mask === "cstCsosn" && ![2, 3].includes(onlyDigits(stringValue).length)) {
      errors[field.name] = `${field.label} deve ter 2 ou 3 dÃ­gitos.`;
    }
    if (field.mask === "origem" && !/^[0-8]$/.test(stringValue)) {
      errors[field.name] = `${field.label} deve ser um nÃºmero entre 0 e 8.`;
    }
    if (field.strongPassword && !isStrongPassword(rawValue)) {
      errors[field.name] = `${field.label} deve ter mais de 6 caracteres, letra maiÃºscula, letra minÃºscula e nÃºmero.`;
    }
    if (field.sameAs) {
      const otherValue = form[field.sameAs];
      if (String(rawValue ?? "") !== String(otherValue ?? "")) {
        errors[field.name] = `${field.label} deve ser igual a senha.`;
      }
    }

    return errors;
  }, {});
}

export function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : "NÃ£o foi possÃ­vel concluir a aÃ§Ã£o.";
}

export function formatCurrency(value: unknown) {
  const number = typeof value === "number" ? value : Number(value ?? 0);
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(Number.isFinite(number) ? number : 0);
}

export function formatDate(value: unknown) {
  if (!value) return "-";
  const date = new Date(String(value));
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat("pt-BR").format(date);
}

export function displayValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "boolean") return value ? "Sim" : "NÃ£o";
  if (typeof value === "number") return String(value);
  return String(value);
}

