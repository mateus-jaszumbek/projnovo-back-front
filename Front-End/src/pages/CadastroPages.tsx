import { CrudPage } from "../components/CrudPage";
import type { ColumnConfig, FieldConfig } from "../components/Ui";
import { displayValue, formatCurrency, formatDate } from "../components/uiHelpers";
import { useOptions } from "../hooks/useApi";
import { useState } from "react";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";


const ativoField: FieldConfig = {
  name: "ativo",
  label: "Ativo",
  type: "checkbox",
  defaultValue: true,
};

function statusBadge(active: unknown) {
  const isActive = Boolean(active);

  return (
    <span
      className={[
        "inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
        isActive
          ? "border-emerald-200 bg-emerald-50 text-emerald-700"
          : "border-rose-200 bg-rose-50 text-rose-700",
      ].join(" ")}
    >
      {isActive ? "Ativo" : "Inativo"}
    </span>
  );
}

const ativoColumn: ColumnConfig = {
  key: "ativo",
  label: "Status",
  render: (row) => statusBadge(row.ativo),
};

function optionLabel(map: Map<string, string>, value: unknown, fallback = "-") {
  const key = String(value ?? "");
  return map.get(key) ?? fallback;
}

function tipoPessoaLabel(value: unknown) {
  return String(value ?? "") === "JURIDICA" ? "Jurídica" : "Física";
}

function garantiaLabel(value: unknown) {
  const days = Number(value ?? 0);
  if (!Number.isFinite(days) || days <= 0) return "Sem garantia";
  return `${days} dia${days === 1 ? "" : "s"}`;
}

function tempoLabel(value: unknown) {
  const minutes = Number(value ?? 0);
  if (!Number.isFinite(minutes) || minutes <= 0) return "-";
  if (minutes < 60) return `${minutes} min`;

  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return rest > 0 ? `${hours}h ${rest}min` : `${hours}h`;
}

function estoqueBadge(value: unknown, minimo: unknown) {
  const atual = Number(value ?? 0);
  const min = Number(minimo ?? 0);

  const tone =
    atual <= 0
      ? "border-rose-200 bg-rose-50 text-rose-700"
      : atual <= min
        ? "border-amber-200 bg-amber-50 text-amber-700"
        : "border-emerald-200 bg-emerald-50 text-emerald-700";

  return (
    <span className={["inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold", tone].join(" ")}>
      {displayValue(atual)}
    </span>
  );
}

export function ClientesPage() {
  const fields: FieldConfig[] = [
    { name: "nome", label: "Nome", required: true, maxLength: 150, placeholder: "Nome do cliente" },
    {
      name: "tipoPessoa",
      label: "Tipo de pessoa",
      type: "select",
      required: true,
      defaultValue: "FISICA",
      options: [
        { value: "FISICA", label: "Pessoa física" },
        { value: "JURIDICA", label: "Pessoa jurídica" },
      ],
    },
    { name: "cpfCnpj", label: "CPF/CNPJ", mask: "cpfCnpj", placeholder: "Digite o documento" },
    { name: "telefone", label: "Telefone", mask: "phone", placeholder: "(00) 00000-0000" },
    { name: "email", label: "E-mail", type: "email", maxLength: 150, placeholder: "cliente@exemplo.com" },
    { name: "cep", label: "CEP", mask: "cep", placeholder: "00000-000" },
    { name: "logradouro", label: "Logradouro", maxLength: 200, placeholder: "Rua, avenida..." },
    { name: "numero", label: "Número", maxLength: 20, placeholder: "123" },
    { name: "complemento", label: "Complemento", maxLength: 100, placeholder: "Apto, sala, bloco..." },
    { name: "bairro", label: "Bairro", maxLength: 100 },
    { name: "cidade", label: "Cidade", maxLength: 100 },
    { name: "uf", label: "UF", placeholder: "SP", mask: "uf" },
    {
      name: "observacoes",
      label: "Observações",
      type: "textarea",
      span: "full",
      maxLength: 1000,
      placeholder: "Informações adicionais sobre o cliente",
    },
    ativoField,
  ];

  return (
    <CrudPage
      eyebrow="Cadastros"
      title="Clientes"
      description="Cadastre compradores, donos de aparelhos e dados fiscais para emissão de notas."
      endpoint="/clientes"
      fields={fields}
      customModuleKey="clientes"
      customModuleName="Clientes"
      columns={[
        { key: "nome", label: "Nome" },
        {
          key: "tipoPessoa",
          label: "Tipo",
          render: (row) => tipoPessoaLabel(row.tipoPessoa),
        },
        { key: "cpfCnpj", label: "CPF/CNPJ" },
        { key: "telefone", label: "Telefone" },
        ativoColumn,
      ]}
      submitLabel="Salvar cliente"
      emptyText="Nenhum cliente cadastrado."
      allowDelete
      deleteMode="inativar"
    />
  );
}

