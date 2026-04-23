import { useMemo, useState } from "react";
import { PackageCheck, RefreshCw, ShoppingCart, Truck } from "lucide-react";

import { DataTable, Notice } from "../components/Ui";
import { PageHeader } from "../components/app/PageHeader";
import { PageSection } from "../components/app/PageSection";
import { StatCard } from "../components/app/StartCard";
import { useList, useOptions } from "../hooks/useApi";
import { apiRequest } from "../lib/api";
import type { ApiRecord } from "../lib/api";
import { errorMessage, formatCurrency, formatDate } from "../components/uiHelpers";

const inputClass =
  "h-11 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 disabled:cursor-not-allowed disabled:bg-slate-100";

const textareaClass =
  "min-h-[110px] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-slate-400 focus:ring-4 focus:ring-slate-200/60 resize-y";

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function toNumber(value: unknown) {
  const number = Number(value ?? 0);
  return Number.isFinite(number) ? number : 0;
}

function buttonClass(variant: "primary" | "secondary" = "secondary") {
  if (variant === "primary") {
    return "inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60";
  }

  return "inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60";
}

function estoqueBadge(row: ApiRecord) {
  const atual = toNumber(row.estoqueAtual);
  const minimo = toNumber(row.estoqueMinimo);
  const baixo = minimo > 0 && atual <= minimo;

  return (
    <span
      className={[
        "inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold",
        baixo
          ? "border-amber-200 bg-amber-50 text-amber-700"
          : "border-emerald-200 bg-emerald-50 text-emerald-700",
      ].join(" ")}
    >
      {atual} / min. {minimo}
    </span>
  );
}

