import { useState } from "react";

import { lookupAddressByCep } from "../lib/cep";
import { onlyDigits } from "./uiHelpers";

export type QuickAddressForm = {
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  uf: string;
};

export const emptyQuickAddress: QuickAddressForm = {
  cep: "",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  cidade: "",
  uf: "",
};

export function formatCep(value: unknown) {
  return onlyDigits(value).slice(0, 8).replace(/^(\d{5})(\d)/, "$1-$2");
}

export function quickTabClass(active: boolean) {
  return [
    "rounded-xl px-4 py-2 text-sm font-medium transition",
    active ? "bg-white text-slate-900 shadow-sm" : "text-slate-500 hover:text-slate-900",
  ].join(" ");
}

export function useQuickCepLookup() {
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  async function lookup(cepValue: unknown, onFound: (address: Partial<QuickAddressForm>) => void) {
    const digits = onlyDigits(cepValue);
    if (digits.length !== 8) {
      setError("Informe um CEP com 8 dígitos.");
      setMessage("");
      return;
    }

    setLoading(true);
    setError("");
    setMessage("");

    try {
      const address = await lookupAddressByCep(digits);
      onFound(address);
      setMessage("Endereço preenchido a partir do CEP.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Não foi possível consultar o CEP agora.");
    } finally {
      setLoading(false);
    }
  }

  function reset() {
    setMessage("");
    setError("");
  }

  return { loading, message, error, lookup, reset };
}
