export type CepAddress = {
  logradouro?: string;
  complemento?: string;
  bairro?: string;
  cidade?: string;
  uf?: string;
};

function onlyDigits(value: unknown) {
  return String(value ?? "").replace(/\D/g, "");
}

export async function lookupAddressByCep(value: unknown): Promise<CepAddress> {
  const cep = onlyDigits(value);

  if (cep.length !== 8) {
    throw new Error("Informe um CEP com 8 digitos.");
  }

  const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
  const data = (await response.json()) as {
    erro?: boolean;
    logradouro?: string;
    complemento?: string;
    bairro?: string;
    localidade?: string;
    uf?: string;
  };

  if (!response.ok || data.erro) {
    throw new Error("CEP nao encontrado.");
  }

  return {
    logradouro: data.logradouro,
    complemento: data.complemento,
    bairro: data.bairro,
    cidade: data.localidade,
    uf: data.uf,
  };
}
