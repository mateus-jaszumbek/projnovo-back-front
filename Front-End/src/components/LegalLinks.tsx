import { Link } from "react-router-dom";
import { useCookieConsent } from "../legal/CookieConsent";

type LegalLinksProps = {
  className?: string;
  align?: "left" | "center";
};

export function LegalLinks({ className, align = "center" }: LegalLinksProps) {
  const { openPreferences } = useCookieConsent();

  return (
    <div
      className={[
        "flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-slate-500",
        align === "center" ? "justify-center" : "justify-start",
        className ?? "",
      ].join(" ")}
    >
      <Link className="transition hover:text-slate-900" to="/termos-de-uso">
        Termos de uso
      </Link>

      <Link className="transition hover:text-slate-900" to="/privacidade-lgpd">
        Privacidade e LGPD
      </Link>

      <Link className="transition hover:text-slate-900" to="/politica-de-cookies">
        Política de cookies
      </Link>

      <button
        type="button"
        onClick={openPreferences}
        className="cursor-pointer transition hover:text-slate-900"
      >
        Preferências de cookies
      </button>
    </div>
  );
}
