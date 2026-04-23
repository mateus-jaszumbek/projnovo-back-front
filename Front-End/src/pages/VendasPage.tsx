import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import {
  AlertTriangle,
  ArrowLeft,
  ArrowRight,
  CreditCard,
  Package,
  Plus,
  Receipt,
  Search,
  ShoppingCart,
  Trash2,
  UserRound,
  Wallet,
  X,
} from "lucide-react";

import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { DataTable, FieldRenderer, Notice } from "../components/Ui";
import type { FieldConfig, FieldType } from "../components/Ui";
import {
  defaultForm,
  errorMessage,
  formatCurrency,
  formatDate,
  onlyDigits,
  payloadFromForm,
  validateForm,
} from "../components/uiHelpers";
import { useList } from "../hooks/useApi";
import { useAuth } from "../auth/AuthContext";
import { PageHeader } from "../components/app/PageHeader";
import { PageSection } from "../components/app/PageSection";
import { StatCard } from "../components/app/StartCard";
import { EmptyState } from "../components/app/EmptyState";

const pagamentoOptions = [
  { value: "DINHEIRO", label: "Dinheiro" },
  { value: "PIX", label: "Pix" },
  { value: "CARTAO_CREDITO", label: "Cartão de crédito" },
  { value: "CARTAO_DEBITO", label: "Cartão de débito" },
  { value: "BOLETO", label: "Boleto" },
  { value: "CREDIARIO", label: "Crediário" },
];

const vendaFieldAreas = [
  { value: "Cliente e pagamento", label: "Cliente e pagamento" },
  { value: "Adicionar item", label: "Adicionar item" },
  { value: "Resumo da venda", label: "Resumo da venda" },
  { value: "Observações", label: "Observações" },
];

type CartItem = {
  key: string;
  pecaId: string;
  descricao: string;
  estoqueAtual: number;
  quantidade: number;
  valorUnitario: number;
  desconto: number;
};

type CustomField = {
  id: string;
  nome: string;
  chave: string;
  tipo: FieldType;
  obrigatorio: boolean;
  aba?: string;
  linha: number;
  posicao: number;
  ordem: number;
  placeholder?: string;
  valorPadrao?: string;
  opcoes?: string[];
  exportarExcel?: boolean;
  exportarExcelResumo?: boolean;
  exportarPdf?: boolean;
};

type CustomModule = {
  id: string;
  campos?: CustomField[];
};

type SaleStep = 1 | 2 | 3;

const customFieldTypes = [
  { value: "text", label: "Texto curto" },
  { value: "email", label: "E-mail" },
  { value: "number", label: "Número" },
  { value: "currency", label: "Valor" },
  { value: "percentage", label: "Porcentagem" },
  { value: "date", label: "Data" },
  { value: "select", label: "Lista" },
  { value: "textarea", label: "Texto longo" },
  { value: "checkbox", label: "Sim/Não" },
];

const customFieldFormFields: FieldConfig[] = [
  { name: "nome", label: "Nome do campo", required: true, maxLength: 100 },
  {
    name: "aba",
    label: "Onde aparece",
    type: "select",
    required: true,
    defaultValue: "Cliente e pagamento",
    options: vendaFieldAreas,
  },
  { name: "tipo", label: "Tipo", type: "select", required: true, options: customFieldTypes },
  { name: "obrigatorio", label: "Obrigatório", type: "checkbox" },
  { name: "exportarExcel", label: "Aparecer no Excel", type: "checkbox", defaultValue: true },
  { name: "exportarExcelResumo", label: "Aparecer no Excel resumido", type: "checkbox" },
  { name: "exportarPdf", label: "Aparecer no PDF", type: "checkbox", defaultValue: true },
  {
    name: "opcoesText",
    label: "Opções",
    type: "textarea",
    span: "full",
    helper: "Uma opção por linha.",
  },
];

function toNumber(value: unknown) {
  const number = Number(value ?? 0);
  return Number.isFinite(number) ? number : 0;
}

function cartTotal(item: CartItem) {
  return Math.max(0, item.quantidade * item.valorUnitario - item.desconto);
}

function normalizeAreaName(value: unknown) {
  const text = String(value ?? "").trim();
  const normalized = text.toLowerCase();

  if (!text) return "Cliente e pagamento";
  if (normalized.includes("observa")) return "Observações";
  if (normalized.includes("cliente")) return "Cliente e pagamento";
  if (normalized.includes("adicionar") || normalized.includes("item")) return "Adicionar item";
  if (normalized.includes("resumo")) return "Resumo da venda";

  return text;
}

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60";

const textareaClass =
  "min-h-[120px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 resize-y";

const cardClass = "rounded-3xl border border-slate-200 bg-white p-5 shadow-sm";

function SectionTitle({
  icon,
  title,
  description,
}: {
  icon: React.ReactNode;
  title: string;
  description?: string;
}) {
  return (
    <div className="mb-4 flex items-start gap-3">
      <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
        {icon}
      </div>

      <div className="min-w-0">
        <h3 className="text-base font-semibold text-slate-900">{title}</h3>
        {description ? <p className="mt-1 text-sm text-slate-500">{description}</p> : null}
      </div>
    </div>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-slate-700">{label}</span>
      {children}
    </label>
  );
}

function estoqueTone(estoque: number) {
  if (estoque <= 0) return "bg-rose-50 text-rose-700 border-rose-200";
  if (estoque <= 3) return "bg-amber-50 text-amber-700 border-amber-200";
  return "bg-emerald-50 text-emerald-700 border-emerald-200";
}