export function AparelhosPage() {
  const clientes = useOptions("/clientes", "nome");
  const clienteMap = new Map(clientes.map((item) => [String(item.value), item.label]));
  const [imeiLoading, setImeiLoading] = useState(false);
  const [imeiMessage, setImeiMessage] = useState("");

async function consultarImei(
  form: ApiRecord,
  setField: (name: string, value: unknown) => void,
) {
  const imei = String(form.imei ?? "").replace(/\D/g, "");
  setImeiMessage("");

  if (imei.length !== 15) {
    setImeiMessage("Informe um IMEI com 15 dígitos.");
    return;
  }

  setImeiLoading(true);

  try {
    const result = await apiRequest<ApiRecord>(`/aparelhos/imei/${imei}`);

    const marca = String(
      result.marca ??
      result.brand ??
      result.Brand ??
      ""
    ).trim();

    const nomeComercial = String(
      result.nomeComercial ??
      result.modelName ??
      result["Model Name"] ??
      ""
    ).trim();

    const modelo = String(
      result.modelo ??
      result.model ??
      result.Model ??
      ""
    ).trim();

    const cor = String(
      result.cor ??
      result.color ??
      result.Color ??
      ""
    ).trim();

    const encontrado =
      result.encontrado === true ||
      Boolean(marca || nomeComercial || modelo);

    if (encontrado) {
      if (marca) setField("marca", marca === "Apple Inc" ? "Apple" : marca);
      if (nomeComercial) setField("modelo", nomeComercial);
      else if (modelo) setField("modelo", modelo);
      if (cor) setField("cor", cor);

      setImeiMessage(
        String(result.mensagem ?? "Dados do aparelho preenchidos automaticamente.")
      );
      return;
    }

    setImeiMessage(
      String(result.mensagem ?? "IMEI válido, mas sem dados detalhados.")
    );
  } catch (error) {
    setImeiMessage(
      error instanceof Error
        ? error.message
        : "Não foi possível consultar o IMEI."
    );
  } finally {
    setImeiLoading(false);
  }
}

  const fields: FieldConfig[] = [
    { name: "clienteId", label: "Cliente", type: "select", required: true, options: clientes },
    { name: "marca", label: "Marca", required: true, maxLength: 100, placeholder: "Ex.: Apple" },
    { name: "modelo", label: "Modelo", required: true, maxLength: 100, placeholder: "Ex.: iPhone 13" },
    { name: "cor", label: "Cor", maxLength: 50 },
    { name: "imei", label: "IMEI", mask: "digits", maxLength: 15, placeholder: "Somente números" },
    { name: "serialNumber", label: "Número de série", maxLength: 80 },
    { name: "senhaAparelho", label: "Senha do aparelho", maxLength: 80 },
    { name: "acessorios", label: "Acessórios", maxLength: 300, placeholder: "Capinha, carregador..." },
    {
      name: "estadoFisico",
      label: "Estado físico",
      type: "textarea",
      span: "full",
      maxLength: 1000,
      placeholder: "Descreva o estado visual do aparelho",
    },
    {
      name: "observacoes",
      label: "Observações",
      type: "textarea",
      span: "full",
      maxLength: 1000,
      placeholder: "Anotações importantes para a OS",
    },
    ativoField,
  ];

  return (
    <CrudPage
      eyebrow="Cadastros"
      title="Aparelhos"
      description="Vincule celulares aos clientes com dados técnicos úteis para atendimento e garantia."
      endpoint="/aparelhos"
      fields={fields}
      customModuleKey="aparelhos"
      customModuleName="Aparelhos"
      columns={[
        { key: "marca", label: "Marca" },
        { key: "modelo", label: "Modelo" },
        { key: "imei", label: "IMEI" },
        {
          key: "clienteId",
          label: "Cliente",
          render: (row) => optionLabel(clienteMap, row.clienteId),
        },
        ativoColumn,
      ]}
      submitLabel="Salvar aparelho"
      emptyText="Nenhum aparelho cadastrado."
      allowDelete
      formFieldActions={({ field, form, setField }) =>
        field.name === "imei" ? (
          <div className="mt-2 flex flex-col gap-2">
            <button
              type="button"
              className="inline-flex w-fit items-center justify-center rounded-2xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
              disabled={imeiLoading}
              onClick={() => void consultarImei(form, setField)}
            >
              {imeiLoading ? "Consultando..." : "Buscar dados pelo IMEI"}
            </button>
            {imeiMessage ? <small className="text-xs text-slate-500">{imeiMessage}</small> : null}
          </div>
        ) : null
      }
    />
  );
}

