import { Link } from "react-router-dom";
import { LifeBuoy, Mail, MessageCircle } from "lucide-react";

import type { ApiRecord } from "../../lib/api";
import { LEGAL_CONFIG } from "../../legal/legalConfig";
import { LegalLinks } from "../LegalLinks";
import {
  buildSupportWhatsAppUrl,
  formatSupportPhone,
  resolveSupportEmail,
  resolveSupportPhone,
} from "../../support/supportContact";

type SiteFooterProps = {
  company?: ApiRecord | null;
  companyName?: string;
};

export function SiteFooter({ company, companyName }: SiteFooterProps) {
  const supportEmail = resolveSupportEmail(company);
  const supportPhone = resolveSupportPhone(company);
  const supportPhoneLabel = formatSupportPhone(supportPhone);
  const whatsappUrl = buildSupportWhatsAppUrl(
    supportPhone,
    "Olá! Preciso de ajuda com o sistema.",
  );

  return (
    <footer className="border-t border-emerald-100/80 bg-white/78 backdrop-blur">
      <div className="px-4 py-5 lg:px-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="max-w-2xl">
            <strong className="block text-sm font-semibold text-slate-950">
              {companyName ?? LEGAL_CONFIG.appName}
            </strong>
            <p className="mt-1 text-sm leading-6 text-slate-600">
              Suporte, sugestões e melhorias em um só lugar.
            </p>
          </div>

          <div className="flex flex-col gap-2 text-sm text-slate-600">
            <Link
              to="/suporte"
              className="inline-flex items-center gap-2 font-medium text-slate-700 transition hover:text-slate-950"
            >
              <LifeBuoy size={16} />
              Central de suporte
            </Link>

            <a
              href={`mailto:${supportEmail}`}
              className="inline-flex items-center gap-2 transition hover:text-slate-950"
            >
              <Mail size={16} />
              {supportEmail}
            </a>

            {whatsappUrl ? (
              <a
                href={whatsappUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-2 transition hover:text-slate-950"
              >
                <MessageCircle size={16} />
                {supportPhoneLabel}
              </a>
            ) : (
              <span className="inline-flex items-center gap-2 text-slate-500">
                <MessageCircle size={16} />
                WhatsApp do suporte em atualização
              </span>
            )}
          </div>
        </div>

        <div className="mt-4 flex flex-col gap-3 border-t border-emerald-100/80 pt-4 md:flex-row md:items-center md:justify-between">
          <span className="text-xs text-slate-500">
            Atendimento pronto para receber dúvidas e ideias novas.
          </span>

          <LegalLinks align="left" />
        </div>
      </div>
    </footer>
  );
}
