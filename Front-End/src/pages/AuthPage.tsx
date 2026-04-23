import { useMemo, useState } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import {
  ArrowRight,
  Building2,
  CheckCircle2,
  Circle,
  LockKeyhole,
  Mail,
  Phone,
  ShieldCheck,
  Store,
  User2,
} from "lucide-react";
import type { ChangeEvent, FormEvent, InputHTMLAttributes, ReactNode } from "react";

import { useAuth } from "../auth/AuthContext";
import { LegalLinks } from "../components/LegalLinks";
import { errorMessage, isStrongPassword, isValidCnpj } from "../components/uiHelpers";

const loginInitial = {
  email: "",
  senha: "",
};

const registerInitial = {
  razaoSocial: "",
  nomeFantasia: "",
  cnpj: "",
  emailEmpresa: "",
  telefoneEmpresa: "",
  nomeUsuario: "",
  emailUsuario: "",
  senha: "",
  confirmarSenha: "",
  aceitouTermosUso: false,
  aceitouPoliticaPrivacidade: false,
};

function onlyDigits(value: string) {
  return value.replace(/\D/g, "");
}

function formatCnpj(value: string) {
  const digits = onlyDigits(value).slice(0, 14);

  return digits
    .replace(/^(\d{2})(\d)/, "$1.$2")
    .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/\.(\d{3})(\d)/, ".$1/$2")
    .replace(/(\d{4})(\d)/, "$1-$2");
}

function formatPhone(value: string) {
  const digits = onlyDigits(value).slice(0, 11);

  if (digits.length <= 10) {
    return digits
      .replace(/^(\d{2})(\d)/, "($1) $2")
      .replace(/(\d{4})(\d)/, "$1-$2");
  }

  return digits
    .replace(/^(\d{2})(\d)/, "($1) $2")
    .replace(/(\d{5})(\d)/, "$1-$2");
}

type FieldProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  icon?: ReactNode;
  hint?: string;
};

function Field({ label, icon, hint, className, ...props }: FieldProps) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-slate-700">{label}</span>

      <div className="relative">
        {icon ? (
          <span className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4 text-slate-400">
            {icon}
          </span>
        ) : null}

        <input
          {...props}
          className={[
            "h-12 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition",
            icon ? "pl-11" : "",
            "placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60",
            className ?? "",
          ].join(" ")}
        />
      </div>

      {hint ? <span className="mt-2 block text-xs text-slate-500">{hint}</span> : null}
    </label>
  );
}

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

type ConsentFieldProps = {
  name: "aceitouTermosUso" | "aceitouPoliticaPrivacidade";
  checked: boolean;
  required?: boolean;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  children: ReactNode;
};

function ConsentField({ name, checked, required, onChange, children }: ConsentFieldProps) {
  return (
    <label className="flex items-start gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm leading-6 text-slate-600 transition hover:border-slate-300">
      <input
        type="checkbox"
        name={name}
        checked={checked}
        required={required}
        onChange={onChange}
        className="mt-1 h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-400"
      />
      <span>{children}</span>
    </label>
  );
}