export function ComprasEstoquePage() {
  const [reload, setReload] = useState(0);
  const pecas = useList("/pecas", reload);
  const fornecedores = useOptions("/fornecedores", "nome");

  const [pecaId, setPecaId] = useState("");
  const [fornecedorId, setFornecedorId] = useState("");
  const [quantidade, setQuantidade] = useState("1");
  const [custoUnitario, setCustoUnitario] = useState("0");
  const [dataVencimento, setDataVencimento] = useState(todayIso());
  const [gerarContaPagar, setGerarContaPagar] = useState(true);
  const [observacoes, setObservacoes] = useState("");
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState("");
  const [failure, setFailure] = useState("");

  const selectedPeca = useMemo(
    () => pecas.data.find((item) => String(item.id ?? "") === pecaId),
    [pecaId, pecas.data],
  );

  const baixoEstoque = useMemo(
    () =>
      pecas.data.filter((peca) => {
        const minimo = toNumber(peca.estoqueMinimo);
        return minimo > 0 && toNumber(peca.estoqueAtual) <= minimo;
      }),
    [pecas.data],
  );

  const totalCompra = toNumber(quantidade) * toNumber(custoUnitario);

  function selecionarPeca(row: ApiRecord) {
    const atual = toNumber(row.estoqueAtual);
    const minimo = toNumber(row.estoqueMinimo);
    const reposicao = Math.max(0, minimo - atual) || minimo || 1;

    setPecaId(String(row.id ?? ""));
    setFornecedorId(String(row.fornecedorId ?? ""));
    setQuantidade(String(reposicao));
    setCustoUnitario(String(toNumber(row.custoUnitario)));
    setObservacoes(`Reposicao de estoque: ${String(row.nome ?? "peca")}.`);
  }

  function limparFormulario() {
    setPecaId("");
    setFornecedorId("");
    setQuantidade("1");
    setCustoUnitario("0");
    setDataVencimento(todayIso());
    setGerarContaPagar(true);
    setObservacoes("");
  }

  async function registrarCompra() {
    setNotice("");
    setFailure("");

    if (!pecaId) {
      setFailure("Selecione uma peca antes de registrar a compra.");
      return;
    }

    if (toNumber(quantidade) <= 0) {
      setFailure("Informe uma quantidade maior que zero.");
      return;
    }

    if (toNumber(custoUnitario) < 0) {
      setFailure("O custo unitario nao pode ser negativo.");
      return;
    }

    setSaving(true);

    try {
      const fornecedorNome = fornecedores.find((item) => item.value === fornecedorId)?.label;

      await apiRequest("/gestao/compras-pecas", {
        method: "POST",
        body: {
          pecaId,
          fornecedorId: fornecedorId || null,
          fornecedor: fornecedorNome || null,
          quantidade: toNumber(quantidade),
          custoUnitario: toNumber(custoUnitario),
          dataVencimento,
          gerarContaPagar,
          observacoes: observacoes.trim() || null,
        },
      });

      setNotice("Compra registrada, estoque atualizado e conta a pagar criada quando solicitado.");
      limparFormulario();
      setReload((value) => value + 1);
    } catch (err) {
      setFailure(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Estoque"
        title="Compras e reposicao"
        description="Registre entradas de pecas, atualize o custo e gere contas a pagar no mesmo passo."
        actions={
          <button
            type="button"
            className={buttonClass()}
            onClick={() => setReload((value) => value + 1)}
          >
            <RefreshCw size={16} />
            Atualizar
          </button>
        }
      />

      {notice ? <Notice type="success">{notice}</Notice> : null}
      {failure || pecas.error ? <Notice type="error">{failure || pecas.error}</Notice> : null}

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard
          title="Pecas cadastradas"
          value={pecas.data.length}
          description="Itens disponiveis"
          icon={PackageCheck}
        />
        <StatCard
          title="Estoque baixo"
          value={baixoEstoque.length}
          description="Precisam de atencao"
          icon={ShoppingCart}
          tone={baixoEstoque.length > 0 ? "warning" : "success"}
        />
        <StatCard
          title="Total da compra"
          value={formatCurrency(totalCompra)}
          description="Pelo formulario atual"
          icon={Truck}
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1fr_0.9fr]">
        <PageSection
          title="Registrar entrada"
          description="Selecione a peca e confirme os dados da compra."
        >
          <div className="grid gap-4 md:grid-cols-2">
            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Peca</span>
              <select
                className={inputClass}
                value={pecaId}
                onChange={(event) => {
                  const row = pecas.data.find((item) => String(item.id ?? "") === event.target.value);
                  if (row) selecionarPeca(row);
                  else setPecaId("");
                }}
              >
                <option value="">Selecione</option>
                {pecas.data.map((peca) => (
                  <option key={String(peca.id ?? "")} value={String(peca.id ?? "")}>
                    {String(peca.nome ?? "Peca")} · estoque {String(peca.estoqueAtual ?? 0)}
                  </option>
                ))}
              </select>
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Fornecedor</span>
              <select
                className={inputClass}
                value={fornecedorId}
                onChange={(event) => setFornecedorId(event.target.value)}
              >
                <option value="">Sem fornecedor cadastrado</option>
                {fornecedores.map((fornecedor) => (
                  <option key={fornecedor.value} value={fornecedor.value}>
                    {fornecedor.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Quantidade</span>
              <input
                className={inputClass}
                type="number"
                min="0.001"
                step="0.001"
                value={quantidade}
                onChange={(event) => setQuantidade(event.target.value)}
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Custo unitario</span>
              <input
                className={inputClass}
                type="number"
                min="0"
                step="0.01"
                value={custoUnitario}
                onChange={(event) => setCustoUnitario(event.target.value)}
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">Vencimento</span>
              <input
                className={inputClass}
                type="date"
                value={dataVencimento}
                onChange={(event) => setDataVencimento(event.target.value)}
              />
            </label>

            <label className="flex min-h-11 items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={gerarContaPagar}
                onChange={(event) => setGerarContaPagar(event.target.checked)}
              />
              Gerar conta a pagar
            </label>

            <label className="block md:col-span-2">
              <span className="mb-2 block text-sm font-medium text-slate-700">Observacoes</span>
              <textarea
                className={textareaClass}
                value={observacoes}
                maxLength={1000}
                onChange={(event) => setObservacoes(event.target.value)}
              />
            </label>
          </div>

          {selectedPeca ? (
            <div className="mt-4 rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
              <strong className="text-slate-900">{String(selectedPeca.nome ?? "Peca")}</strong>
              <span className="ml-2">
                Estoque atual: {String(selectedPeca.estoqueAtual ?? 0)} · minimo:{" "}
                {String(selectedPeca.estoqueMinimo ?? 0)} · ultimo custo:{" "}
                {formatCurrency(selectedPeca.custoUnitario)}
              </span>
            </div>
          ) : null}

          <div className="mt-5 flex flex-wrap justify-end gap-2">
            <button type="button" className={buttonClass()} onClick={limparFormulario}>
              Limpar
            </button>
            <button
              type="button"
              className={buttonClass("primary")}
              disabled={saving}
              onClick={() => void registrarCompra()}
            >
              {saving ? "Registrando..." : "Registrar compra"}
            </button>
          </div>
        </PageSection>

        <PageSection
          title="Sugestoes de reposicao"
          description="Pecas abaixo ou no estoque minimo."
        >
          <DataTable
            columns={[
              { key: "nome", label: "Peca" },
              { key: "fornecedorNome", label: "Fornecedor" },
              { key: "estoqueAtual", label: "Estoque", render: estoqueBadge },
              { key: "custoUnitario", label: "Custo", render: (row) => formatCurrency(row.custoUnitario) },
            ]}
            rows={baixoEstoque}
            loading={pecas.loading}
            emptyText="Nenhuma peca abaixo do estoque minimo."
            actions={(row) => (
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                onClick={() => selecionarPeca(row)}
              >
                Repor
              </button>
            )}
          />

          <p className="mt-3 text-xs text-slate-500">
            Hoje: {formatDate(todayIso())}. A entrada atualiza o estoque imediatamente.
          </p>
        </PageSection>
      </div>
    </div>
  );
}
