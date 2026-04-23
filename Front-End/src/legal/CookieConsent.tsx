import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { BarChart3, Check, Cookie, Settings2, ShieldCheck, X } from "lucide-react";
import type { ReactNode } from "react";
import { LEGAL_DOCUMENT_VERSIONS } from "./legalConfig";

type CookiePreferences = {
  essential: true;
  analytics: boolean;
  version: string;
  updatedAt: string;
};

type CookieConsentContextValue = {
  preferences: CookiePreferences | null;
  hasDecision: boolean;
  isPreferencesOpen: boolean;
  acceptAll: () => void;
  acceptEssentialOnly: () => void;
  savePreferences: (analytics: boolean) => void;
  openPreferences: () => void;
  closePreferences: () => void;
};

const COOKIE_CONSENT_NAME = "servicosapp_cookie_consent";
const COOKIE_CONSENT_MAX_AGE = 60 * 60 * 24 * 180;

const CookieConsentContext = createContext<CookieConsentContextValue | null>(null);

function readCookie(name: string) {
  if (typeof document === "undefined") return null;

  const prefix = `${name}=`;
  const entry = document.cookie
    .split(";")
    .map((item) => item.trim())
    .find((item) => item.startsWith(prefix));

  if (!entry) return null;
  return decodeURIComponent(entry.slice(prefix.length));
}

function writeConsentCookie(preferences: CookiePreferences) {
  if (typeof document === "undefined") return;

  const secureFlag =
    typeof window !== "undefined" && window.location.protocol === "https:" ? "; Secure" : "";

  document.cookie = [
    `${COOKIE_CONSENT_NAME}=${encodeURIComponent(JSON.stringify(preferences))}`,
    `Max-Age=${COOKIE_CONSENT_MAX_AGE}`,
    "Path=/",
    "SameSite=Lax",
    secureFlag,
  ]
    .filter(Boolean)
    .join("; ");
}

function parseConsentCookie() {
  const raw = readCookie(COOKIE_CONSENT_NAME);
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw) as Partial<CookiePreferences>;
    if (parsed.version !== LEGAL_DOCUMENT_VERSIONS.cookies) return null;

    return {
      essential: true,
      analytics: Boolean(parsed.analytics),
      version: LEGAL_DOCUMENT_VERSIONS.cookies,
      updatedAt: parsed.updatedAt || new Date().toISOString(),
    } satisfies CookiePreferences;
  } catch {
    return null;
  }
}

function buildPreferences(analytics: boolean): CookiePreferences {
  return {
    essential: true,
    analytics,
    version: LEGAL_DOCUMENT_VERSIONS.cookies,
    updatedAt: new Date().toISOString(),
  };
}

function CookieRow({
  title,
  description,
  enabled,
  locked,
  onToggle,
  icon,
}: {
  title: string;
  description: string;
  enabled: boolean;
  locked?: boolean;
  onToggle?: () => void;
  icon: ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-4 rounded-2xl border border-slate-200 bg-white p-4">
      <div className="flex gap-3">
        <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
          {icon}
        </div>

        <div>
          <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
          <p className="mt-1 text-sm leading-6 text-slate-600">{description}</p>
        </div>
      </div>

      <button
        type="button"
        disabled={locked}
        onClick={onToggle}
        className={[
          "mt-1 inline-flex h-7 w-12 shrink-0 items-center rounded-full border transition",
          enabled
            ? "border-slate-900 bg-slate-900 justify-end"
            : "border-slate-300 bg-slate-200 justify-start",
          locked ? "cursor-not-allowed opacity-80" : "cursor-pointer",
        ].join(" ")}
        aria-pressed={enabled}
        aria-label={locked ? `${title} sempre ativo` : `Alternar ${title}`}
      >
        <span className="mx-1 flex h-5 w-5 items-center justify-center rounded-full bg-white text-slate-900 shadow-sm" />
      </button>
    </div>
  );
}

export function CookieConsentProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<CookiePreferences | null>(null);
  const [isPreferencesOpen, setIsPreferencesOpen] = useState(false);

  useEffect(() => {
    setPreferences(parseConsentCookie());
  }, []);

  function persistPreferences(nextPreferences: CookiePreferences) {
    writeConsentCookie(nextPreferences);
    setPreferences(nextPreferences);
    setIsPreferencesOpen(false);
  }

  function acceptAll() {
    persistPreferences(buildPreferences(true));
  }

  function acceptEssentialOnly() {
    persistPreferences(buildPreferences(false));
  }

  function savePreferences(analytics: boolean) {
    persistPreferences(buildPreferences(analytics));
  }

  const value = useMemo<CookieConsentContextValue>(
    () => ({
      preferences,
      hasDecision: Boolean(preferences),
      isPreferencesOpen,
      acceptAll,
      acceptEssentialOnly,
      savePreferences,
      openPreferences: () => setIsPreferencesOpen(true),
      closePreferences: () => setIsPreferencesOpen(false),
    }),
    [isPreferencesOpen, preferences],
  );

  return (
    <CookieConsentContext.Provider value={value}>{children}</CookieConsentContext.Provider>
  );
}

