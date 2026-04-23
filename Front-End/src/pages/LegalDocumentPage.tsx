import { ArrowLeft, ExternalLink, ShieldCheck } from "lucide-react";
import { Link } from "react-router-dom";
import { LegalLinks } from "../components/LegalLinks";
import { LEGAL_CONFIG } from "../legal/legalConfig";
import { documentVersionBadge, getLegalDocument } from "../legal/legalDocuments";
import type { LegalDocumentKey } from "../legal/legalDocuments";

export function LegalDocumentPage({ documentKey }: { documentKey: LegalDocumentKey }) {
  const document = getLegalDocument(documentKey);

  return (
    <main className="min-h-screen bg-slate-100 px-4 py-8 text-slate-900 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-5xl">
        <div className="rounded-[32px] border border-slate-200 bg-white p-5 shadow-[0_20px_60px_rgba(15,23,42,0.10)] sm:p-8">
          <div className="flex flex-col gap-5 border-b border-slate-200 pb-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <Link
                to="/entrar"
                className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-100"
              >
                <ArrowLeft size={15} />
                Voltar para acesso
              </Link>

              <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-xs font-medium uppercase tracking-[0.12em] text-slate-500">
                <ShieldCheck size={14} className="text-emerald-600" />
                {documentVersionBadge(document.version)}
              </div>
            </div>

            <div>
              <h1 className="text-3xl font-bold tracking-tight text-slate-950 sm:text-4xl">
                {document.title}
              </h1>
              <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-600 sm:text-base">
                {document.summary}
              </p>
            </div>
          </div>

          <div className="mt-6 rounded-[28px] border border-slate-200 bg-slate-50 p-5">
            <div className="flex flex-col gap-2 text-sm text-slate-600">
              <span>
                <strong className="font-semibold text-slate-900">Controlador informado:</strong>{" "}
                {LEGAL_CONFIG.controllerName}
              </span>
              <span>
                <strong className="font-semibold text-slate-900">Contato principal:</strong>{" "}
                {LEGAL_CONFIG.supportEmail}
              </span>
              <span>
                <strong className="font-semibold text-slate-900">Contato privacidade:</strong>{" "}
                {LEGAL_CONFIG.privacyEmail}
              </span>
              <span>
                <strong className="font-semibold text-slate-900">Site:</strong>{" "}
                <a
                  href={LEGAL_CONFIG.appUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1 text-slate-700 underline underline-offset-2"
                >
                  {LEGAL_CONFIG.appUrl}
                  <ExternalLink size={14} />
                </a>
              </span>
            </div>
          </div>

          <div className="mt-8 space-y-8">
            {document.sections.map((section) => (
              <section key={section.title} className="space-y-3">
                <h2 className="text-xl font-semibold tracking-tight text-slate-950">
                  {section.title}
                </h2>
                <div className="space-y-3 text-sm leading-7 text-slate-700 sm:text-base">
                  {section.content}
                </div>
              </section>
            ))}
          </div>

          <div className="mt-10 border-t border-slate-200 pt-6">
            <LegalLinks align="left" />
          </div>
        </div>
      </div>
    </main>
  );
}
