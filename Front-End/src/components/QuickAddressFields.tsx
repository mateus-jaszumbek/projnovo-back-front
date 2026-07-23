import { onlyDigits } from "./uiHelpers";
import { formatCep, quickTabClass } from "./quickAddressHelpers";
import type { QuickAddressForm } from "./quickAddressHelpers";

export function QuickFormTabs<T extends string>({
  tabs,
  active,
  onChange,
}: {
  tabs: { id: T; label: string }[];
  active: T;
  onChange: (id: T) => void;
}) {
  return (
    <div className="mb-4 inline-flex flex-wrap gap-1 rounded-2xl bg-slate-100 p-1">
      {tabs.map((tab) => (
        <button
          key={tab.id}
          type="button"
          className={quickTabClass(active === tab.id)}
          onClick={() => onChange(tab.id)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}

const fieldLabelClass = "mb-2 block text-sm font-medium text-slate-700";

export function QuickAddressFields({
  value,
  onChange,
  inputClassName,
  cepLoading,
  cepMessage,
  cepError,
  onLookupCep,
}: {
  value: QuickAddressForm;
  onChange: (patch: Partial<QuickAddressForm>) => void;
  inputClassName: string;
  cepLoading: boolean;
  cepMessage: string;
  cepError: string;
  onLookupCep: (cep: string) => void;
}) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div>
        <label className={fieldLabelClass}>CEP</label>
        <input
          className={inputClassName}
          placeholder="00000-000"
          maxLength={9}
          value={value.cep}
          onChange={(e) => {
            const formatted = formatCep(e.target.value);
            onChange({ cep: formatted });
            if (onlyDigits(formatted).length === 8) onLookupCep(formatted);
          }}
        />
        {cepLoading ? <small className="mt-1 block text-xs text-slate-500">Consultando CEP...</small> : null}
        {!cepLoading && cepMessage ? (
          <small className="mt-1 block text-xs text-emerald-600">{cepMessage}</small>
        ) : null}
        {!cepLoading && cepError ? <small className="mt-1 block text-xs text-red-600">{cepError}</small> : null}
      </div>
      <div>
        <label className={fieldLabelClass}>Número</label>
        <input
          className={inputClassName}
          value={value.numero}
          onChange={(e) => onChange({ numero: e.target.value })}
        />
      </div>
      <div className="md:col-span-2">
        <label className={fieldLabelClass}>Logradouro</label>
        <input
          className={inputClassName}
          value={value.logradouro}
          onChange={(e) => onChange({ logradouro: e.target.value })}
        />
      </div>
      <div>
        <label className={fieldLabelClass}>Complemento</label>
        <input
          className={inputClassName}
          value={value.complemento}
          onChange={(e) => onChange({ complemento: e.target.value })}
        />
      </div>
      <div>
        <label className={fieldLabelClass}>Bairro</label>
        <input
          className={inputClassName}
          value={value.bairro}
          onChange={(e) => onChange({ bairro: e.target.value })}
        />
      </div>
      <div>
        <label className={fieldLabelClass}>Cidade</label>
        <input
          className={inputClassName}
          value={value.cidade}
          onChange={(e) => onChange({ cidade: e.target.value })}
        />
      </div>
      <div>
        <label className={fieldLabelClass}>UF</label>
        <input
          className={inputClassName}
          maxLength={2}
          placeholder="SP"
          value={value.uf}
          onChange={(e) => onChange({ uf: e.target.value.toUpperCase() })}
        />
      </div>
    </div>
  );
}
