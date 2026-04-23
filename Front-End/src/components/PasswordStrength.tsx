import { CheckCircle2, Circle } from "lucide-react";
import { passwordChecks, passwordStrength } from "./uiHelpers";

type PasswordStrengthProps = {
  value: unknown;
  confirmValue?: unknown;
  showMatch?: boolean;
};

const labels = [
  { key: "minLength", label: "Mais de 6 caracteres" },
  { key: "uppercase", label: "Maiúscula" },
  { key: "lowercase", label: "Minúscula" },
  { key: "number", label: "Número" },
] as const;

export function PasswordStrength({
  value,
  confirmValue,
  showMatch,
}: PasswordStrengthProps) {
  const checks = passwordChecks(value);
  const strength = passwordStrength(value);
  const matches = showMatch ? String(value ?? "") === String(confirmValue ?? "") && Boolean(value) : true;

  return (
    <div className={`password-strength password-${strength.tone}`}>
      <div className="password-meter" aria-hidden="true">
        <span style={{ width: `${Math.max(strength.score, 1) * 20}%` }} />
      </div>
      <div className="password-strength-top">
        <strong>Senha {strength.label.toLowerCase()}</strong>
        {showMatch ? <small>{matches ? "Senhas iguais" : "Senhas diferentes"}</small> : null}
      </div>
      <div className="password-rules">
        {labels.map((item) => {
          const ok = checks[item.key];
          const Icon = ok ? CheckCircle2 : Circle;

          return (
            <span key={item.key} className={ok ? "ok" : ""}>
              <Icon size={14} />
              {item.label}
            </span>
          );
        })}
      </div>
    </div>
  );
}
