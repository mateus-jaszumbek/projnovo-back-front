export type ApiRecord = Record<string, unknown>;

export type AuthSession = {
  token: string;
  expiresAtUtc: string;
  usuarioId: string;
  nome: string;
  email: string;
  perfil: string;
  empresaId?: string;
  empresaNomeFantasia?: string;
  isSuperAdmin: boolean;
  nivelAcesso: number;
};

type AuthResponse = {
  accessToken: string;
  expiresAtUtc: string;
  usuarioId: string;
  nome: string;
  email: string;
  isSuperAdmin: boolean;
  empresaId?: string;
  empresaNomeFantasia?: string;
  perfil?: string;
  nivelAcesso?: number;
};

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "DELETE" | "PATCH";
  body?: unknown;
  signal?: AbortSignal;
};

const SESSION_KEY = "servicosapp.session";
const DEFAULT_API_URL = import.meta.env.DEV
  ? "http://localhost:5221/api"
  : "https://52.207.193.4/api";

export const API_BASE_URL =
  import.meta.env.VITE_API_URL?.replace(/\/$/, "") ?? DEFAULT_API_URL;

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

export function getSession(): AuthSession | null {
  const raw = localStorage.getItem(SESSION_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    localStorage.removeItem(SESSION_KEY);
    return null;
  }
}

export function saveSession(session: AuthSession) {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession() {
  localStorage.removeItem(SESSION_KEY);
}

export function apiBaseUrl() {
  return API_BASE_URL;
}

export function apiResourceUrl(value?: string | null) {
  if (!value) return "";
  if (/^(https?:|data:)/i.test(value)) return value;
  return `${API_BASE_URL}${value.startsWith("/") ? value : `/${value}`}`;
}

export function apiAbsoluteResourceUrl(value?: string | null) {
  const normalized = apiResourceUrl(value);
  if (!normalized || /^(https?:|data:)/i.test(normalized)) return normalized;
  return new URL(normalized, window.location.origin).toString();
}

function extractErrorMessage(payload: unknown, fallback: string) {
  if (!payload || typeof payload !== "object") return fallback;

  const record = payload as ApiRecord;
  const errors = record.errors;
  if (errors && typeof errors === "object") {
    const messages = Object.entries(errors as Record<string, unknown[]>)
      .flatMap(([field, fieldErrors]) =>
        Array.isArray(fieldErrors)
          ? fieldErrors.map((message) => `${field}: ${String(message)}`)
          : [],
      )
      .slice(0, 6);

    if (messages.length > 0) return messages.join(" | ");
  }

  return String(record.detail ?? record.message ?? record.title ?? fallback);
}

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const session = getSession();
  const headers = new Headers();

  if (options.body !== undefined) headers.set("Content-Type", "application/json");
  if (session?.token) headers.set("Authorization", `Bearer ${session.token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  });

  const hasJson = response.headers.get("content-type")?.includes("application/json");
  const payload = hasJson ? await response.json() : null;

  if (!response.ok) {
    throw new ApiError(
      extractErrorMessage(payload, "A API recusou a solicitação."),
      response.status,
    );
  }

  return payload as T;
}

export async function apiUpload<T>(
  path: string,
  formData: FormData,
  options: Pick<RequestOptions, "method" | "signal"> = {},
): Promise<T> {
  const session = getSession();
  const headers = new Headers();
  if (session?.token) headers.set("Authorization", `Bearer ${session.token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "POST",
    headers,
    body: formData,
    signal: options.signal,
  });

  const hasJson = response.headers.get("content-type")?.includes("application/json");
  const payload = hasJson ? await response.json() : null;

  if (!response.ok) {
    throw new ApiError(
      extractErrorMessage(payload, "A API recusou o envio do arquivo."),
      response.status,
    );
  }

  return payload as T;
}

export async function login(email: string, senha: string) {
  const response = await apiRequest<AuthResponse>("/auth/login", {
    method: "POST",
    body: { email, senha },
  });

  return normalizeSession(response);
}

export async function registrarEmpresa(payload: ApiRecord) {
  const response = await apiRequest<AuthResponse>("/auth/registrar-empresa", {
    method: "POST",
    body: payload,
  });

  return normalizeSession(response);
}

function normalizeSession(response: AuthResponse): AuthSession {
  return {
    token: response.accessToken,
    expiresAtUtc: response.expiresAtUtc,
    usuarioId: response.usuarioId,
    nome: response.nome,
    email: response.email,
    perfil: response.perfil ?? (response.isSuperAdmin ? "super-admin" : "owner"),
    empresaId: response.empresaId,
    empresaNomeFantasia: response.empresaNomeFantasia,
    isSuperAdmin: response.isSuperAdmin,
    nivelAcesso: response.nivelAcesso ?? (response.isSuperAdmin ? 5 : 1),
  };
}