export function AuthPage() {
  const { entrar, registrar, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const [mode, setMode] = useState<"login" | "register">("login");
  const [loginForm, setLoginForm] = useState(loginInitial);
  const [registerForm, setRegisterForm] = useState(registerInitial);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState("");

  if (isAuthenticated) return <Navigate to="/" replace />;

  function updateLogin(event: ChangeEvent<HTMLInputElement>) {
    setLoginForm((current) => ({
      ...current,
      [event.target.name]: event.target.value,
    }));
  }

  function updateRegister(event: ChangeEvent<HTMLInputElement>) {
    const { name, value, type, checked } = event.target;

    setRegisterForm((current) => ({
      ...current,
      [name]:
        type === "checkbox"
          ? checked
          : name === "cnpj"
            ? formatCnpj(value)
            : name === "telefoneEmpresa"
              ? formatPhone(value)
              : value,
    }));
  }

  async function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setFailure("");

try {
    await entrar(loginForm.email, loginForm.senha);
    navigate("/", { replace: true });
  } catch (err) {
    setFailure(errorMessage(err));
  } finally {
    setLoading(false);
  }
  }

  async function submitRegister(event: FormEvent<HTMLFormElement>) {
  event.preventDefault();
  setLoading(true);
  setFailure("");

  try {
    if (!isValidCnpj(String(registerForm.cnpj ?? ""))) {
      throw new Error("Informe um CNPJ válido para a empresa.");
    }

    if (!isStrongPassword(String(registerForm.senha ?? ""))) {
      throw new Error(
        "A senha deve ter ao menos 7 caracteres, letra maiúscula, letra minúscula e número.",
      );
    }

    if (registerForm.senha !== registerForm.confirmarSenha) {
      throw new Error("A confirmação de senha deve ser igual à senha.");
    }

    if (!registerForm.aceitouTermosUso) {
      throw new Error("É obrigatório aceitar os Termos de Uso para criar a conta.");
    }

    if (!registerForm.aceitouPoliticaPrivacidade) {
      throw new Error("É obrigatório aceitar a Política de Privacidade e LGPD para criar a conta.");
    }

    const { confirmarSenha, ...payload } = registerForm;
    void confirmarSenha;

    await registrar({
      ...payload,
      cnpj: String(registerForm.cnpj ?? "").replace(/\D/g, ""),
      telefoneEmpresa: String(registerForm.telefoneEmpresa ?? "").replace(/\D/g, ""),
    });

    navigate("/", { replace: true });
  } catch (err) {
    setFailure(errorMessage(err));
  } finally {
    setLoading(false);
  }
}

  const passwordChecks = useMemo(() => {
    const senha = String(registerForm.senha ?? "");

    return {
      min: senha.length >= 7,
      upper: /[A-Z]/.test(senha),
      lower: /[a-z]/.test(senha),
      number: /\d/.test(senha),
      match:
        senha.length > 0 &&
        String(registerForm.confirmarSenha ?? "").length > 0 &&
        senha === String(registerForm.confirmarSenha ?? ""),
    };
  }, [registerForm.senha, registerForm.confirmarSenha]);

  return (
    <main className="min-h-screen bg-slate-950 text-slate-900">
      <div className="grid min-h-screen lg:grid-cols-[1.15fr_0.85fr]">
        <section className="relative hidden overflow-hidden lg:flex">
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(59,130,246,0.35),transparent_30%),radial-gradient(circle_at_80%_20%,rgba(168,85,247,0.24),transparent_30%),linear-gradient(135deg,#020617,#0f172a,#111827)]" />
          <div className="absolute inset-0 bg-[linear-gradient(to_bottom,transparent,rgba(2,6,23,0.5))]" />

          <div className="relative z-10 flex w-full flex-col justify-between p-10 xl:p-14">
            <div className="inline-flex w-fit items-center gap-3 rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-white/90 backdrop-blur">
              <div className="flex h-9 w-9 items-center justify-center rounded-2xl bg-white text-slate-900 shadow-sm">
                <Store size={18} />
              </div>
              <span className="font-medium">Serviços App</span>
            </div>

            <div className="max-w-xl">
              <div className="inline-flex items-center gap-2 rounded-full border border-emerald-400/20 bg-emerald-400/10 px-3 py-1 text-xs font-medium text-emerald-200">
                <ShieldCheck size={14} />
                Plataforma segura para operação diária
              </div>

              <h1 className="mt-6 text-4xl font-bold tracking-tight text-white xl:text-5xl">
                Controle sua loja em um painel bonito, rápido e fácil de visualizar.
              </h1>

              <p className="mt-5 text-base leading-7 text-slate-300 xl:text-lg">
                Organize ordens de serviço, vendas, estoque, financeiro e fiscal no
                mesmo fluxo, com uma experiência moderna para o time inteiro.
              </p>

              <div className="mt-8 grid gap-3 sm:grid-cols-2">
                {[
                  "Ordens de serviço e vendas em um só lugar",
                  "Visão clara do caixa, estoque e equipe",
                  "Fluxo moderno para operação diária",
                  "Base pronta para evoluir para kanban premium",
                ].map((item) => (
                  <div
                    key={item}
                    className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-200 backdrop-blur"
                  >
                    {item}
                  </div>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4">
              {[
                { label: "OS abertas", value: "128" },
                { label: "Vendas no mês", value: "R$ 42k" },
                { label: "Equipe ativa", value: "12" },
              ].map((item) => (
                <div
                  key={item.label}
                  className="rounded-3xl border border-white/10 bg-white/5 p-4 backdrop-blur"
                >
                  <span className="block text-xs uppercase tracking-[0.14em] text-slate-400">
                    {item.label}
                  </span>
                  <strong className="mt-2 block text-2xl font-bold text-white">
                    {item.value}
                  </strong>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-8 sm:px-6 lg:px-8">
          <div className="w-full max-w-xl">
            <div className="mb-6 text-center lg:hidden">
              <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-3xl bg-slate-900 text-white shadow-lg">
                <Store size={22} />
              </div>
              <h1 className="mt-4 text-2xl font-bold tracking-tight text-slate-900">
                Serviços App
              </h1>
              <p className="mt-2 text-sm text-slate-500">
                Caixa, estoque, ordens de serviço, vendas e notas no mesmo fluxo.
              </p>
            </div>

            <div className="rounded-[28px] border border-slate-200 bg-white p-5 shadow-[0_20px_60px_rgba(15,23,42,0.10)] sm:p-7">
              <div className="mb-6 flex items-center justify-between gap-3">
                <div>
                  <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-medium text-slate-600">
                    <ShieldCheck size={14} className="text-emerald-600" />
                    Ambiente seguro
                  </div>
                  <h2 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
                    {mode === "login" ? "Entrar na plataforma" : "Criar sua loja"}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    {mode === "login"
                      ? "Acesse sua operação e continue de onde parou."
                      : "Cadastre a empresa e crie o primeiro usuário proprietário."}
                  </p>
                </div>
              </div>

              <div
                className="mb-6 grid grid-cols-2 rounded-2xl bg-slate-100 p-1"
                role="tablist"
                aria-label="Acesso"
              >
                <button
                  className={[
                    "rounded-xl px-4 py-2.5 text-sm font-medium transition",
                    mode === "login"
                      ? "bg-white text-slate-900 shadow-sm"
                      : "text-slate-500 hover:text-slate-900",
                  ].join(" ")}
                  type="button"
                  onClick={() => {
                    setMode("login");
                    setFailure("");
                  }}
                >
                  Entrar
                </button>

                <button
                  className={[
                    "rounded-xl px-4 py-2.5 text-sm font-medium transition",
                    mode === "register"
                      ? "bg-white text-slate-900 shadow-sm"
                      : "text-slate-500 hover:text-slate-900",
                  ].join(" ")}
                  type="button"
                  onClick={() => {
                    setMode("register");
                    setFailure("");
                  }}
                >
                  Criar loja
                </button>
              </div>

              {failure ? (
                <div className="mb-5 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                  {failure}
                </div>
              ) : null}

              {mode === "login" ? (
                <form className="space-y-4" onSubmit={submitLogin}>
                  <Field
                    label="E-mail"
                    name="email"
                    type="email"
                    autoComplete="email"
                    value={loginForm.email}
                    required
                    maxLength={150}
                    onChange={updateLogin}
                    icon={<Mail size={16} />}
                    placeholder="voce@empresa.com.br"
                  />

                  <Field
                    label="Senha"
                    name="senha"
                    type="password"
                    autoComplete="current-password"
                    value={loginForm.senha}
                    required
                    minLength={7}
                    onChange={updateLogin}
                    icon={<LockKeyhole size={16} />}
                    placeholder="Digite sua senha"
                  />

                  <button
                    type="submit"
                    disabled={loading}
                    className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {loading ? "Entrando..." : "Entrar"}
                    {!loading ? <ArrowRight size={16} /> : null}
                  </button>
                </form>
              ) : (
                <form className="space-y-4" onSubmit={submitRegister}>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field
                      label="Razão social"
                      name="razaoSocial"
                      value={String(registerForm.razaoSocial ?? "")}
                      required
                      maxLength={150}
                      onChange={updateRegister}
                      icon={<Building2 size={16} />}
                      placeholder="Empresa LTDA"
                    />

                    <Field
                      label="Nome fantasia"
                      name="nomeFantasia"
                      value={String(registerForm.nomeFantasia ?? "")}
                      required
                      maxLength={150}
                      onChange={updateRegister}
                      icon={<Store size={16} />}
                      placeholder="Minha loja"
                    />
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field
                      label="CNPJ"
                      name="cnpj"
                      value={String(registerForm.cnpj ?? "")}
                      required
                      minLength={18}
                      maxLength={18}
                      onChange={updateRegister}
                      icon={<Building2 size={16} />}
                      placeholder="00.000.000/0000-00"
                    />

                    <Field
                      label="Telefone"
                      name="telefoneEmpresa"
                      value={String(registerForm.telefoneEmpresa ?? "")}
                      maxLength={15}
                      onChange={updateRegister}
                      icon={<Phone size={16} />}
                      placeholder="(41) 99999-9999"
                    />
                  </div>

                  <Field
                    label="E-mail da empresa"
                    name="emailEmpresa"
                    type="email"
                    value={String(registerForm.emailEmpresa ?? "")}
                    maxLength={150}
                    onChange={updateRegister}
                    icon={<Mail size={16} />}
                    placeholder="contato@empresa.com.br"
                  />

                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field
                      label="Nome do proprietário"
                      name="nomeUsuario"
                      value={String(registerForm.nomeUsuario ?? "")}
                      required
                      maxLength={150}
                      onChange={updateRegister}
                      icon={<User2 size={16} />}
                      placeholder="Seu nome"
                    />

                    <Field
                      label="E-mail do proprietário"
                      name="emailUsuario"
                      type="email"
                      value={String(registerForm.emailUsuario ?? "")}
                      required
                      maxLength={150}
                      onChange={updateRegister}
                      icon={<Mail size={16} />}
                      placeholder="voce@empresa.com.br"
                    />
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field
                      label="Senha"
                      name="senha"
                      type="password"
                      value={String(registerForm.senha ?? "")}
                      required
                      minLength={7}
                      onChange={updateRegister}
                      icon={<LockKeyhole size={16} />}
                      placeholder="Crie uma senha forte"
                    />

                    <Field
                      label="Confirmar senha"
                      name="confirmarSenha"
                      type="password"
                      value={String(registerForm.confirmarSenha ?? "")}
                      required
                      minLength={7}
                      onChange={updateRegister}
                      icon={<LockKeyhole size={16} />}
                      placeholder="Repita a senha"
                    />
                  </div>

                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <span className="mb-3 block text-sm font-medium text-slate-700">
                      Qualidade da senha
                    </span>

                    <div className="grid gap-2 sm:grid-cols-2">
                      <PasswordRule ok={passwordChecks.min}>Ao menos 7 caracteres</PasswordRule>
                      <PasswordRule ok={passwordChecks.upper}>Uma letra maiúscula</PasswordRule>
                      <PasswordRule ok={passwordChecks.lower}>Uma letra minúscula</PasswordRule>
                      <PasswordRule ok={passwordChecks.number}>Um número</PasswordRule>
                      <PasswordRule ok={passwordChecks.match}>As senhas coincidem</PasswordRule>
                    </div>
                  </div>

                  <div className="space-y-3">
                    <ConsentField
                      name="aceitouTermosUso"
                      checked={registerForm.aceitouTermosUso}
                      required
                      onChange={updateRegister}
                    >
                      Li e aceito os{" "}
                      <Link
                        to="/termos-de-uso"
                        target="_blank"
                        rel="noreferrer"
                        className="font-semibold text-slate-900 underline underline-offset-2"
                      >
                        Termos de Uso
                      </Link>
                      .
                    </ConsentField>

                    <ConsentField
                      name="aceitouPoliticaPrivacidade"
                      checked={registerForm.aceitouPoliticaPrivacidade}
                      required
                      onChange={updateRegister}
                    >
                      Li e aceito a{" "}
                      <Link
                        to="/privacidade-lgpd"
                        target="_blank"
                        rel="noreferrer"
                        className="font-semibold text-slate-900 underline underline-offset-2"
                      >
                        Política de Privacidade e LGPD
                      </Link>
                      , incluindo o tratamento de dados necessário para criação da conta e uso
                      da plataforma.
                    </ConsentField>
                  </div>

                  <p className="text-xs leading-6 text-slate-500">
                    A política de cookies fica disponível a qualquer momento no site para
                    consulta e personalização.
                  </p>

                  <button
                    type="submit"
                    disabled={loading}
                    className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {loading ? "Criando..." : "Criar loja"}
                    {!loading ? <ArrowRight size={16} /> : null}
                  </button>
                </form>
              )}
            </div>

            <div className="mt-5">
              <LegalLinks />
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
