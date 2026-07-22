import { useState } from "react";
import { DollarSign, Lock, TrendingUp, Unlock, Wallet, X } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Link } from "react-router-dom";

import { CrudPage } from "../components/CrudPage";
import { Notice, PageFrame } from "../components/Ui";
import type { FieldConfig } from "../components/Ui";
import { useList, useOptions } from "../hooks/useApi";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import {
  errorMessage,
  formatCurrency,
  formatDate,
  formatFieldInput,
  parseMoney,
} from "../components/uiHelpers";

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 disabled:cursor-not-allowed disabled:bg-slate-100";

const MONEY_FIELD: FieldConfig = { name: "valor", label: "Valor", type: "currency" };

function maskMoney(value: unknown) {
  return formatFieldInput(MONEY_FIELD, value);
}

function moneyFromNumber(value: number) {
  return maskMoney(String(Math.round(value * 100)));
}

function toNumber(value: unknown) {
  const number = Number(value ?? 0);
  return Number.isFinite(number) ? number : 0;
}

const categoriaDespesaOptions = [
  { value: "Aluguel", label: "Aluguel" },
  { value: "Manutenção", label: "Manutenção" },
  { value: "Fornecedores", label: "Fornecedores" },
  { value: "Salários", label: "Salários" },
  { value: "Impostos", label: "Impostos" },
  { value: "Marketing", label: "Marketing" },
  { value: "Utilidades", label: "Utilidades (água, luz, internet)" },
  { value: "Outras", label: "Outras" },
];

function buttonClass(variant: "primary" | "secondary" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60";
}

function card(title: string, value: string, icon: LucideIcon) {
  const Icon = icon;

  return (
    <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
          <Icon size={20} />
        </div>

        <div>
          <p className="text-sm text-slate-500">{title}</p>
          <strong className="text-xl text-slate-900">{value}</strong>
        </div>
      </div>
    </div>
  );
}

type PagamentoModal = {
  tipo: "receber" | "pagar";
  row: ApiRecord;
  refresh: () => void;
};

