import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  Circle,
  LockKeyhole,
  ShieldCheck,
  Store,
} from "lucide-react";
import type { ReactNode } from "react";

import { redefinirSenha } from "../lib/api";
import { errorMessage } from "../components/uiHelpers";

function PasswordRule({ ok, children }: { ok: boolean; children: ReactNode }) {
  return (
    <div className="flex items-center gap-2 text-xs">
      {ok ? (
        <CheckCircle2 size={14} className="shrink-0 text-emerald-600" />
      ) : (
        <Circle size={14} className="shrink-0 text-slate-300" />
      )}
      <span className={ok ? "text-emerald-700" : "text-slate-500"}>{children}</span>
    </div>
  );
}

export function RedefinirSenhaPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token") ?? "";

  const [novaSenha, setNovaSenha] = useState("");
  const [confirmarSenha, setConfirmarSenha] = useState("");
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState("");
  const [sucesso, setSucesso] = useState(false);

  const checks = useMemo(
    () => ({
      min: novaSenha.length >= 7,
      upper: /[A-Z]/.test(novaSenha),
      lower: /[a-z]/.test(novaSenha),
      number: /\d/.test(novaSenha),
      match: novaSenha.length > 0 && confirmarSenha.length > 0 && novaSenha === confirmarSenha,
    }),
    [novaSenha, confirmarSenha],
  );

  const senhaValida = checks.min && checks.upper && checks.lower && checks.number;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFailure("");

    if (!senhaValida) {
      setFailure("A senha deve ter ao menos 7 caracteres, letra maiúscula, letra minúscula e número.");
      return;
    }

    if (!checks.match) {
      setFailure("A confirmação de senha deve ser igual à nova senha.");
      return;
    }

    setLoading(true);

    try {
      await redefinirSenha(token, novaSenha);
      setSucesso(true);
      setTimeout(() => navigate("/entrar", { replace: true }), 2500);
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
              Redefinir senha
            </div>

            <h2 className="mt-4 text-2xl font-semibold tracking-tight text-slate-950">
              Criar nova senha
            </h2>

            <p className="mt-2 text-sm leading-6 text-slate-600">
              Escolha uma nova senha para continuar acessando sua conta.
            </p>
          </div>

          {!token ? (
            <div className="rounded-lg border border-rose-200/80 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              Link inválido ou incompleto. Solicite um novo link de redefinição.
            </div>
          ) : sucesso ? (
            <div className="flex flex-col items-center gap-3 rounded-lg border border-emerald-200/70 bg-emerald-50/70 px-4 py-6 text-center">
              <CheckCircle2 size={28} className="text-emerald-600" />
              <p className="text-sm leading-6 text-slate-700">
                Senha redefinida com sucesso. Você já pode entrar com a nova senha — vamos te
                levar para a tela de login.
              </p>
            </div>
          ) : (
            <>
              {failure ? (
                <div className="mb-5 rounded-lg border border-rose-200/80 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                  {failure}
                </div>
              ) : null}

              <form className="space-y-4" onSubmit={handleSubmit}>
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Nova senha</span>
                  <div className="relative">
                    <span className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4 text-slate-400">
                      <LockKeyhole size={16} />
                    </span>
                    <input
                      type="password"
                      required
                      minLength={7}
                      autoFocus
                      autoComplete="new-password"
                      value={novaSenha}
                      onChange={(event) => setNovaSenha(event.target.value)}
                      placeholder="Crie uma senha forte"
                      className="h-12 w-full rounded-lg border border-emerald-200/70 bg-white/95 pl-11 pr-4 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80"
                    />
                  </div>
                </label>

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-slate-700">Confirmar senha</span>
                  <div className="relative">
                    <span className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4 text-slate-400">
                      <LockKeyhole size={16} />
                    </span>
                    <input
                      type="password"
                      required
                      minLength={7}
                      autoComplete="new-password"
                      value={confirmarSenha}
                      onChange={(event) => setConfirmarSenha(event.target.value)}
                      placeholder="Repita a senha"
                      className="h-12 w-full rounded-lg border border-emerald-200/70 bg-white/95 pl-11 pr-4 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80"
                    />
                  </div>
                </label>

                <div className="rounded-lg border border-emerald-100/80 bg-emerald-50/55 p-4">
                  <span className="mb-3 block text-sm font-medium text-slate-700">
                    Qualidade da senha
                  </span>

                  <div className="grid gap-2 sm:grid-cols-2">
                    <PasswordRule ok={checks.min}>Ao menos 7 caracteres</PasswordRule>
                    <PasswordRule ok={checks.upper}>Uma letra maiúscula</PasswordRule>
                    <PasswordRule ok={checks.lower}>Uma letra minúscula</PasswordRule>
                    <PasswordRule ok={checks.number}>Um número</PasswordRule>
                    <PasswordRule ok={checks.match}>As senhas coincidem</PasswordRule>
                  </div>
                </div>

                <button
                  type="submit"
                  disabled={loading}
                  className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-[linear-gradient(135deg,#0f766e,#0d9488)] px-4 text-sm font-semibold text-white shadow-[0_16px_32px_rgba(13,148,136,0.26)] transition hover:brightness-105 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {loading ? "Salvando..." : "Redefinir senha"}
                  {!loading ? <ArrowRight size={16} /> : null}
                </button>
              </form>
            </>
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
