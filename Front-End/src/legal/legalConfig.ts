const defaultAppUrl =
  typeof window !== "undefined"
    ? window.location.origin
    : "https://main.d3ui93ghbu9l01.amplifyapp.com";

export const LEGAL_DOCUMENT_VERSIONS = {
  terms: "2026-04-23",
  privacy: "2026-04-23",
  cookies: "2026-04-23",
} as const;

export const LEGAL_CONFIG = {
  appName: import.meta.env.VITE_LEGAL_APP_NAME?.trim() || "Servicos App",
  appUrl: import.meta.env.VITE_LEGAL_APP_URL?.trim() || defaultAppUrl,
  controllerName:
    import.meta.env.VITE_LEGAL_CONTROLLER_NAME?.trim() || "Servicos App",
  controllerDocument:
    import.meta.env.VITE_LEGAL_CONTROLLER_DOCUMENT?.trim() || "Documento sob atualização",
  supportEmail:
    import.meta.env.VITE_LEGAL_SUPPORT_EMAIL?.trim() || "contato@seudominio.com",
  privacyEmail:
    import.meta.env.VITE_LEGAL_PRIVACY_EMAIL?.trim() ||
    import.meta.env.VITE_LEGAL_SUPPORT_EMAIL?.trim() ||
    "privacidade@seudominio.com",
  address:
    import.meta.env.VITE_LEGAL_ADDRESS?.trim() || "Endereço comercial sob atualização",
  lastUpdatedLabel: "23 de abril de 2026",
};

export function legalVersionLabel(version: string) {
  const [year, month, day] = version.split("-");
  if (!year || !month || !day) return version;
  return `${day}/${month}/${year}`;
}
