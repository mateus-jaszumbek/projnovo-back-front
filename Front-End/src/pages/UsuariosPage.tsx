import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Building2,
  RefreshCw,
  Shield,
  UserRound,
  Users,
} from "lucide-react";

import { CrudPage } from "../components/CrudPage";
import { Notice, PageFrame } from "../components/Ui";
import type { FieldConfig } from "../components/Ui";
import { useAuth } from "../auth/AuthContext";
import { apiRequest } from "../lib/api";

type UsuarioOptionResponse = {
  id: string;
  nome: string;
};

type SelectOption = {
  value: string;
  label: string;
};

function StatusCard({
  icon,
  title,
  value,
  helper,
}: {
  icon: React.ReactNode;
  title: string;
  value: string;
  helper: string;
}) {
  return (
    <article className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
          {icon}
        </div>

        <div className="min-w-0">
          <span className="block text-sm text-slate-500">{title}</span>
          <strong className="mt-1 block text-2xl font-bold tracking-tight text-slate-900">
            {value}
          </strong>
          <small className="mt-1 block text-xs text-slate-400">{helper}</small>
        </div>
      </div>
    </article>
  );
}

function buttonClass() {
  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60";
}

export function UsuariosPage() {
  const { session } = useAuth();

  const [usuariosOptions, setUsuariosOptions] = useState<SelectOption[]>([]);
  const [loadingUsuarios, setLoadingUsuarios] = useState(false);
  const [erroUsuarios, setErroUsuarios] = useState("");
  const [vinculosKey, setVinculosKey] = useState(0);

  const carregarUsuarios = useCallback(async () => {
    setLoadingUsuarios(true);
    setErroUsuarios("");

    try {
      const response = await apiRequest<UsuarioOptionResponse[]>("/usuarios");

      const options = response
        .map((usuario) => ({
          value: usuario.id,
          label: usuario.nome,
        }))
        .sort((a, b) =>
          a.label.localeCompare(b.label, "pt-BR", { sensitivity: "base" }),
        );

      setUsuariosOptions(options);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Não foi possível carregar os usuários.";
      setErroUsuarios(message);
    } finally {
      setLoadingUsuarios(false);
    }
  }, []);

  useEffect(() => {
    void carregarUsuarios();
  }, [carregarUsuarios]);

  async function atualizarUsuariosEVinculos() {
    await carregarUsuarios();
    setVinculosKey((current) => current + 1);
  }

  const usuarioFields: FieldConfig[] = [
    { name: "nome", label: "Nome", required: true, maxLength: 150 },
    { name: "email", label: "E-mail", type: "email", required: true, maxLength: 150 },
    {
      name: "senha",
      label: "Senha inicial",
      type: "password",
      required: true,
      minLength: 7,
      maxLength: 100,
      strongPassword: true,
    },
    {
      name: "confirmarSenha",
      label: "Confirmar senha",
      type: "password",
      required: true,
      minLength: 7,
      maxLength: 100,
      strongPassword: true,
      sameAs: "senha",
    },
    { name: "ativo", label: "Ativo", type: "checkbox", defaultValue: true },
    { name: "isSuperAdmin", label: "Super administrador", type: "checkbox" },
  ];

  const vinculoFields: FieldConfig[] = useMemo(
    () => [
      {
        name: "usuarioId",
        label: "Usuário",
        type: "select",
        required: true,
        options: usuariosOptions,
      },
      {
        name: "perfil",
        label: "Perfil",
        type: "select",
        required: true,
        defaultValue: "atendente",
        options: [
          { value: "owner", label: "Proprietário" },
          { value: "admin", label: "Administrador" },
          { value: "gerente", label: "Gerente" },
          { value: "atendente", label: "Atendente" },
          { value: "tecnico", label: "Técnico" },
          { value: "financeiro", label: "Financeiro" },
          { value: "estoque", label: "Estoque" },
        ],
      },
      {
        name: "nivelAcesso",
        label: "Nível do que pode ver",
        type: "select",
        required: true,
        defaultValue: "2",
        options: [
          { value: "1", label: "1 - Básico" },
          { value: "2", label: "2 - Atendimento" },
          { value: "3", label: "3 - Operação" },
          { value: "4", label: "4 - Gestão" },
          { value: "5", label: "5 - Administrador" },
        ],
        helper: "Owner e Administrador sempre ficam no nível 5.",
      },
      { name: "ativo", label: "Ativo", type: "checkbox", defaultValue: true },
    ],
    [usuariosOptions],
  );

  return (
    <PageFrame
      eyebrow="Administração"
      title="Usuários e permissões"
      description="Crie acessos internos e vincule cada usuário à empresa com o perfil e nível corretos."
      actions={
        <button
          type="button"
          className={buttonClass()}
          onClick={() => void atualizarUsuariosEVinculos()}
          disabled={loadingUsuarios}
        >
          <RefreshCw size={16} />
          {loadingUsuarios ? "Atualizando..." : "Atualizar usuários"}
        </button>
      }
    >
      <div className="space-y-6">
        {!session?.empresaId && !session?.isSuperAdmin ? (
          <Notice type="error">
            Sua sessão não tem empresa vinculada para criar vínculos.
          </Notice>
        ) : null}

        {erroUsuarios ? <Notice type="error">{erroUsuarios}</Notice> : null}

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatusCard
            icon={<Users size={20} />}
            title="Usuários disponíveis"
            value={String(usuariosOptions.length)}
            helper="Base carregada para criação de vínculos"
          />
          <StatusCard
            icon={<Building2 size={20} />}
            title="Empresa vinculada"
            value={session?.empresaId ? "Sim" : "Não"}
            helper="Necessária para criar vínculo com a loja"
          />
          <StatusCard
            icon={<Shield size={20} />}
            title="Tipo de sessão"
            value={session?.isSuperAdmin ? "Super admin" : "Empresa"}
            helper="Determina o nível de gestão disponível"
          />
          <StatusCard
            icon={<UserRound size={20} />}
            title="Perfil atual"
            value={String(session?.perfil ?? "-")}
            helper="Perfil logado nesta sessão"
          />
        </div>

        <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 border-b border-slate-100 pb-5">
            <h2 className="text-xl font-bold tracking-tight text-slate-900">Usuários</h2>
            <p className="mt-1 text-sm text-slate-500">
              Cadastre as pessoas que terão acesso ao sistema.
            </p>
          </div>

          <CrudPage
            embedded
            allowEdit={false}
            title="Usuários"
            description="Cadastre acessos internos com nome, e-mail e senha inicial."
            endpoint="/usuarios"
            fields={usuarioFields}
            columns={[
              { key: "nome", label: "Nome" },
              { key: "email", label: "E-mail" },
              {
                key: "ativo",
                label: "Status",
                render: (row) => (row.ativo ? "Ativo" : "Inativo"),
              },
              {
                key: "isSuperAdmin",
                label: "Super admin",
                render: (row) => (row.isSuperAdmin ? "Sim" : "Não"),
              },
            ]}
            submitLabel="Criar usuário"
            emptyText="Nenhum usuário cadastrado."
            allowDelete
            deleteMode="inativar"
            onAfterChange={atualizarUsuariosEVinculos}
            transformPayload={(payload) => {
              const usuario = { ...payload };
              delete usuario.confirmarSenha;
              return usuario;
            }}
          />
        </section>

        <section className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-5 flex flex-col gap-4 border-b border-slate-100 pb-5 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <h2 className="text-xl font-bold tracking-tight text-slate-900">
                Vínculos com a loja
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                Defina o papel e o nível de acesso do usuário dentro desta empresa.
              </p>
            </div>

            <button
              type="button"
              className={buttonClass()}
              onClick={() => void atualizarUsuariosEVinculos()}
              disabled={loadingUsuarios}
            >
              <RefreshCw size={16} />
              {loadingUsuarios ? "Atualizando..." : "Atualizar usuários"}
            </button>
          </div>

          <CrudPage
            key={vinculosKey}
            embedded
            allowEdit
            title="Vínculos"
            description="Relacione usuários à empresa com perfil e permissão adequados."
            endpoint="/usuario-empresas"
            fields={vinculoFields}
            columns={[
              { key: "usuarioNome", label: "Usuário" },
              { key: "empresaNomeFantasia", label: "Empresa" },
              { key: "perfil", label: "Perfil" },
              { key: "nivelAcesso", label: "Nível" },
              {
                key: "ativo",
                label: "Status",
                render: (row) => (row.ativo ? "Ativo" : "Inativo"),
              },
            ]}
            submitLabel="Criar vínculo"
            emptyText="Nenhum vínculo criado."
            allowDelete
            deleteMode="inativar"
            onAfterChange={atualizarUsuariosEVinculos}
            transformPayload={(payload) => ({
              ...payload,
              nivelAcesso: Number(payload.nivelAcesso ?? 2),
              empresaId: session?.empresaId ?? payload.empresaId,
            })}
          />
        </section>
      </div>
    </PageFrame>
  );
}