export function FinanceiroPage() {
  const clientes = useOptions("/clientes", "nome");
  const fornecedores = useOptions("/fornecedores", "nome");
  const [reload, setReload] = useState(0);

  const caixas = useList("/caixas-diarios", reload);
  const caixaAberto = caixas.data.find((c) => String(c.status ?? "") === "ABERTO");

  const [pagamentoModal, setPagamentoModal] = useState<PagamentoModal | null>(null);
  const [valorPagamento, setValorPagamento] = useState(() => maskMoney("0"));
  const [salvandoPagamento, setSalvandoPagamento] = useState(false);
  const [failure, setFailure] = useState("");

  function abrirModalPagamento(tipo: "receber" | "pagar", row: ApiRecord, refresh: () => void) {
    setFailure("");

    if (!caixaAberto) {
      setFailure("Abra o caixa antes de registrar recebimentos ou pagamentos.");
      return;
    }

    const pendente =
      tipo === "receber"
        ? toNumber(row.valor) - toNumber(row.valorRecebido)
        : toNumber(row.valor) - toNumber(row.valorPago);

    setValorPagamento(moneyFromNumber(Math.max(0, pendente)));
    setPagamentoModal({ tipo, row, refresh });
  }

  function fecharModalPagamento() {
    setPagamentoModal(null);
    setValorPagamento(maskMoney("0"));
  }

  async function confirmarPagamento() {
    if (!pagamentoModal || !caixaAberto) return;

    const valor = parseMoney(valorPagamento);
    if (valor <= 0) {
      setFailure("Informe um valor válido.");
      return;
    }

    setSalvandoPagamento(true);
    setFailure("");

    try {
      if (pagamentoModal.tipo === "receber") {
        await apiRequest(`/contas-receber/${pagamentoModal.row.id}/receber`, {
          method: "PATCH",
          body: { valorRecebido: valor, caixaDiarioId: caixaAberto.id },
        });
      } else {
        await apiRequest(`/contas-pagar/${pagamentoModal.row.id}/pagar`, {
          method: "PATCH",
          body: { valorPago: valor, caixaDiarioId: caixaAberto.id },
        });
      }

      pagamentoModal.refresh();
      setReload((r) => r + 1);
      fecharModalPagamento();
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSalvandoPagamento(false);
    }
  }

  return (
    <PageFrame
      eyebrow="Financeiro"
      title="Controle financeiro"
      description="Gerencie caixa, entradas e saídas de forma simples."
    >
      <div className="space-y-6">
        {!caixaAberto ? (
          <Notice type="error">
            <span className="flex flex-wrap items-center gap-2">
              <Lock size={16} />
              O caixa está fechado. Abra o caixa para registrar recebimentos e pagamentos.
              <Link to="/caixa" className="font-semibold underline">
                Ir para o Caixa
              </Link>
            </span>
          </Notice>
        ) : (
          <Notice type="success">
            <span className="flex items-center gap-2">
              <Unlock size={16} />
              Caixa aberto: {formatDate(caixaAberto.dataCaixa)}.
            </span>
          </Notice>
        )}

        {failure ? <Notice type="error">{failure}</Notice> : null}

        <div className="grid gap-4 md:grid-cols-3">
          {card("Caixas", String(caixas.data.length), Wallet)}
          {card("Status", caixaAberto ? "Aberto" : "Fechado", DollarSign)}
          {card("Atualizações", "Tempo real", TrendingUp)}
        </div>

        <CrudPage
          embedded
          title="Contas a receber"
          description="Registre e acompanhe valores pendentes de clientes."
          endpoint="/contas-receber"
          fields={[
            { name: "clienteId", label: "Cliente", type: "select", options: clientes },
            { name: "descricao", label: "Descrição", required: true },
            { name: "dataVencimento", label: "Vencimento", type: "date", required: true },
            { name: "valor", label: "Valor", type: "currency", required: true },
          ]}
          columns={[
            { key: "descricao", label: "Descrição" },
            { key: "clienteNome", label: "Cliente" },
            { key: "valor", label: "Valor", render: (r) => formatCurrency(r.valor) },
            { key: "status", label: "Status" },
          ]}
          rowActions={(row, refresh) =>
            row.status !== "PAGO" ? (
              <button
                className="text-blue-600"
                type="button"
                onClick={() => abrirModalPagamento("receber", row, refresh)}
              >
                Receber
              </button>
            ) : null
          }
        />

        <CrudPage
          embedded
          title="Contas a pagar"
          description="Registre e acompanhe despesas, fornecedores e pagamentos."
          endpoint="/contas-pagar"
          fields={[
            { name: "descricao", label: "Descrição", required: true },
            { name: "fornecedorId", label: "Fornecedor cadastrado", type: "select", options: fornecedores },
            { name: "fornecedor", label: "Fornecedor manual" },
            { name: "categoria", label: "Categoria", type: "select", options: categoriaDespesaOptions },
            { name: "dataVencimento", label: "Vencimento", type: "date", required: true },
            { name: "valor", label: "Valor", type: "currency", required: true },
          ]}
          columns={[
            { key: "descricao", label: "Descrição" },
            { key: "fornecedor", label: "Fornecedor" },
            { key: "categoria", label: "Categoria", render: (r) => String(r.categoria ?? "Sem categoria") },
            { key: "valor", label: "Valor", render: (r) => formatCurrency(r.valor) },
            { key: "status", label: "Status" },
          ]}
          rowActions={(row, refresh) =>
            row.status !== "PAGO" ? (
              <button
                className="text-red-600"
                type="button"
                onClick={() => abrirModalPagamento("pagar", row, refresh)}
              >
                Pagar
              </button>
            ) : null
          }
        />
      </div>

      {pagamentoModal ? (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-slate-950/80 p-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-[28px] border border-slate-200 bg-white shadow-2xl">
            <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-5">
              <div>
                <h3 className="text-lg font-bold tracking-tight text-slate-900">
                  {pagamentoModal.tipo === "receber" ? "Registrar recebimento" : "Registrar pagamento"}
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  {String(pagamentoModal.row.descricao ?? "")}
                </p>
              </div>
              <button type="button" className={buttonClass()} onClick={fecharModalPagamento}>
                <X size={16} />
                Fechar
              </button>
            </div>

            <div className="space-y-4 px-6 py-5">
              <label className="block">
                <span className="mb-2 block text-sm font-medium text-slate-700">
                  {pagamentoModal.tipo === "receber" ? "Valor recebido" : "Valor pago"}
                </span>
                <input
                  className={inputClass}
                  type="text"
                  inputMode="numeric"
                  value={valorPagamento}
                  onChange={(event) => setValorPagamento(maskMoney(event.target.value))}
                  autoFocus
                />
              </label>

              <div className="flex justify-end gap-3">
                <button type="button" className={buttonClass()} onClick={fecharModalPagamento}>
                  Cancelar
                </button>
                <button
                  type="button"
                  className={buttonClass("primary")}
                  disabled={salvandoPagamento}
                  onClick={() => void confirmarPagamento()}
                >
                  {salvandoPagamento ? "Salvando..." : "Confirmar"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </PageFrame>
  );
}
