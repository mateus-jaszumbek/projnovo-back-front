import { useState } from "react";
import type { FormEvent } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, ArrowRight, Mail, MailCheck, ShieldCheck, Store } from "lucide-react";

import { esqueciSenha } from "../lib/api";
import { errorMessage } from "../components/uiHelpers";

export function EsqueciSenhaPage() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState("");
  const [enviado, setEnviado] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setFailure("");

    try {
      await esqueciSenha(email);
      setEnviado(true);
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-8 text-slate-900">
      <div className="w-full max-w-md">
        <div className="mb-6 text-center">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-lg border border-emerald-200/70 bg-white shadow-sm">
            <Store size={22} className="text-teal-700" />
          </div>
          <h1 className="mt-4 text-2xl font-semibold tracking-tight text-slate-950">
            Servicos App
          </h1>
        </div>

        <div className="app-panel overflow-hidden p-5 sm:p-7">
          <div className="mb-6 border-b border-emerald-100/80 pb-5">
            <div className="app-chip">
              <ShieldCheck size={14} className="text-emerald-600" />
              Recuperação de senha
            </div>

            <h2 className="mt-4 text-2xl font-semibold tracking-tight text-slate-950">
              Esqueci minha senha
            </h2>

            <p className="mt-2 text-sm leading-6 text-slate-600">
              Informe o e-mail da sua conta e enviaremos um link para você criar uma nova senha.
            </p>
          </div>

          {failure ? (
            <div className="mb-5 rounded-lg border border-rose-200/80 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {failure}
            </div>
          ) : null}

          {enviado ? (
            <div className="flex flex-col items-center gap-3 rounded-lg border border-emerald-200/70 bg-emerald-50/70 px-4 py-6 text-center">
              <MailCheck size={28} className="text-emerald-600" />
              <p className="text-sm leading-6 text-slate-700">
                Se o e-mail <strong>{email}</strong> estiver cadastrado, você vai receber as
                instruções para redefinir a senha em alguns instantes. Confira também a caixa de
                spam.
              </p>
            </div>
          ) : (
            <form className="space-y-4" onSubmit={handleSubmit}>
              <label className="block">
                <span className="mb-2 block text-sm font-medium text-slate-700">E-mail</span>
                <div className="relative">
                  <span className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4 text-slate-400">
                    <Mail size={16} />
                  </span>
                  <input
                    type="email"
                    required
                    maxLength={150}
                    autoComplete="email"
                    autoFocus
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    placeholder="voce@empresa.com.br"
                    className="h-12 w-full rounded-lg border border-emerald-200/70 bg-white/95 pl-11 pr-4 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80"
                  />
                </div>
              </label>

              <button
                type="submit"
                disabled={loading}
                className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-[linear-gradient(135deg,#0f766e,#0d9488)] px-4 text-sm font-semibold text-white shadow-[0_16px_32px_rgba(13,148,136,0.26)] transition hover:brightness-105 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loading ? "Enviando..." : "Enviar link de redefinição"}
                {!loading ? <ArrowRight size={16} /> : null}
              </button>
            </form>
          )}

          <Link
            to="/entrar"
            className="mt-5 inline-flex items-center gap-2 text-sm font-medium text-slate-600 hover:text-slate-900"
          >
            <ArrowLeft size={14} />
            Voltar para o login
          </Link>
        </div>
      </div>
    </main>
  );
}