export function VendasPage() {
  const { session } = useAuth();
  const userRole = String(session?.perfil ?? "").toLowerCase();
  const canManageCustomFields = Boolean(
    session?.isSuperAdmin ||
      ["owner", "admin", "administrador", "super-admin", "superadmin"].includes(userRole),
  );

  const clientes = useList("/clientes");
  const pecas = useList("/pecas");
  const [reloadKey, setReloadKey] = useState(0);
  const vendas = useList("/vendas", reloadKey);

  const [clienteId, setClienteId] = useState("");
  const [clienteBusca, setClienteBusca] = useState("");
  const [quickClienteOpen, setQuickClienteOpen] = useState(false);
  const [quickClienteSaving, setQuickClienteSaving] = useState(false);
  const [quickCliente, setQuickCliente] = useState({
    nome: "",
    cpfCnpj: "",
    telefone: "",
    email: "",
  });
  const [formaPagamento, setFormaPagamento] = useState("DINHEIRO");
  const [descontoVenda, setDescontoVenda] = useState("0");
  const [parcelas, setParcelas] = useState("1");
  const [taxaPercentual, setTaxaPercentual] = useState("0");
  const [primeiroVencimento, setPrimeiroVencimento] = useState(
    new Date().toISOString().slice(0, 10),
  );
  const [observacoes, setObservacoes] = useState("");
  const [pecaId, setPecaId] = useState("");
  const [quantidade, setQuantidade] = useState("1");
  const [valorUnitario, setValorUnitario] = useState("");
  const [descontoItem, setDescontoItem] = useState("0");
  const [cart, setCart] = useState<CartItem[]>([]);
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");
  const [saving, setSaving] = useState(false);
  const [stockPopupOpen, setStockPopupOpen] = useState(false);
  const [stockPopupMessage, setStockPopupMessage] = useState("");
  const [customModule, setCustomModule] = useState<CustomModule | null>(null);
  const [customReloadKey, setCustomReloadKey] = useState(0);
  const [customFieldForm, setCustomFieldForm] = useState<ApiRecord>(() =>
    defaultForm(customFieldFormFields),
  );
  const [customFieldErrors, setCustomFieldErrors] = useState<Record<string, string>>({});
  const [editingCustomFieldId, setEditingCustomFieldId] = useState("");
  const [showCustomBuilder, setShowCustomBuilder] = useState(false);
  const [saleWizardOpen, setSaleWizardOpen] = useState(false);
  const [saleStep, setSaleStep] = useState<SaleStep>(1);
  const [vendaCustomForm, setVendaCustomForm] = useState<ApiRecord>({});

  function openStockPopup(message: string) {
    setStockPopupMessage(message);
    setStockPopupOpen(true);
  }

  useEffect(() => {
    let active = true;

    async function ensureModule() {
      try {
        const module = await apiRequest<CustomModule>("/modulos-personalizados/sistema", {
          method: "POST",
          body: {
            chave: "vendas",
            nome: "Vendas",
            descricao: "Campos extras de vendas",
          },
        });

        if (active) setCustomModule(module);
      } catch (err) {
        if (active) setFailure(errorMessage(err));
      }
    }

    void ensureModule();

    return () => {
      active = false;
    };
  }, [customReloadKey]);

  const customFields = useMemo(
    () =>
      [...(customModule?.campos ?? [])].sort(
        (a, b) =>
          normalizeAreaName(a.aba).localeCompare(normalizeAreaName(b.aba)) ||
          a.linha - b.linha ||
          a.posicao - b.posicao ||
          a.ordem - b.ordem,
      ),
    [customModule],
  );

  const dynamicVendaFields = useMemo<FieldConfig[]>(
    () =>
      customFields.map((field) => ({
        name: field.chave,
        label: field.nome,
        type: field.tipo,
        required: field.obrigatorio,
        placeholder: field.placeholder,
        helper: normalizeAreaName(field.aba),
        defaultValue:
          field.tipo === "checkbox" ? field.valorPadrao === "true" : field.valorPadrao ?? "",
        options: field.opcoes?.map((option) => ({ value: option, label: option })),
        line: field.linha,
        position: field.posicao,
      })),
    [customFields],
  );

  useEffect(() => {
    setVendaCustomForm((current) => ({ ...defaultForm(dynamicVendaFields), ...current }));
  }, [dynamicVendaFields]);

  function updateVendaCustomField(name: string, value: unknown) {
    setVendaCustomForm((current) => ({ ...current, [name]: value }));
  }

  const fieldsByArea = useMemo(() => {
    const map = new Map<string, FieldConfig[]>();

    customFields.forEach((field) => {
      const area = normalizeAreaName(field.aba);
      const config = dynamicVendaFields.find((item) => item.name === field.chave);
      if (!config) return;

      map.set(area, [...(map.get(area) ?? []), config]);
    });

    return map;
  }, [customFields, dynamicVendaFields]);

  function abrirCriacaoCampoVenda(area: string) {
    setEditingCustomFieldId("");
    setCustomFieldForm({
      ...defaultForm(customFieldFormFields),
      aba: area,
    });
    setCustomFieldErrors({});
    setShowCustomBuilder(true);
  }

  function renderAreaFieldControls(area: string) {
    const areaFields = customFields.filter((field) => normalizeAreaName(field.aba) === area);

    if (!canManageCustomFields && areaFields.length === 0) return null;

    return (
      <div className="mb-4 space-y-3">
        {canManageCustomFields ? (
          <button
            type="button"
            className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-700 transition hover:bg-slate-50"
            onClick={() => abrirCriacaoCampoVenda(area)}
          >
            <Plus size={14} />
            Adicionar campo
          </button>
        ) : null}

        {areaFields.length > 0 ? (
          <div className="space-y-2">
            <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
              Campos desta etapa
            </span>

            <div className="flex flex-wrap gap-2">
              {areaFields.map((field) => (
                <button
                  key={field.id}
                  type="button"
                  className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700 transition hover:bg-slate-100"
                  onClick={() => editarCampoExtra(field)}
                >
                  {field.nome}
                  <span className="rounded-full bg-white px-2 py-0.5 text-xs text-slate-500">
                    {normalizeAreaName(field.aba)}
                  </span>
                </button>
              ))}
            </div>
          </div>
        ) : null}
      </div>
    );
  }

  function renderVendaCustomFields(area: string) {
    const fields = fieldsByArea.get(area) ?? [];
    if (fields.length === 0) return null;

    return (
      <div className="mt-4 grid gap-4 md:grid-cols-2">
        {fields.map((field) => (
          <FieldRenderer
            key={field.name}
            field={field}
            value={vendaCustomForm[field.name]}
            onChange={updateVendaCustomField}
          />
        ))}
      </div>
    );
  }

  const selectedPeca = useMemo(
    () => pecas.data.find((item) => String(item.id ?? "") === pecaId),
    [pecaId, pecas.data],
  );

  const subtotal = cart.reduce((total, item) => total + cartTotal(item), 0);
  const total = Math.max(0, subtotal - toNumber(descontoVenda));
  const usaParcelas = ["CARTAO_CREDITO", "BOLETO", "CREDIARIO"].includes(formaPagamento);

  const quantidadeItens = cart.reduce((totalAtual, item) => totalAtual + item.quantidade, 0);

  const ticketMedio =
    vendas.data.length > 0
      ? vendas.data.reduce((acc, row) => acc + toNumber(row.valorTotal), 0) / vendas.data.length
      : 0;

  const estoqueSelecionado = toNumber(selectedPeca?.estoqueAtual);
  const precoSelecionado = toNumber(selectedPeca?.precoVenda);
  const selectedCliente = useMemo(
    () => clientes.data.find((item) => String(item.id ?? "") === clienteId),
    [clienteId, clientes.data],
  );
  const clienteMatches = useMemo(() => {
    const term = clienteBusca.trim().toLowerCase();
    const digits = onlyDigits(clienteBusca);

    if (!term && !digits) return [];

    return clientes.data
      .filter((cliente) => {
        const nome = String(cliente.nome ?? "").toLowerCase();
        const documento = onlyDigits(cliente.cpfCnpj);
        return nome.includes(term) || (digits.length > 0 && documento.includes(digits));
      })
      .slice(0, 6);
  }, [clienteBusca, clientes.data]);

  function selecionarCliente(cliente: ApiRecord) {
    setClienteId(String(cliente.id ?? ""));
    setClienteBusca(
      [String(cliente.nome ?? ""), String(cliente.cpfCnpj ?? "")].filter(Boolean).join(" - "),
    );
    setQuickClienteOpen(false);
  }

  function limparClienteSelecionado() {
    setClienteId("");
    setClienteBusca("");
  }

  async function criarClienteRapido() {
    setFailure("");
    setNotice("");

    const nome = quickCliente.nome.trim();
    const documento = onlyDigits(quickCliente.cpfCnpj);

    if (!nome) {
      setFailure("Informe o nome do cliente para criar o cadastro rapido.");
      return;
    }

    setQuickClienteSaving(true);

    try {
      const saved = await apiRequest<ApiRecord>("/clientes", {
        method: "POST",
        body: {
          nome,
          tipoPessoa: documento.length > 11 ? "JURIDICA" : "FISICA",
          cpfCnpj: documento || null,
          telefone: quickCliente.telefone.trim() || null,
          email: quickCliente.email.trim() || null,
        },
      });

      selecionarCliente(saved);
      setQuickCliente({ nome: "", cpfCnpj: "", telefone: "", email: "" });
      setReloadKey((key) => key + 1);
      setNotice("Cliente criado e selecionado para a venda.");
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setQuickClienteSaving(false);
    }
  }

  function gerarParcelas() {
    if (!usaParcelas) return [];

    const quantidadeParcelas = Math.max(1, Math.floor(toNumber(parcelas)));
    const baseDate = primeiroVencimento
      ? new Date(`${primeiroVencimento}T00:00:00`)
      : new Date();
    const valorBase = Math.floor((total / quantidadeParcelas) * 100) / 100;
    let acumulado = 0;

    return Array.from({ length: quantidadeParcelas }, (_, index) => {
      const vencimento = new Date(baseDate);
      vencimento.setMonth(baseDate.getMonth() + index);
      const valor =
        index === quantidadeParcelas - 1
          ? Number((total - acumulado).toFixed(2))
          : valorBase;

      acumulado += valor;

      return {
        dataVencimento: vencimento.toISOString().slice(0, 10),
        valor,
        formaPagamento,
        taxaPercentual: toNumber(taxaPercentual),
      };
    });
  }

  const previewParcelas = useMemo(
    () => gerarParcelas(),
    [formaPagamento, parcelas, taxaPercentual, primeiroVencimento, total],
  );

  function selecionarPeca(id: string) {
    setPecaId(id);
    const peca = pecas.data.find((item) => String(item.id ?? "") === id);
    setValorUnitario(peca ? String(toNumber(peca.precoVenda)) : "");

    if (peca && toNumber(peca.estoqueAtual) <= 0) {
      openStockPopup("Este produto está sem estoque.");
    }
  }

  function adicionarItem() {
    setFailure("");
    setNotice("");

    if (!selectedPeca) {
      openStockPopup("Selecione uma peça para adicionar.");
      return;
    }

    const qtd = toNumber(quantidade);
    const unitario =
      valorUnitario === "" ? toNumber(selectedPeca.precoVenda) : toNumber(valorUnitario);
    const desconto = toNumber(descontoItem);
    const estoqueAtual = toNumber(selectedPeca.estoqueAtual);

    if (qtd <= 0) {
      openStockPopup("A quantidade deve ser maior que zero.");
      return;
    }

    if (unitario < 0 || desconto < 0) {
      openStockPopup("Valor unitário e desconto não podem ser negativos.");
      return;
    }

    const quantidadeNoCarrinho = cart
      .filter((item) => item.pecaId === pecaId)
      .reduce((totalAtual, item) => totalAtual + item.quantidade, 0);

    if (estoqueAtual <= 0) {
      openStockPopup("Este produto está sem estoque.");
      return;
    }

    if (estoqueAtual < quantidadeNoCarrinho + qtd) {
      openStockPopup(
        `Estoque insuficiente. Disponível: ${estoqueAtual}. Você está tentando adicionar ${qtd}.`,
      );
      return;
    }

    setCart((current) => [
      ...current,
      {
        key: crypto.randomUUID(),
        pecaId,
        descricao: String(selectedPeca.nome ?? "Peça"),
        estoqueAtual,
        quantidade: qtd,
        valorUnitario: unitario,
        desconto,
      },
    ]);

    setQuantidade("1");
    setDescontoItem("0");
  }

  function removerItem(key: string) {
    setCart((current) => current.filter((item) => item.key !== key));
  }

  function limparVenda() {
    setClienteId("");
    setClienteBusca("");
    setQuickClienteOpen(false);
    setQuickCliente({ nome: "", cpfCnpj: "", telefone: "", email: "" });
    setFormaPagamento("DINHEIRO");
    setDescontoVenda("0");
    setParcelas("1");
    setTaxaPercentual("0");
    setPrimeiroVencimento(new Date().toISOString().slice(0, 10));
    setObservacoes("");
    setPecaId("");
    setQuantidade("1");
    setValorUnitario("");
    setDescontoItem("0");
    setCart([]);
    setVendaCustomForm(defaultForm(dynamicVendaFields));
    setShowCustomBuilder(false);
    setEditingCustomFieldId("");
    setCustomFieldForm(defaultForm(customFieldFormFields));
    setCustomFieldErrors({});
  }

  function openSaleWizard() {
    setSaleStep(1);
    setSaleWizardOpen(true);
  }

  function closeSaleWizard() {
    setSaleWizardOpen(false);
    setSaleStep(1);
    setQuickClienteOpen(false);
    setShowCustomBuilder(false);
  }

  function stepForArea(area: string): SaleStep {
    if (area === "Adicionar item") return 2;
    if (area === "Resumo da venda") return 3;
    return 1;
  }

  async function salvarVenda(finalizar: boolean) {
    setFailure("");
    setNotice("");

    if (cart.length === 0) {
      setSaleStep(2);
      setFailure("Adicione pelo menos um item antes de salvar a venda.");
      return;
    }

    if (toNumber(descontoVenda) < 0) {
      setSaleStep(1);
      setFailure("O desconto da venda não pode ser negativo.");
      return;
    }

    const customErrors = validateForm(dynamicVendaFields, vendaCustomForm);
    if (Object.keys(customErrors).length > 0) {
      const firstErrorField = dynamicVendaFields.find((field) => customErrors[field.name]);
      if (firstErrorField) {
        setSaleStep(stepForArea(normalizeAreaName(firstErrorField.helper)));
      }
      setFailure("Corrija os campos extras da venda antes de salvar.");
      return;
    }

    setSaving(true);

    try {
      const saved = await apiRequest<ApiRecord>("/vendas/com-itens", {
        method: "POST",
        body: {
          clienteId: clienteId || null,
          formaPagamento,
          desconto: toNumber(descontoVenda),
          observacoes: observacoes.trim() || null,
          finalizar,
          parcelas: gerarParcelas(),
          itens: cart.map((item) => ({
            pecaId: item.pecaId,
            quantidade: item.quantidade,
            valorUnitario: item.valorUnitario,
            desconto: item.desconto,
          })),
        },
      });

      const vendaId = String(saved.id ?? "");
      if (customModule && vendaId && dynamicVendaFields.length > 0) {
        await apiRequest(`/modulos-personalizados/${customModule.id}/registros/origem/${vendaId}`, {
          method: "PUT",
          body: { valores: payloadFromForm(dynamicVendaFields, vendaCustomForm) },
        });
        setCustomReloadKey((key) => key + 1);
      }

      limparVenda();
      setSaleWizardOpen(false);
      setSaleStep(1);
      setReloadKey((key) => key + 1);
      setNotice(finalizar ? "Venda criada e finalizada." : "Venda criada como rascunho.");
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function salvarCampoExtra(event?: FormEvent<HTMLFormElement>) {
    event?.preventDefault();
    if (!customModule || !canManageCustomFields) return;

    const validation = validateForm(customFieldFormFields, customFieldForm);
    if (Object.keys(validation).length > 0) {
      setCustomFieldErrors(validation);
      setFailure("Corrija os dados do campo extra.");
      return;
    }

    const payload = payloadFromForm(customFieldFormFields, customFieldForm);
    const area = normalizeAreaName(payload.aba);
    const index = customFields.filter((field) => normalizeAreaName(field.aba) === area).length;
    const body = {
      nome: payload.nome,
      tipo: payload.tipo,
      obrigatorio: payload.obrigatorio,
      aba: area,
      linha: Math.floor(index / 3) + 1,
      posicao: (index % 3) + 1,
      ordem: index + 1,
      placeholder: null,
      valorPadrao: null,
      opcoes: String(customFieldForm.opcoesText ?? "")
        .split(/\r?\n/)
        .map((option) => option.trim())
        .filter(Boolean),
      exportarExcel: payload.exportarExcel !== false,
      exportarExcelResumo: payload.exportarExcelResumo === true,
      exportarPdf: payload.exportarPdf !== false,
      ativo: true,
    };

    try {
      if (editingCustomFieldId) {
        const { tipo: _tipo, ...updateBody } = body;
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingCustomFieldId}`, {
          method: "PUT",
          body: updateBody,
        });
        setNotice("Campo extra atualizado. O tipo foi preservado.");
      } else {
        await apiRequest(`/modulos-personalizados/${customModule.id}/campos`, {
          method: "POST",
          body,
        });
        setNotice("Campo extra criado para vendas.");
      }

      setEditingCustomFieldId("");
      setShowCustomBuilder(false);
      setCustomFieldForm(defaultForm(customFieldFormFields));
      setCustomFieldErrors({});
      setCustomReloadKey((key) => key + 1);
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  function editarCampoExtra(field: CustomField) {
    if (!canManageCustomFields) return;

    setEditingCustomFieldId(field.id);
    setShowCustomBuilder(true);
    setCustomFieldForm({
      nome: field.nome,
      aba: normalizeAreaName(field.aba),
      tipo: field.tipo,
      obrigatorio: field.obrigatorio,
      exportarExcel: field.exportarExcel !== false,
      exportarExcelResumo: field.exportarExcelResumo === true,
      exportarPdf: field.exportarPdf !== false,
      opcoesText: (field.opcoes ?? []).join("\n"),
    });
  }

  async function excluirCampoExtra() {
    if (!customModule || !editingCustomFieldId || !canManageCustomFields) return;
    if (!window.confirm("Excluir este campo extra de vendas?")) return;

    try {
      await apiRequest(`/modulos-personalizados/${customModule.id}/campos/${editingCustomFieldId}`, {
        method: "DELETE",
      });
      setEditingCustomFieldId("");
      setShowCustomBuilder(false);
      setCustomFieldForm(defaultForm(customFieldFormFields));
      setCustomReloadKey((key) => key + 1);
      setNotice("Campo extra excluído.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }

  async function patchVenda(id: unknown, action: "finalizar" | "cancelar") {
    setFailure("");
    setNotice("");

    try {
      await apiRequest(`/vendas/${id}/${action}`, { method: "PATCH" });
      setReloadKey((key) => key + 1);
      setNotice(action === "finalizar" ? "Venda finalizada." : "Venda cancelada.");
    } catch (err) {
      setFailure(errorMessage(err));
    }
  }


  const saleSteps = [
    {
      id: 1 as SaleStep,
      title: "Cliente, pagamento e observações",
      description: "Selecione o cliente, defina o pagamento e registre observações.",
    },
    {
      id: 2 as SaleStep,
      title: "Adicionar item e carrinho",
      description: "Monte a venda com os itens e revise o carrinho.",
    },
    {
      id: 3 as SaleStep,
      title: "Resumo da venda",
      description: "Confira totais, parcelas e finalize a operação.",
    },
  ];

  const canAdvanceFromItems = cart.length > 0;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Operação"
        title="Vendas"
        description="Monte a venda em um fluxo por etapas, com menos poluição na tela e mais foco em cada fase."
      />

      <div className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <StatCard
            title="Itens no carrinho"
            value={cart.length}
            description={`${quantidadeItens} unidades`}
            icon={ShoppingCart}
          />
          <StatCard
            title="Subtotal"
            value={formatCurrency(subtotal)}
            description="Antes do desconto"
            icon={Receipt}
          />
          <StatCard
            title="Desconto"
            value={formatCurrency(descontoVenda)}
            description="Desconto da venda"
            icon={Wallet}
            tone="warning"
          />
          <StatCard
            title="Total"
            value={formatCurrency(total)}
            description="Valor final"
            icon={CreditCard}
            tone="success"
          />
          <StatCard
            title="Ticket médio"
            value={formatCurrency(ticketMedio)}
            description="Base nas vendas registradas"
            icon={ShoppingCart}
          />
        </div>

        <div className="flex justify-end">
          <button
            type="button"
            className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-800"
            onClick={openSaleWizard}
          >
            <Plus size={16} />
            Nova venda
          </button>
        </div>
      </div>

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure || vendas.error || pecas.error ? (
        <Notice type="error">{failure || vendas.error || pecas.error}</Notice>
      ) : null}

      <PageSection
        title="Vendas registradas"
        description="Visualize as últimas vendas e finalize ou cancele quando necessário."
      >
        <DataTable
          columns={[
            { key: "numeroVenda", label: "Venda" },
            { key: "clienteNome", label: "Cliente" },
            { key: "status", label: "Status" },
            { key: "formaPagamento", label: "Pagamento" },
            {
              key: "valorTotal",
              label: "Total",
              render: (row) => formatCurrency(row.valorTotal),
            },
            {
              key: "dataVenda",
              label: "Data",
              render: (row) => formatDate(row.dataVenda),
            },
          ]}
          rows={vendas.data}
          loading={vendas.loading}
          emptyText="Nenhuma venda criada."
          actions={(row: ApiRecord) => (
            <div className="flex flex-wrap gap-2">
              <button
                className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                type="button"
                onClick={() => patchVenda(row.id, "finalizar")}
              >
                Finalizar
              </button>

              <button
                className="inline-flex items-center justify-center rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-100"
                type="button"
                onClick={() => patchVenda(row.id, "cancelar")}
              >
                Cancelar
              </button>
            </div>
          )}
        />
      </PageSection>

      {saleWizardOpen ? (
        <div className="fixed inset-0 z-40 bg-slate-950/50 p-2 backdrop-blur-sm sm:p-4">
          <div className="flex h-full w-full items-stretch justify-center">
            <div className="flex h-full w-full max-w-[1600px] flex-col overflow-hidden rounded-[32px] border border-slate-200 bg-white shadow-2xl">
            <div className="border-b border-slate-200 px-6 py-5">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                  <span className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                    Nova venda
                  </span>
                  <h2 className="mt-1 text-2xl font-bold tracking-tight text-slate-900">
                    Fluxo em etapas
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Primeiro cliente e pagamento, depois itens e carrinho, e por fim o resumo da venda.
                  </p>
                </div>

                <div className="flex items-center gap-2">
                  <div className="hidden rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2 text-sm text-slate-600 sm:block">
                    <strong className="mr-1 text-slate-900">{cart.length}</strong>
                    {cart.length === 1 ? "item no carrinho" : "itens no carrinho"}
                  </div>

                  <button
                    type="button"
                    className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
                    onClick={closeSaleWizard}
                    aria-label="Fechar fluxo de venda"
                  >
                    <X size={18} />
                  </button>
                </div>
              </div>

              <div className="mt-5 grid gap-3 lg:grid-cols-3">
                {saleSteps.map((step) => {
                  const active = saleStep === step.id;
                  const completed = saleStep > step.id;

                  return (
                    <button
                      key={step.id}
                      type="button"
                      className={`rounded-2xl border px-4 py-4 text-left transition ${
                        active
                          ? "border-slate-900 bg-slate-900 text-white"
                          : completed
                            ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                            : "border-slate-200 bg-slate-50 text-slate-700 hover:bg-white"
                      }`}
                      onClick={() => setSaleStep(step.id)}
                    >
                      <div className="flex items-center gap-3">
                        <span
                          className={`inline-flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold ${
                            active
                              ? "bg-white/15 text-white"
                              : completed
                                ? "bg-emerald-100 text-emerald-800"
                                : "bg-white text-slate-700"
                          }`}
                        >
                          {step.id}
                        </span>
                        <div className="min-w-0">
                          <strong className="block text-sm">{step.title}</strong>
                          <span className={`mt-1 block text-xs ${active ? "text-slate-200" : "text-slate-500"}`}>
                            {step.description}
                          </span>
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 sm:px-6 sm:py-6">
              {saleStep === 1 ? (
                <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
                  <section className={cardClass}>
                    <SectionTitle
                      icon={<UserRound size={20} />}
                      title="Cliente e pagamento"
                      description="Escolha o cliente e defina as condições da venda."
                    />
                    {renderAreaFieldControls("Cliente e pagamento")}

                    <div className="grid gap-4">
                      <Field label="Cliente">
                        <div className="mb-3 space-y-3">
                          <div className="relative">
                            <Search
                              size={16}
                              className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-slate-400"
                            />
                            <input
                              className={`${inputClass} pl-11`}
                              value={clienteBusca}
                              placeholder="Digite nome ou CPF/CNPJ"
                              onChange={(event) => {
                                setClienteBusca(event.target.value);
                                setClienteId("");
                              }}
                            />
                          </div>

                          {selectedCliente ? (
                            <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800">
                              <div className="flex items-start justify-between gap-3">
                                <div>
                                  <strong className="block">{String(selectedCliente.nome ?? "-")}</strong>
                                  <span className="text-emerald-700">
                                    {String(selectedCliente.cpfCnpj ?? "Sem CPF/CNPJ")}
                                  </span>
                                </div>
                                <button
                                  type="button"
                                  className="rounded-xl border border-emerald-200 bg-white px-3 py-1.5 text-xs font-medium text-emerald-700"
                                  onClick={limparClienteSelecionado}
                                >
                                  Trocar
                                </button>
                              </div>
                            </div>
                          ) : null}

                          {!selectedCliente && clienteMatches.length > 0 ? (
                            <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white">
                              {clienteMatches.map((cliente) => (
                                <button
                                  key={String(cliente.id ?? "")}
                                  type="button"
                                  className="flex w-full items-center justify-between gap-3 border-b border-slate-100 px-3 py-2 text-left text-sm last:border-b-0 hover:bg-slate-50"
                                  onClick={() => selecionarCliente(cliente)}
                                >
                                  <span>
                                    <strong className="block text-slate-900">
                                      {String(cliente.nome ?? "-")}
                                    </strong>
                                    <small className="text-slate-500">
                                      {String(cliente.cpfCnpj ?? "Sem CPF/CNPJ")}
                                    </small>
                                  </span>
                                  <span className="text-xs font-medium text-slate-500">Selecionar</span>
                                </button>
                              ))}
                            </div>
                          ) : null}

                          {!selectedCliente && clienteBusca.trim() ? (
                            <button
                              type="button"
                              className="inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                              onClick={() => {
                                setQuickCliente((current) => ({
                                  ...current,
                                  nome: current.nome || clienteBusca,
                                  cpfCnpj: current.cpfCnpj || onlyDigits(clienteBusca),
                                }));
                                setQuickClienteOpen((current) => !current);
                              }}
                            >
                              <Plus size={16} />
                              Criar cliente rápido
                            </button>
                          ) : null}

                          {!selectedCliente && !clienteBusca.trim() ? (
                            <span className="block rounded-2xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-500">
                              Venda como consumidor não identificado, ou busque um cliente acima.
                            </span>
                          ) : null}

                          {quickClienteOpen ? (
                            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-3">
                              <div className="grid gap-3">
                                <input className={inputClass} value={quickCliente.nome} placeholder="Nome do cliente" maxLength={150} onChange={(event) => setQuickCliente((current) => ({ ...current, nome: event.target.value }))} />
                                <input className={inputClass} value={quickCliente.cpfCnpj} placeholder="CPF/CNPJ" maxLength={18} onChange={(event) => setQuickCliente((current) => ({ ...current, cpfCnpj: event.target.value }))} />
                                <input className={inputClass} value={quickCliente.telefone} placeholder="Telefone" maxLength={20} onChange={(event) => setQuickCliente((current) => ({ ...current, telefone: event.target.value }))} />
                                <input className={inputClass} value={quickCliente.email} placeholder="E-mail" type="email" maxLength={200} onChange={(event) => setQuickCliente((current) => ({ ...current, email: event.target.value }))} />
                              </div>

                              <div className="mt-3 flex flex-wrap justify-end gap-2">
                                <button type="button" className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50" onClick={() => setQuickClienteOpen(false)}>
                                  Cancelar
                                </button>
                                <button type="button" className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60" disabled={quickClienteSaving} onClick={() => void criarClienteRapido()}>
                                  {quickClienteSaving ? "Criando..." : "Criar e selecionar"}
                                </button>
                              </div>
                            </div>
                          ) : null}
                        </div>
                        <select
                          className="hidden"
                          value={clienteId}
                          onChange={(event) => setClienteId(event.target.value)}
                        >
                          <option value="">Consumidor não identificado</option>
                          {clientes.data.map((cliente) => (
                            <option key={String(cliente.id ?? "")} value={String(cliente.id ?? "")}>
                              {String(cliente.nome ?? "-")}
                            </option>
                          ))}
                        </select>
                      </Field>

                      <Field label="Forma de pagamento">
                        <select
                          className={inputClass}
                          value={formaPagamento}
                          onChange={(event) => setFormaPagamento(event.target.value)}
                        >
                          {pagamentoOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                      </Field>

                      <Field label="Desconto da venda">
                        <input
                          className={inputClass}
                          type="number"
                          min="0"
                          step="0.01"
                          value={descontoVenda}
                          onChange={(event) => setDescontoVenda(event.target.value)}
                        />
                      </Field>
                    </div>

                    {renderVendaCustomFields("Cliente e pagamento")}
                  </section>

                  <div className="space-y-6">
                    {usaParcelas ? (
                      <section className={cardClass}>
                        <SectionTitle
                          icon={<CreditCard size={20} />}
                          title="Parcelamento"
                          description="Configure as parcelas e confira a prévia."
                        />

                        <div className="grid gap-4 md:grid-cols-3">
                          <Field label="Parcelas">
                            <input
                              className={inputClass}
                              type="number"
                              min="1"
                              max="24"
                              step="1"
                              value={parcelas}
                              onChange={(event) => setParcelas(event.target.value)}
                            />
                          </Field>

                          <Field label="Taxa (%)">
                            <input
                              className={inputClass}
                              type="number"
                              min="0"
                              max="100"
                              step="0.01"
                              value={taxaPercentual}
                              onChange={(event) => setTaxaPercentual(event.target.value)}
                            />
                          </Field>

                          <Field label="Primeiro vencimento">
                            <input
                              className={inputClass}
                              type="date"
                              value={primeiroVencimento}
                              onChange={(event) => setPrimeiroVencimento(event.target.value)}
                            />
                          </Field>
                        </div>

                        <div className="mt-4 rounded-2xl border border-slate-200 bg-slate-50 p-4">
                          <span className="block text-sm font-semibold text-slate-900">Prévia das parcelas</span>

                          <div className="mt-3 space-y-2">
                            {previewParcelas.map((parcela, index) => (
                              <div
                                key={`${parcela.dataVencimento}-${index}`}
                                className="flex items-center justify-between rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm"
                              >
                                <span className="text-slate-600">
                                  {index + 1}ª parcela • {formatDate(parcela.dataVencimento)}
                                </span>
                                <strong className="text-slate-900">{formatCurrency(parcela.valor)}</strong>
                              </div>
                            ))}
                          </div>
                        </div>
                      </section>
                    ) : null}

                    <section className={cardClass}>
                      <SectionTitle
                        icon={<Receipt size={20} />}
                        title="Observações"
                        description="Registre anotações importantes da venda."
                      />

                      {renderAreaFieldControls("Observações")}

                      <Field label="Observações">
                        <textarea
                          className={textareaClass}
                          value={observacoes}
                          maxLength={1000}
                          onChange={(event) => setObservacoes(event.target.value)}
                          placeholder="Ex.: cliente pediu entrega amanhã, pagamento parcialmente combinado..."
                        />
                      </Field>

                      {renderVendaCustomFields("Observações")}
                    </section>
                  </div>
                </div>
              ) : null}

              {saleStep === 2 ? (
                <div className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
                  <section className={cardClass}>
                    <SectionTitle
                      icon={<Package size={20} />}
                      title="Adicionar item"
                      description="Selecione a peça, confira o estoque e envie para o carrinho."
                    />

                    {renderAreaFieldControls("Adicionar item")}

                    <div className="grid gap-4 md:grid-cols-2">
                      <Field label="Peça">
                        <select
                          className={inputClass}
                          value={pecaId}
                          onChange={(event) => selecionarPeca(event.target.value)}
                        >
                          <option value="">Selecione</option>
                          {pecas.data.map((peca) => (
                            <option key={String(peca.id)} value={String(peca.id ?? "")}>
                              {String(peca.nome ?? "Peça")} • {formatCurrency(peca.precoVenda)} • estoque{" "}
                              {String(peca.estoqueAtual ?? 0)}
                            </option>
                          ))}
                        </select>
                      </Field>

                      <Field label="Quantidade">
                        <input
                          className={inputClass}
                          type="number"
                          min="0.001"
                          step="0.001"
                          value={quantidade}
                          onChange={(event) => setQuantidade(event.target.value)}
                        />
                      </Field>

                      <Field label="Valor unitário">
                        <input
                          className={inputClass}
                          type="number"
                          min="0"
                          step="0.01"
                          value={valorUnitario}
                          onChange={(event) => setValorUnitario(event.target.value)}
                        />
                      </Field>

                      <Field label="Desconto do item">
                        <input
                          className={inputClass}
                          type="number"
                          min="0"
                          step="0.01"
                          value={descontoItem}
                          onChange={(event) => setDescontoItem(event.target.value)}
                        />
                      </Field>
                    </div>

                    {selectedPeca ? (
                      <div className="mt-4 grid gap-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 sm:grid-cols-3">
                        <div>
                          <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Peça selecionada
                          </span>
                          <strong className="mt-1 block text-sm text-slate-900">
                            {String(selectedPeca.nome ?? "Peça")}
                          </strong>
                        </div>

                        <div>
                          <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Estoque
                          </span>
                          <span
                            className={[
                              "mt-1 inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
                              estoqueTone(estoqueSelecionado),
                            ].join(" ")}
                          >
                            {estoqueSelecionado <= 0 ? "Sem estoque" : `${estoqueSelecionado} disponível`}
                          </span>
                        </div>

                        <div>
                          <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Preço padrão
                          </span>
                          <strong className="mt-1 block text-sm text-slate-900">
                            {formatCurrency(precoSelecionado)}
                          </strong>
                        </div>
                      </div>
                    ) : null}

                    <button
                      type="button"
                      className="mt-4 inline-flex w-full items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
                      onClick={adicionarItem}
                      disabled={estoqueSelecionado <= 0 && Boolean(selectedPeca)}
                    >
                      <Plus size={16} />
                      Adicionar ao carrinho
                    </button>

                    {renderVendaCustomFields("Adicionar item")}
                  </section>

                  <section className={cardClass}>
                    <SectionTitle
                      icon={<ShoppingCart size={20} />}
                      title="Itens no carrinho"
                      description="Revise os itens adicionados antes de seguir."
                    />

                    {cart.length === 0 ? (
                      <EmptyState
                        title="Carrinho vazio"
                        description="Adicione uma peça para começar a montar a venda."
                        icon={ShoppingCart}
                      />
                    ) : (
                      <div className="space-y-3">
                        {cart.map((item) => (
                          <div
                            key={item.key}
                            className="rounded-2xl border border-slate-200 bg-slate-50 p-4"
                          >
                            <div className="flex items-start justify-between gap-3">
                              <div className="min-w-0">
                                <strong className="block truncate text-sm text-slate-900">
                                  {item.descricao}
                                </strong>

                                <div className="mt-2 flex flex-wrap gap-2 text-xs text-slate-500">
                                  <span className="rounded-full bg-white px-2 py-1">
                                    Qtd: {item.quantidade}
                                  </span>
                                  <span className="rounded-full bg-white px-2 py-1">
                                    Unitário: {formatCurrency(item.valorUnitario)}
                                  </span>
                                  <span className="rounded-full bg-white px-2 py-1">
                                    Desconto: {formatCurrency(item.desconto)}
                                  </span>
                                  <span className="rounded-full bg-white px-2 py-1">
                                    Estoque: {item.estoqueAtual}
                                  </span>
                                </div>
                              </div>

                              <button
                                className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-rose-200 bg-rose-50 text-rose-700 transition hover:bg-rose-100"
                                type="button"
                                onClick={() => removerItem(item.key)}
                                title="Remover item"
                              >
                                <Trash2 size={16} />
                              </button>
                            </div>

                            <div className="mt-3 flex items-center justify-between border-t border-slate-200 pt-3 text-sm">
                              <span className="text-slate-500">Total do item</span>
                              <strong className="text-slate-900">{formatCurrency(cartTotal(item))}</strong>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </section>
                </div>
              ) : null}

              {saleStep === 3 ? (
                <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
                  <section className={`${cardClass} space-y-4`}>
                    <SectionTitle
                      icon={<Wallet size={20} />}
                      title="Resumo da venda"
                      description="Confira os totais e finalize a operação."
                    />

                    {renderAreaFieldControls("Resumo da venda")}

                    <div className="space-y-3">
                      <div className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3 text-sm">
                        <span className="text-slate-500">Itens</span>
                        <strong className="text-slate-900">{cart.length}</strong>
                      </div>

                      <div className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3 text-sm">
                        <span className="text-slate-500">Subtotal</span>
                        <strong className="text-slate-900">{formatCurrency(subtotal)}</strong>
                      </div>

                      <div className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3 text-sm">
                        <span className="text-slate-500">Desconto</span>
                        <strong className="text-slate-900">{formatCurrency(descontoVenda)}</strong>
                      </div>

                      <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-4">
                        <span className="block text-sm text-emerald-700">Total da venda</span>
                        <strong className="mt-1 block text-2xl font-bold tracking-tight text-emerald-900">
                          {formatCurrency(total)}
                        </strong>
                      </div>

                      {usaParcelas ? (
                        <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm">
                          <span className="text-slate-500">Parcelamento</span>
                          <strong className="ml-2 text-slate-900">{previewParcelas.length}x</strong>
                        </div>
                      ) : null}
                    </div>

                    {renderVendaCustomFields("Resumo da venda")}
                  </section>

                  <div className="space-y-6">
                    <section className={cardClass}>
                      <SectionTitle
                        icon={<UserRound size={20} />}
                        title="Dados da operação"
                        description="Resumo rápido do cliente, pagamento e observações."
                      />

                      <div className="grid gap-3 md:grid-cols-2">
                        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                          <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Cliente
                          </span>
                          <strong className="mt-2 block text-sm text-slate-900">
                            {selectedCliente ? String(selectedCliente.nome ?? "-") : "Consumidor não identificado"}
                          </strong>
                          <span className="mt-1 block text-sm text-slate-500">
                            {selectedCliente ? String(selectedCliente.cpfCnpj ?? "Sem CPF/CNPJ") : "Sem cadastro selecionado"}
                          </span>
                        </div>

                        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                          <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                            Pagamento
                          </span>
                          <strong className="mt-2 block text-sm text-slate-900">
                            {pagamentoOptions.find((option) => option.value === formaPagamento)?.label ?? formaPagamento}
                          </strong>
                          <span className="mt-1 block text-sm text-slate-500">
                            {usaParcelas ? `${previewParcelas.length} parcela(s)` : "Pagamento à vista"}
                          </span>
                        </div>
                      </div>

                      <div className="mt-4 rounded-2xl border border-slate-200 bg-slate-50 p-4">
                        <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">
                          Observações
                        </span>
                        <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">
                          {observacoes.trim() || "Nenhuma observação informada."}
                        </p>
                      </div>
                    </section>

                    <section className={cardClass}>
                      <SectionTitle
                        icon={<ShoppingCart size={20} />}
                        title="Carrinho"
                        description="Conferência final dos itens adicionados."
                      />

                      {cart.length === 0 ? (
                        <EmptyState
                          title="Carrinho vazio"
                          description="Volte para a etapa anterior e adicione itens."
                          icon={ShoppingCart}
                        />
                      ) : (
                        <div className="space-y-3">
                          {cart.map((item) => (
                            <div key={item.key} className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                              <div className="flex items-start justify-between gap-3">
                                <div className="min-w-0">
                                  <strong className="block truncate text-sm text-slate-900">
                                    {item.descricao}
                                  </strong>
                                  <div className="mt-2 flex flex-wrap gap-2 text-xs text-slate-500">
                                    <span className="rounded-full bg-white px-2 py-1">Qtd: {item.quantidade}</span>
                                    <span className="rounded-full bg-white px-2 py-1">
                                      Unitário: {formatCurrency(item.valorUnitario)}
                                    </span>
                                    <span className="rounded-full bg-white px-2 py-1">
                                      Desconto: {formatCurrency(item.desconto)}
                                    </span>
                                  </div>
                                </div>
                                <strong className="text-sm text-slate-900">{formatCurrency(cartTotal(item))}</strong>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </section>
                  </div>
                </div>
              ) : null}
            </div>

            <div className="flex flex-col gap-3 border-t border-slate-200 px-6 py-5 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                  onClick={closeSaleWizard}
                  disabled={saving}
                >
                  Cancelar
                </button>

                <button
                  type="button"
                  className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                  onClick={limparVenda}
                  disabled={saving}
                >
                  Limpar venda
                </button>
              </div>

              <div className="flex flex-wrap justify-end gap-2">
                {saleStep > 1 ? (
                  <button
                    type="button"
                    className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                    onClick={() => setSaleStep((current) => Math.max(1, current - 1) as SaleStep)}
                    disabled={saving}
                  >
                    <ArrowLeft size={16} />
                    Voltar
                  </button>
                ) : null}

                {saleStep < 3 ? (
                  <button
                    type="button"
                    className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                    onClick={() => {
                      if (saleStep === 2 && !canAdvanceFromItems) {
                        setFailure("Adicione pelo menos um item antes de avançar.");
                        return;
                      }
                      setSaleStep((current) => Math.min(3, current + 1) as SaleStep);
                    }}
                    disabled={saving}
                  >
                    Avançar
                    <ArrowRight size={16} />
                  </button>
                ) : (
                  <>
                    <button
                      type="button"
                      className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                      onClick={() => void salvarVenda(false)}
                      disabled={saving}
                    >
                      {saving ? "Salvando..." : "Salvar rascunho"}
                    </button>

                    <button
                      type="button"
                      className="inline-flex items-center justify-center rounded-2xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                      onClick={() => void salvarVenda(true)}
                      disabled={saving}
                    >
                      {saving ? "Finalizando..." : "Salvar e finalizar"}
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
      ) : null}

      {showCustomBuilder && canManageCustomFields ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <form
            className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-[28px] border border-slate-200 bg-white p-6 shadow-2xl"
            onSubmit={salvarCampoExtra}
          >
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h3 className="text-xl font-bold tracking-tight text-slate-900">
                  {editingCustomFieldId ? "Editar campo" : "Adicionar campo"}
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  O campo aparecerá automaticamente na etapa escolhida do fluxo de vendas.
                </p>
              </div>

              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 transition hover:bg-slate-50"
                onClick={() => {
                  setEditingCustomFieldId("");
                  setShowCustomBuilder(false);
                  setCustomFieldForm(defaultForm(customFieldFormFields));
                  setCustomFieldErrors({});
                }}
              >
                <X size={18} />
              </button>
            </div>

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {customFieldFormFields
                .filter((field) => field.name !== "opcoesText" || customFieldForm.tipo === "select")
                .map((field) => (
                  <FieldRenderer
                    key={field.name}
                    field={editingCustomFieldId && field.name === "tipo" ? { ...field, disabled: true } : field}
                    value={customFieldForm[field.name]}
                    error={customFieldErrors[field.name]}
                    onChange={(name, value) => {
                      setCustomFieldForm((current) => ({ ...current, [name]: value }));
                      setCustomFieldErrors((current) => {
                        const next = { ...current };
                        delete next[name];
                        return next;
                      });
                    }}
                  />
                ))}
            </div>

            <div className="mt-6 flex flex-wrap justify-end gap-3">
              {editingCustomFieldId ? (
                <button
                  className="inline-flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100"
                  type="button"
                  onClick={() => void excluirCampoExtra()}
                >
                  <Trash2 size={16} />
                  Excluir campo
                </button>
              ) : null}

              <button
                className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                type="button"
                onClick={() => {
                  setEditingCustomFieldId("");
                  setShowCustomBuilder(false);
                  setCustomFieldForm(defaultForm(customFieldFormFields));
                  setCustomFieldErrors({});
                }}
              >
                Cancelar
              </button>

              <button className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800" type="submit">
                {editingCustomFieldId ? "Salvar campo" : "Criar campo"}
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {stockPopupOpen ? (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <div className="relative w-full max-w-md rounded-[28px] border border-rose-200 bg-white p-6 shadow-2xl">
            <div className="flex items-start gap-3">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-rose-50 text-rose-700">
                <AlertTriangle size={22} />
              </div>

              <div className="min-w-0">
                <h3 className="text-lg font-bold text-slate-900">Estoque insuficiente</h3>
                <p className="mt-2 text-sm leading-6 text-slate-600">
                  {stockPopupMessage}
                </p>
              </div>
            </div>

            <div className="mt-6 flex justify-end">
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800"
                onClick={() => setStockPopupOpen(false)}
              >
                Entendi
              </button>
            </div>

            <button
              type="button"
              className="absolute right-4 top-4 inline-flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-50 hover:text-slate-900"
              onClick={() => setStockPopupOpen(false)}
              aria-label="Fechar pop-up"
            >
              <X size={16} />
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