export function useCookieConsent() {
  const context = useContext(CookieConsentContext);
  if (!context) throw new Error("useCookieConsent deve ser usado dentro de CookieConsentProvider.");
  return context;
}

export function CookieConsentBanner() {
  const {
    preferences,
    hasDecision,
    isPreferencesOpen,
    acceptAll,
    acceptEssentialOnly,
    savePreferences,
    openPreferences,
    closePreferences,
  } = useCookieConsent();
  const [analyticsEnabled, setAnalyticsEnabled] = useState(preferences?.analytics ?? false);

  useEffect(() => {
    setAnalyticsEnabled(preferences?.analytics ?? false);
  }, [preferences?.analytics, isPreferencesOpen]);

  if (!hasDecision && !isPreferencesOpen) {
    return (
      <div className="pointer-events-none fixed inset-x-0 bottom-0 z-[90] p-4">
        <div className="pointer-events-auto mx-auto max-w-5xl rounded-[28px] border border-slate-200 bg-white p-5 shadow-[0_20px_60px_rgba(15,23,42,0.16)] sm:p-6">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-3xl">
              <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-medium text-slate-600">
                <Cookie size={14} className="text-slate-700" />
                Cookies e armazenamento local
              </div>

              <h2 className="mt-3 text-xl font-semibold tracking-tight text-slate-950">
                Usamos recursos essenciais para manter login, segurança e preferências do site.
              </h2>

              <p className="mt-2 text-sm leading-6 text-slate-600">
                Você pode aceitar apenas o necessário ou permitir também cookies de medição e
                melhoria quando eles estiverem ativos. Sua escolha pode ser revisada a qualquer
                momento pelo botão de cookies.
              </p>
            </div>

            <div className="flex flex-col gap-2 sm:flex-row">
              <button
                type="button"
                onClick={acceptEssentialOnly}
                className="inline-flex h-11 items-center justify-center rounded-2xl border border-slate-200 px-4 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
              >
                Recusar opcionais
              </button>

              <button
                type="button"
                onClick={openPreferences}
                className="inline-flex h-11 items-center justify-center gap-2 rounded-2xl border border-slate-200 px-4 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
              >
                <Settings2 size={15} />
                Personalizar
              </button>

              <button
                type="button"
                onClick={acceptAll}
                className="inline-flex h-11 items-center justify-center rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
              >
                Aceitar tudo
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (isPreferencesOpen) {
    return (
      <div className="pointer-events-none fixed inset-x-0 bottom-0 z-[95] p-4">
        <div className="pointer-events-auto mx-auto max-w-4xl rounded-[28px] border border-slate-200 bg-white p-5 shadow-[0_20px_60px_rgba(15,23,42,0.18)] sm:p-6">
          <div className="flex items-start justify-between gap-4">
            <div>
              <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-medium text-slate-600">
                <ShieldCheck size={14} className="text-emerald-600" />
                Preferências de privacidade
              </div>

              <h2 className="mt-3 text-xl font-semibold tracking-tight text-slate-950">
                Escolha como o site pode usar cookies e tecnologias similares.
              </h2>

              <p className="mt-2 text-sm leading-6 text-slate-600">
                Os recursos essenciais ficam sempre ligados para manter autenticação, segurança
                e estabilidade. Itens opcionais só serão usados se você permitir.
              </p>
            </div>

            <button
              type="button"
              onClick={closePreferences}
              className="inline-flex h-10 w-10 items-center justify-center rounded-2xl border border-slate-200 text-slate-500 transition hover:border-slate-300 hover:bg-slate-50 hover:text-slate-700"
              aria-label="Fechar preferências"
            >
              <X size={16} />
            </button>
          </div>

          <div className="mt-5 grid gap-3">
            <CookieRow
              title="Essenciais"
              description="Necessários para login, segurança, roteamento técnico e registro da sua escolha de cookies."
              enabled
              locked
              icon={<ShieldCheck size={16} />}
            />

            <CookieRow
              title="Medição e melhoria"
              description="Permite uso de recursos opcionais para entender navegação e melhorar a experiência, quando essas ferramentas estiverem instaladas."
              enabled={analyticsEnabled}
              onToggle={() => setAnalyticsEnabled((current) => !current)}
              icon={<BarChart3 size={16} />}
            />
          </div>

          <div className="mt-5 flex flex-col gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={acceptEssentialOnly}
              className="inline-flex h-11 items-center justify-center rounded-2xl border border-slate-200 px-4 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
            >
              Salvar só essenciais
            </button>

            <button
              type="button"
              onClick={() => savePreferences(analyticsEnabled)}
              className="inline-flex h-11 items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
            >
              <Check size={16} />
              Salvar preferências
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={openPreferences}
      className="fixed bottom-4 right-4 z-[80] inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-[0_12px_30px_rgba(15,23,42,0.14)] transition hover:border-slate-300 hover:bg-slate-50"
    >
      <Cookie size={15} />
      Cookies
    </button>
  );
}
