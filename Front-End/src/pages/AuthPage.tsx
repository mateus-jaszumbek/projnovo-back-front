import { useMemo, useState } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import {
  ArrowRight,
  BadgeCheck,
  Building2,
  CheckCircle2,
  Circle,
  LockKeyhole,
  Mail,
  Phone,
  ShieldCheck,
  Sparkles,
  Store,
  User2,
} from "lucide-react";
import type { ChangeEvent, FormEvent, InputHTMLAttributes, ReactNode } from "react";

import heroImage from "../assets/hero.png";
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

const heroStats = [
  { label: "Atendimentos", value: "128" },
  { label: "Faturamento", value: "R$ 42k" },
  { label: "Equipe", value: "12" },
];

const heroHighlights = [
  "Ordens, vendas e estoque no mesmo fluxo",
  "Visual claro para operacao diaria",
  "Cadastro rapido com base pronta para crescer",
  "Financeiro e fiscal mais organizados",
];

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
            "h-12 w-full rounded-lg border border-emerald-200/70 bg-white/95 px-4 text-sm text-slate-900 shadow-[inset_0_1px_0_rgba(255,255,255,0.95)] outline-none transition",
            icon ? "pl-11" : "",
            "placeholder:text-slate-400 focus:border-teal-400 focus:ring-4 focus:ring-teal-100/80",
            className ?? "",
          ].join(" ")}
        />
      </div>

      {hint ? <span className="mt-2 block text-xs leading-5 text-slate-500">{hint}</span> : null}
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
    <label className="flex items-start gap-3 rounded-lg border border-emerald-200/70 bg-emerald-50/55 px-4 py-3 text-sm leading-6 text-slate-600 transition hover:border-emerald-300">
      <input
        type="checkbox"
        name={name}
        checked={checked}
        required={required}
        onChange={onChange}
        className="mt-1 h-4 w-4 rounded border-slate-300 text-teal-600 focus:ring-teal-400"
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
        throw new Error("Informe um CNPJ valido para a empresa.");
      }

      if (!isStrongPassword(String(registerForm.senha ?? ""))) {
        throw new Error(
          "A senha deve ter ao menos 7 caracteres, letra maiuscula, letra minuscula e numero.",
        );
      }

      if (registerForm.senha !== registerForm.confirmarSenha) {
        throw new Error("A confirmacao de senha deve ser igual a senha.");
      }

      if (!registerForm.aceitouTermosUso) {
        throw new Error("E obrigatorio aceitar os Termos de Uso para criar a conta.");
      }

      if (!registerForm.aceitouPoliticaPrivacidade) {
        throw new Error("E obrigatorio aceitar a Politica de Privacidade e LGPD para criar a conta.");
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

  return (
    <main className="min-h-screen text-slate-900">
      <div className="grid min-h-screen lg:grid-cols-[1.08fr_0.92fr]">
        <section className="relative hidden overflow-hidden lg:flex">
          <img
            src={heroImage}
            alt="Ambiente de trabalho da assistencia tecnica"
            className="absolute inset-0 h-full w-full object-cover"
          />
          <div className="absolute inset-0 bg-[linear-gradient(135deg,rgba(15,23,42,0.84),rgba(13,148,136,0.48),rgba(249,115,22,0.26))]" />
          <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(15,23,42,0.12),rgba(15,23,42,0.78))]" />

          <div className="relative z-10 flex w-full flex-col justify-between p-10 xl:p-14">
            <div className="inline-flex w-fit items-center gap-3 rounded-lg border border-white/15 bg-white/10 px-4 py-2 text-sm text-white/90 backdrop-blur-md">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-white text-slate-900 shadow-sm">
                <Store size={18} />
              </div>
              <div className="min-w-0">
                <strong className="block text-sm font-semibold">Servicos App</strong>
                <span className="block text-xs text-white/70">Operacao centralizada para a loja</span>
              </div>
            </div>

            <div className="max-w-xl">
              <div className="inline-flex items-center gap-2 rounded-md border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-semibold text-white/90 backdrop-blur-md">
                <Sparkles size={14} />
                Nova experiencia para atendimento, vendas e financeiro
              </div>

              <h1 className="mt-6 text-4xl font-semibold tracking-tight text-white xl:text-5xl">
                Um acesso com mais cara de produto e menos cara de template.
              </h1>

              <p className="mt-5 max-w-lg text-base leading-7 text-white/78 xl:text-lg">
                Organize OS, vendas, caixa, equipe e estoque em um painel mais leve,
                claro e pronto para acompanhar o ritmo da sua operacao.
              </p>

              <div className="mt-8 grid gap-3 sm:grid-cols-2">
                {heroHighlights.map((item) => (
                  <div
                    key={item}
                    className="rounded-lg border border-white/14 bg-white/10 px-4 py-3 text-sm text-white/88 backdrop-blur-md"
                  >
                    {item}
                  </div>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4">
              {heroStats.map((item) => (
                <div
                  key={item.label}
                  className="rounded-lg border border-white/14 bg-white/10 p-4 backdrop-blur-md"
                >
                  <span className="block text-[11px] uppercase tracking-[0.14em] text-white/60">
                    {item.label}
                  </span>
                  <strong className="mt-2 block text-2xl font-semibold text-white">
                    {item.value}
                  </strong>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="flex min-h-screen items-center justify-center px-4 py-8 sm:px-6 lg:px-8">
          <div className="w-full max-w-xl">
            <div className="mb-6 text-center lg:hidden">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-lg border border-emerald-200/70 bg-white shadow-sm">
                <Store size={22} className="text-teal-700" />
              </div>
              <h1 className="mt-4 text-3xl font-semibold tracking-tight text-slate-950">
                Servicos App
              </h1>
              <p className="mt-2 text-sm leading-6 text-slate-500">
                Atendimento, vendas, estoque e financeiro no mesmo fluxo.
              </p>
            </div>

            <div className="app-panel overflow-hidden p-5 sm:p-7">
              <div className="mb-6 flex items-start justify-between gap-4 border-b border-emerald-100/80 pb-5">
                <div className="min-w-0">
                  <div className="app-chip">
                    <ShieldCheck size={14} className="text-emerald-600" />
                    Ambiente seguro
                  </div>

                  <h2 className="mt-4 text-3xl font-semibold tracking-tight text-slate-950">
                    {mode === "login" ? "Entrar na plataforma" : "Criar sua loja"}
                  </h2>

                  <p className="mt-2 text-sm leading-6 text-slate-600">
                    {mode === "login"
                      ? "Acesse sua operacao e continue exatamente de onde parou."
                      : "Cadastre a empresa e crie o primeiro usuario proprietario em um unico fluxo."}
                  </p>
                </div>

                <div className="hidden rounded-lg border border-emerald-100/80 bg-emerald-50/70 px-3 py-2 text-xs font-medium text-emerald-700 sm:flex sm:items-center sm:gap-2">
                  <BadgeCheck size={14} />
                  Dados protegidos
                </div>
              </div>

              <div
                className="mb-6 grid grid-cols-2 rounded-lg border border-emerald-100/80 bg-emerald-50/55 p-1.5"
                role="tablist"
                aria-label="Acesso"
              >
                <button
                  className={[
                    "rounded-md px-4 py-2.5 text-sm font-medium transition",
                    mode === "login"
                      ? "bg-white text-slate-950 shadow-sm"
                      : "text-slate-500 hover:text-slate-950",
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
                    "rounded-md px-4 py-2.5 text-sm font-medium transition",
                    mode === "register"
                      ? "bg-white text-slate-950 shadow-sm"
                      : "text-slate-500 hover:text-slate-950",
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
                <div className="mb-5 rounded-lg border border-rose-200/80 bg-rose-50 px-4 py-3 text-sm text-rose-700">
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
                    className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-[linear-gradient(135deg,#0f766e,#0d9488)] px-4 text-sm font-semibold text-white shadow-[0_16px_32px_rgba(13,148,136,0.26)] transition hover:brightness-105 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {loading ? "Entrando..." : "Entrar"}
                    {!loading ? <ArrowRight size={16} /> : null}
                  </button>
                </form>
              ) : (
                <form className="space-y-4" onSubmit={submitRegister}>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field
                      label="Razao social"
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
                      label="Nome do proprietario"
                      name="nomeUsuario"
                      value={String(registerForm.nomeUsuario ?? "")}
                      required
                      maxLength={150}
                      onChange={updateRegister}
                      icon={<User2 size={16} />}
                      placeholder="Seu nome"
                    />

                    <Field
                      label="E-mail do proprietario"
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

                  <div className="rounded-lg border border-emerald-100/80 bg-emerald-50/55 p-4">
                    <span className="mb-3 block text-sm font-medium text-slate-700">
                      Qualidade da senha
                    </span>

                    <div className="grid gap-2 sm:grid-cols-2">
                      <PasswordRule ok={passwordChecks.min}>Ao menos 7 caracteres</PasswordRule>
                      <PasswordRule ok={passwordChecks.upper}>Uma letra maiuscula</PasswordRule>
                      <PasswordRule ok={passwordChecks.lower}>Uma letra minuscula</PasswordRule>
                      <PasswordRule ok={passwordChecks.number}>Um numero</PasswordRule>
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
                        Politica de Privacidade e LGPD
                      </Link>
                      , incluindo o tratamento de dados necessario para criacao da conta e uso da
                      plataforma.
                    </ConsentField>
                  </div>

                  <p className="text-xs leading-6 text-slate-500">
                    A politica de cookies fica disponivel a qualquer momento no site para consulta e
                    personalizacao.
                  </p>

                  <button
                    type="submit"
                    disabled={loading}
                    className="inline-flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-[linear-gradient(135deg,#0f766e,#0d9488)] px-4 text-sm font-semibold text-white shadow-[0_16px_32px_rgba(13,148,136,0.26)] transition hover:brightness-105 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {loading ? "Criando..." : "Criar loja"}
                    {!loading ? <ArrowRight size={16} /> : null}
                  </button>
                </form>
              )}
            </div>

            <div className="mt-5 rounded-lg border border-white/70 bg-white/75 px-4 py-3 shadow-sm backdrop-blur">
              <LegalLinks className="text-slate-500" />
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