export function FornecedoresPage() {
  const [historicoFornecedor, setHistoricoFornecedor] = useState<{
    fornecedor: ApiRecord;
    mensagens: ApiRecord[];
    loading: boolean;
  } | null>(null);

  async function abrirHistoricoFornecedor(fornecedor: ApiRecord) {
    setHistoricoFornecedor({ fornecedor, mensagens: [], loading: true });

    try {
      const mensagens = await apiRequest<ApiRecord[]>(
        `/fornecedores/${String(fornecedor.id ?? "")}/mensagens`,
      );
      setHistoricoFornecedor({ fornecedor, mensagens, loading: false });
    } catch {
      setHistoricoFornecedor({ fornecedor, mensagens: [], loading: false });
    }
  }

  const fields: FieldConfig[] = [
    { name: "nome", label: "Fornecedor", required: true, maxLength: 150, placeholder: "Nome do fornecedor" },
    {
      name: "tipoPessoa",
      label: "Tipo de pessoa",
      type: "select",
      required: true,
      defaultValue: "JURIDICA",
      options: [
        { value: "JURIDICA", label: "Pessoa juridica" },
        { value: "FISICA", label: "Pessoa fisica" },
      ],
    },
    { name: "cpfCnpj", label: "CPF/CNPJ", mask: "cpfCnpj", placeholder: "Documento do fornecedor" },
    { name: "contato", label: "Contato", maxLength: 150, placeholder: "Pessoa responsavel" },
    { name: "telefone", label: "Telefone", mask: "phone", placeholder: "(00) 00000-0000" },
    { name: "whatsApp", label: "WhatsApp", mask: "phone", placeholder: "(00) 00000-0000" },
    { name: "email", label: "E-mail", type: "email", maxLength: 200, placeholder: "compras@fornecedor.com" },
    {
      name: "produtosFornecidos",
      label: "Produtos fornecidos",
      type: "textarea",
      span: "full",
      maxLength: 2000,
      placeholder: "Telas, baterias, conectores, acessorios...",
    },
    {
      name: "mensagemPadrao",
      label: "Mensagem padrao",
      type: "textarea",
      span: "full",
      maxLength: 2000,
      placeholder: "Olá, preciso repor os itens abaixo...",
    },
    { name: "cep", label: "CEP", mask: "cep", placeholder: "00000-000" },
    { name: "logradouro", label: "Logradouro", maxLength: 200 },
    { name: "numero", label: "Numero", maxLength: 20 },
    { name: "complemento", label: "Complemento", maxLength: 100 },
    { name: "bairro", label: "Bairro", maxLength: 100 },
    { name: "cidade", label: "Cidade", maxLength: 100 },
    { name: "uf", label: "UF", mask: "uf", placeholder: "SP" },
    {
      name: "observacoes",
      label: "Observacoes",
      type: "textarea",
      span: "full",
      maxLength: 1000,
      placeholder: "Condições, prazos, pedido minimo...",
    },
    ativoField,
  ];

  return (
    <>
    <CrudPage
      eyebrow="Cadastros"
      title="Fornecedores"
      description="Cadastre fornecedores, contatos, produtos atendidos e modelos de mensagem para reposicao."
      endpoint="/fornecedores"
      fields={fields}
      customModuleKey="fornecedores"
      customModuleName="Fornecedores"
      columns={[
        { key: "nome", label: "Fornecedor" },
        { key: "contato", label: "Contato" },
        { key: "cpfCnpj", label: "CPF/CNPJ" },
        { key: "whatsApp", label: "WhatsApp" },
        { key: "email", label: "E-mail" },
        ativoColumn,
      ]}
      submitLabel="Salvar fornecedor"
      emptyText="Nenhum fornecedor cadastrado."
      allowDelete
      deleteMode="inativar"
      rowActions={(row) => (
        <button
          className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          type="button"
          onClick={() => void abrirHistoricoFornecedor(row)}
        >
          Historico
        </button>
      )}
    />
    {historicoFornecedor ? (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
        <div className="max-h-[85vh] w-full max-w-3xl overflow-y-auto rounded-3xl border border-slate-200 bg-white p-6 shadow-2xl">
          <div className="mb-5 flex items-start justify-between gap-4">
            <div>
              <h3 className="text-xl font-bold tracking-tight text-slate-900">
                Historico de mensagens
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                {String(historicoFornecedor.fornecedor.nome ?? "Fornecedor")}
              </p>
            </div>
            <button
              type="button"
              className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-50"
              onClick={() => setHistoricoFornecedor(null)}
            >
              ×
            </button>
          </div>

          {historicoFornecedor.loading ? (
            <p className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-500">
              Carregando mensagens...
            </p>
          ) : historicoFornecedor.mensagens.length === 0 ? (
            <p className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-500">
              Nenhuma mensagem registrada para este fornecedor.
            </p>
          ) : (
            <div className="space-y-3">
              {historicoFornecedor.mensagens.map((mensagem) => (
                <article
                  key={String(mensagem.id ?? "")}
                  className="rounded-2xl border border-slate-200 bg-slate-50 p-4"
                >
                  <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                    <div>
                      <strong className="text-sm text-slate-900">
                        {String(mensagem.assunto ?? "Mensagem")}
                      </strong>
                      <p className="mt-1 text-xs text-slate-500">
                        {String(mensagem.canal ?? "-")} · {formatDate(mensagem.enviadoEm)}
                        {mensagem.pecaNome ? ` · ${String(mensagem.pecaNome)}` : ""}
                      </p>
                    </div>
                    {mensagem.quantidadeSolicitada ? (
                      <span className="rounded-full border border-slate-200 bg-white px-3 py-1 text-xs font-medium text-slate-600">
                        Qtd. {String(mensagem.quantidadeSolicitada)}
                      </span>
                    ) : null}
                  </div>
                  <p className="whitespace-pre-wrap text-sm text-slate-700">
                    {String(mensagem.mensagem ?? "")}
                  </p>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>
    ) : null}
    </>
  );
}

export function TecnicosPage() {
  const fields: FieldConfig[] = [
    { name: "nome", label: "Nome", required: true, maxLength: 150, placeholder: "Nome do técnico" },
    { name: "telefone", label: "Telefone", mask: "phone", placeholder: "(00) 00000-0000" },
    { name: "email", label: "E-mail", type: "email", maxLength: 150, placeholder: "tecnico@exemplo.com" },
    { name: "especialidade", label: "Especialidade", maxLength: 100, placeholder: "Ex.: Troca de tela" },
    {
      name: "observacoes",
      label: "Observações",
      type: "textarea",
      span: "full",
      maxLength: 500,
      placeholder: "Informações adicionais sobre o técnico",
    },
    ativoField,
  ];

  return (
    <CrudPage
      eyebrow="Cadastros"
      title="Técnicos"
      description="Organize quem executa diagnósticos, reparos, laudos e entregas."
      endpoint="/tecnicos"
      fields={fields}
      customModuleKey="tecnicos"
      customModuleName="Tecnicos"
      columns={[
        { key: "nome", label: "Nome" },
        { key: "telefone", label: "Telefone" },
        { key: "email", label: "E-mail" },
        { key: "especialidade", label: "Especialidade" },
        ativoColumn,
      ]}
      submitLabel="Salvar técnico"
      emptyText="Nenhum técnico cadastrado."
      allowDelete
      deleteMode="inativar"
    />
  );
}

export function ServicosPage() {
  const fields: FieldConfig[] = [
    { name: "nome", label: "Serviço", required: true, maxLength: 150, placeholder: "Ex.: Troca de tela" },
    {
      name: "descricao",
      label: "Descrição",
      type: "textarea",
      span: "full",
      maxLength: 500,
      placeholder: "Descreva o que está incluso neste serviço",
    },
    {
      name: "valorPadrao",
      label: "Valor padrão",
      type: "number",
      min: 0,
      step: "0.01",
      defaultValue: 0,
      mask: "money",
    },
    { name: "codigoInterno", label: "Código interno", maxLength: 50, placeholder: "Ex.: SERV-001" },
    {
      name: "tempoEstimadoMinutos",
      label: "Tempo estimado em minutos",
      type: "number",
      min: 1,
      nullable: true,
      placeholder: "Ex.: 90",
    },
    { name: "garantiaDias", label: "Garantia em dias", type: "number", min: 0, defaultValue: 0 },
    ativoField,
  ];

  return (
    <CrudPage
      eyebrow="Cadastros"
      title="Catálogo de serviços"
      description="Defina serviços recorrentes para agilizar ordens de serviço e notas de serviço."
      endpoint="/servicos-catalogo"
      fields={fields}
      customModuleKey="servicos"
      customModuleName="Servicos"
      columns={[
        { key: "nome", label: "Serviço" },
        {
          key: "valorPadrao",
          label: "Valor",
          render: (row) => formatCurrency(row.valorPadrao),
        },
        {
          key: "tempoEstimadoMinutos",
          label: "Tempo",
          render: (row) => tempoLabel(row.tempoEstimadoMinutos),
        },
        {
          key: "garantiaDias",
          label: "Garantia",
          render: (row) => garantiaLabel(row.garantiaDias),
        },
        ativoColumn,
      ]}
      submitLabel="Salvar serviço"
      emptyText="Nenhum serviço cadastrado."
      allowDelete
      deleteMode="inativar"
    />
  );
}

export function PecasPage() {
  const fornecedores = useOptions("/fornecedores", "nome");
  const [reposicaoDraft, setReposicaoDraft] = useState<{
    row: Record<string, unknown>;
    mensagem: string;
  } | null>(null);

  function mensagemReposicao(row: Record<string, unknown>) {
    const estoqueAtual = Number(row.estoqueAtual ?? 0);
    const estoqueMinimo = Number(row.estoqueMinimo ?? 0);
    const quantidade = Math.max(0, estoqueMinimo - estoqueAtual);
    const base = String(row.fornecedorMensagemPadrao ?? "").trim();
    const textoPadrao = `Olá, preciso repor ${quantidade || estoqueMinimo || 1} unidade(s) de ${String(row.nome ?? "peça")}. Estoque atual: ${estoqueAtual}. Estoque mínimo: ${estoqueMinimo}.`;
    return base ? `${base}\n\n${textoPadrao}` : textoPadrao;
  }

  function enviarReposicaoEmail(row: Record<string, unknown>) {
    const email = String(row.fornecedorEmail ?? "");
    const subject = encodeURIComponent(`Reposição - ${String(row.nome ?? "peça")}`);
    const body = encodeURIComponent(mensagemReposicao(row));
    window.location.href = `mailto:${email}?subject=${subject}&body=${body}`;
  }

  function abrirMensagemReposicao(row: Record<string, unknown>) {
    setReposicaoDraft({ row, mensagem: mensagemReposicao(row) });
  }

  function quantidadeReposicao(row: Record<string, unknown>) {
    const estoqueAtual = Number(row.estoqueAtual ?? 0);
    const estoqueMinimo = Number(row.estoqueMinimo ?? 0);
    return Math.max(0, estoqueMinimo - estoqueAtual) || estoqueMinimo || 1;
  }

  async function registrarMensagemFornecedor(canal: "WHATSAPP" | "EMAIL") {
    if (!reposicaoDraft) return;

    const fornecedorId = String(reposicaoDraft.row.fornecedorId ?? "");
    if (!fornecedorId) return;

    await apiRequest(`/fornecedores/${fornecedorId}/mensagens`, {
      method: "POST",
      body: {
        pecaId: reposicaoDraft.row.id,
        canal,
        assunto: `Reposição - ${String(reposicaoDraft.row.nome ?? "peça")}`,
        mensagem: reposicaoDraft.mensagem,
        quantidadeSolicitada: quantidadeReposicao(reposicaoDraft.row),
      },
    });
  }

  async function enviarDraftWhatsApp() {
    if (!reposicaoDraft) return;
    const telefone = String(reposicaoDraft.row.fornecedorWhatsApp ?? "").replace(/\D/g, "");
    const text = encodeURIComponent(reposicaoDraft.mensagem);
    await registrarMensagemFornecedor("WHATSAPP");
    window.open(telefone ? `https://wa.me/55${telefone}?text=${text}` : `https://wa.me/?text=${text}`, "_blank");
    setReposicaoDraft(null);
  }

  async function enviarDraftEmail() {
    if (!reposicaoDraft) return;
    const email = String(reposicaoDraft.row.fornecedorEmail ?? "");
    const subject = encodeURIComponent(`Reposição - ${String(reposicaoDraft.row.nome ?? "peça")}`);
    const body = encodeURIComponent(reposicaoDraft.mensagem);
    await registrarMensagemFornecedor("EMAIL");
    window.location.href = `mailto:${email}?subject=${subject}&body=${body}`;
    setReposicaoDraft(null);
  }

  const fields: FieldConfig[] = [
    { name: "nome", label: "Peça/produto", required: true, maxLength: 150, placeholder: "Nome da peça" },
    { name: "codigoInterno", label: "Código interno", maxLength: 50, placeholder: "Ex.: PEC-001" },
    { name: "sku", label: "SKU", maxLength: 80, placeholder: "Código de estoque" },
    { name: "categoria", label: "Categoria", maxLength: 100, placeholder: "Ex.: Display" },
    { name: "marca", label: "Marca", maxLength: 100 },
    { name: "modeloCompativel", label: "Modelo compatível", maxLength: 150, placeholder: "Ex.: iPhone 13 / 13 Pro" },
    {
      name: "descricao",
      label: "Descrição",
      type: "textarea",
      span: "full",
      maxLength: 500,
      placeholder: "Detalhes do produto",
    },
    { name: "ncm", label: "NCM", mask: "ncm", helper: "8 dígitos. Ex.: 85171231." },
    { name: "cest", label: "CEST", mask: "cest", helper: "7 dígitos quando aplicável." },
    { name: "cfopPadraoNfe", label: "CFOP NF-e", mask: "cfop", helper: "4 dígitos. Ex.: 5102." },
    { name: "cfopPadraoNfce", label: "CFOP NFC-e", mask: "cfop", helper: "4 dígitos. Ex.: 5102." },
    { name: "cstCsosn", label: "CST/CSOSN", mask: "cstCsosn", helper: "2 dígitos para CST ou 3 para CSOSN." },
    {
      name: "origemMercadoria",
      label: "Origem",
      mask: "origem",
      defaultValue: "0",
      helper: "0 nacional, 1 importada direta, 2 importada mercado interno.",
    },
    { name: "unidade", label: "Unidade", required: true, defaultValue: "UN", mask: "unit" },
    { name: "fornecedorId", label: "Fornecedor preferencial", type: "select", options: fornecedores },
    { name: "custoUnitario", label: "Custo", type: "number", min: 0, step: "0.01", defaultValue: 0, mask: "money" },
    { name: "precoVenda", label: "Preço de venda", type: "number", min: 0, step: "0.01", defaultValue: 0, mask: "money" },
    { name: "garantiaDias", label: "Garantia em dias", type: "number", min: 0, defaultValue: 0 },
    { name: "estoqueAtual", label: "Estoque atual", type: "number", min: 0, step: "0.001", defaultValue: 0 },
    { name: "estoqueMinimo", label: "Estoque mínimo", type: "number", min: 0, step: "0.001", defaultValue: 0 },
    ativoField,
  ];

  return (
    <>
    <CrudPage
      eyebrow="Cadastros"
      title="Peças e produtos"
      description="Controle estoque, preço de venda e dados fiscais usados na emissão NF-e/NFC-e."
      endpoint="/pecas"
      fields={fields}
      customModuleKey="pecas"
      customModuleName="Pecas e produtos"
      columns={[
        { key: "nome", label: "Peça" },
        { key: "sku", label: "SKU" },
        { key: "ncm", label: "NCM" },
        { key: "fornecedorNome", label: "Fornecedor" },
        {
          key: "estoqueAtual",
          label: "Estoque",
          render: (row) => estoqueBadge(row.estoqueAtual, row.estoqueMinimo),
        },
        {
          key: "precoVenda",
          label: "Preço",
          render: (row) => formatCurrency(row.precoVenda),
        },
        ativoColumn,
      ]}
      submitLabel="Salvar peça"
      emptyText="Nenhuma peça cadastrada."
      allowDelete
      deleteMode="inativar"
      rowActions={(row) => {
        const estoqueAtual = Number(row.estoqueAtual ?? 0);
        const estoqueMinimo = Number(row.estoqueMinimo ?? 0);
        const precisaRepor = estoqueMinimo > 0 && estoqueAtual <= estoqueMinimo;
        if (!precisaRepor) return null;

        return (
          <>
            <button
              className="inline-flex items-center justify-center rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-100"
              type="button"
              onClick={() => abrirMensagemReposicao(row)}
            >
              Montar mensagem
            </button>
            <button
              className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
              type="button"
              onClick={() => enviarReposicaoEmail(row)}
            >
              E-mail fornecedor
            </button>
          </>
        );
      }}
    />
    {reposicaoDraft ? (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
        <div className="w-full max-w-2xl rounded-3xl border border-slate-200 bg-white p-6 shadow-2xl">
          <div className="mb-4 flex items-start justify-between gap-4">
            <div>
              <h3 className="text-xl font-bold tracking-tight text-slate-900">
                Mensagem para fornecedor
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Revise o texto antes de enviar para {String(reposicaoDraft.row.fornecedorNome ?? "o fornecedor")}.
              </p>
            </div>
            <button
              type="button"
              className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-50"
              onClick={() => setReposicaoDraft(null)}
            >
              ×
            </button>
          </div>

          <textarea
            className="min-h-[220px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60"
            value={reposicaoDraft.mensagem}
            onChange={(event) =>
              setReposicaoDraft((current) =>
                current ? { ...current, mensagem: event.target.value } : current,
              )
            }
          />

          <div className="mt-5 flex flex-wrap justify-end gap-2">
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
              onClick={() => setReposicaoDraft(null)}
            >
              Cancelar
            </button>
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
              onClick={() => void enviarDraftEmail()}
            >
              Enviar por e-mail
            </button>
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-2xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700"
              onClick={() => void enviarDraftWhatsApp()}
            >
              Enviar por WhatsApp
            </button>
          </div>
        </div>
      </div>
    ) : null}
    </>
  );
}
