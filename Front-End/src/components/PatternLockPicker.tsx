import { useMemo } from "react";

type PatternLockPickerProps = {
  value: string;
  onChange: (value: string) => void;
};

const DOT_POSITIONS = [1, 2, 3, 4, 5, 6, 7, 8, 9].map((dot) => {
  const index = dot - 1;
  const col = index % 3;
  const row = Math.floor(index / 3);
  return { dot, x: col * 80 + 40, y: row * 80 + 40 };
});

function parseSequence(value: string): number[] {
  return value
    .split(/[,\-\s]+/)
    .map((part) => Number(part))
    .filter((n) => Number.isInteger(n) && n >= 1 && n <= 9);
}

export function PatternLockPicker({ value, onChange }: PatternLockPickerProps) {
  const sequence = useMemo(() => parseSequence(value), [value]);

  function clicarPonto(dot: number) {
    if (sequence.includes(dot)) return;
    onChange([...sequence, dot].join("-"));
  }

  function limpar() {
    onChange("");
  }

  const posicaoPorDot = new Map(DOT_POSITIONS.map((p) => [p.dot, p]));

  return (
    <div className="inline-flex flex-col items-start gap-3">
      <svg width={240} height={240} className="rounded-2xl border border-slate-200 bg-slate-50">
        {sequence.slice(1).map((dot, index) => {
          const from = posicaoPorDot.get(sequence[index])!;
          const to = posicaoPorDot.get(dot)!;
          return (
            <line
              key={`${sequence[index]}-${dot}`}
              x1={from.x}
              y1={from.y}
              x2={to.x}
              y2={to.y}
              stroke="#0f172a"
              strokeWidth={4}
              strokeLinecap="round"
            />
          );
        })}

        {DOT_POSITIONS.map(({ dot, x, y }) => {
          const ordem = sequence.indexOf(dot);
          const marcado = ordem >= 0;

          return (
            <g key={dot} onClick={() => clicarPonto(dot)} className="cursor-pointer">
              <circle
                cx={x}
                cy={y}
                r={16}
                fill={marcado ? "#0f172a" : "#ffffff"}
                stroke="#94a3b8"
                strokeWidth={2}
              />
              {marcado ? (
                <text
                  x={x}
                  y={y + 5}
                  textAnchor="middle"
                  fontSize={12}
                  fontWeight={600}
                  fill="#ffffff"
                >
                  {ordem + 1}
                </text>
              ) : null}
            </g>
          );
        })}
      </svg>

      <div className="flex items-center gap-3">
        <button
          type="button"
          className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
          onClick={limpar}
        >
          Limpar e redesenhar
        </button>
        <span className="text-xs text-slate-500">
          {sequence.length > 0 ? `${sequence.length} pontos marcados` : "Clique nos pontos em ordem"}
        </span>
      </div>
    </div>
  );
}
