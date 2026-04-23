import { useState } from "react";
import { DollarSign, TrendingUp, Wallet } from "lucide-react";
import type { LucideIcon } from "lucide-react";

import { CrudPage } from "../components/CrudPage";
import { Notice, PageFrame } from "../components/Ui";
import { useList, useOptions } from "../hooks/useApi";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { formatCurrency, formatDate } from "../components/uiHelpers";

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

export function FinanceiroPage() {
  const clientes = useOptions("/clientes", "nome");
  const fornecedores = useOptions("/fornecedores", "nome");
  const [reload, setReload] = useState(0);

  const caixas = useList("/caixas-diarios", reload);
  const caixaAberto = caixas.data.find((c) => String(c.status ?? "") === "ABERTO");

  async function receber(row: ApiRecord, refresh: () => void) {
    if (!caixaAberto) {
      window.alert("Abra o caixa primeiro.");
      return;
    }

    const valor = Number(window.prompt("Valor recebido", "0") || 0);
    if (!Number.isFinite(valor) || valor <= 0) {
      window.alert("Informe um valor válido.");
      return;
    }

    await apiRequest(`/contas-receber/${row.id}/receber`, {
      method: "PATCH",
      body: {
        valorRecebido: valor,
        caixaDiarioId: caixaAberto.id,
      },
    });

    refresh();
    setReload((r) => r + 1);
  }

  async function pagar(row: ApiRecord, refresh: () => void) {
    if (!caixaAberto) {
      window.alert("Abra o caixa primeiro.");
      return;
    }

    const valor = Number(window.prompt("Valor pago", "0") || 0);
    if (!Number.isFinite(valor) || valor <= 0) {
      window.alert("Informe um valor válido.");
      return;
    }

    await apiRequest(`/contas-pagar/${row.id}/pagar`, {
      method: "PATCH",
      body: {
        valorPago: valor,
        caixaDiarioId: caixaAberto.id,
      },
    });

    refresh();
    setReload((r) => r + 1);
  }

  return (
    <PageFrame
      eyebrow="Financeiro"
      title="Controle financeiro"
      description="Gerencie caixa, entradas e saídas de forma simples."
    >
      <div className="space-y-6">
        {!caixaAberto ? (
          <Notice type="info">Abra o caixa para começar.</Notice>
        ) : (
          <Notice type="success">
            Caixa aberto: {formatDate(caixaAberto.dataCaixa)}.
          </Notice>
        )}

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
            { name: "valor", label: "Valor", type: "number", required: true },
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
                onClick={() => void receber(row, refresh)}
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
            { name: "dataVencimento", label: "Vencimento", type: "date", required: true },
            { name: "valor", label: "Valor", type: "number", required: true },
          ]}
          columns={[
            { key: "descricao", label: "Descrição" },
            { key: "fornecedor", label: "Fornecedor" },
            { key: "valor", label: "Valor", render: (r) => formatCurrency(r.valor) },
            { key: "status", label: "Status" },
          ]}
          rowActions={(row, refresh) =>
            row.status !== "PAGO" ? (
              <button
                className="text-red-600"
                type="button"
                onClick={() => void pagar(row, refresh)}
              >
                Pagar
              </button>
            ) : null
          }
        />
      </div>
    </PageFrame>
  );
}
