export type ApiRecord = Record<string, unknown>;

export type AuthSession = {
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

type DownloadResult = {
  blob: Blob;
  contentType: string;
  fileName: string | null;
};

const SESSION_KEY = "servicosapp.session";
export const COMPANY_UPDATED_EVENT = "empresa-atualizada";

// Em dev, usa proxy do Vite (/api → localhost:5221) para garantir same-origin e cookies httpOnly
const DEFAULT_API_URL =
  typeof window !== "undefined"
    ? new URL("/api", window.location.origin).toString().replace(/\/$/, "")
    : "/api";

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

export function notifyCompanyUpdated() {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new Event(COMPANY_UPDATED_EVENT));
  }
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

function extractFileName(contentDisposition: string | null) {
  if (!contentDisposition) return null;

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1]);
    } catch {
      return utf8Match[1];
    }
  }

  const fileNameMatch = contentDisposition.match(/filename="?([^";]+)"?/i);
  return fileNameMatch?.[1] ?? null;
}

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const headers = new Headers();

  if (options.body !== undefined) headers.set("Content-Type", "application/json");

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
    credentials: "include",
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
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "POST",
    body: formData,
    signal: options.signal,
    credentials: "include",
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

export async function apiDownload(
  path: string,
  options: Pick<RequestOptions, "method" | "signal"> = {},
): Promise<DownloadResult> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    signal: options.signal,
    credentials: "include",
  });

  if (!response.ok) {
    const hasJson = response.headers.get("content-type")?.includes("application/json");
    const payload = hasJson ? await response.json() : null;

    throw new ApiError(
      extractErrorMessage(payload, "A API recusou o download do arquivo."),
      response.status,
    );
  }

  return {
    blob: await response.blob(),
    contentType: response.headers.get("content-type") ?? "application/octet-stream",
    fileName: extractFileName(response.headers.get("content-disposition")),
  };
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

export async function logout() {
  try {
    await fetch(`${API_BASE_URL}/auth/logout`, {
      method: "POST",
      credentials: "include",
    });
  } catch {
    // ignora erros de rede no logout
  }
  clearSession();
}

function normalizeSession(response: AuthResponse): AuthSession {
  return {
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
