import { useMemo, useRef, useState } from "react";

type PatternLockPickerProps = {
  value: string;
  onChange: (value: string) => void;
  readOnly?: boolean;
};

const DOT_RADIUS = 16;
const HIT_RADIUS = 28;
const SIZE = 240;

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

function distancia(ax: number, ay: number, bx: number, by: number) {
  return Math.hypot(ax - bx, ay - by);
}

export function PatternLockPicker({ value, onChange, readOnly }: PatternLockPickerProps) {
  const sequence = useMemo(() => parseSequence(value), [value]);
  const svgRef = useRef<SVGSVGElement>(null);
  const [dragging, setDragging] = useState(false);
  const [cursor, setCursor] = useState<{ x: number; y: number } | null>(null);

  const posicaoPorDot = new Map(DOT_POSITIONS.map((p) => [p.dot, p]));

  function pontoLocal(event: { clientX: number; clientY: number }) {
    const rect = svgRef.current?.getBoundingClientRect();
    if (!rect) return { x: 0, y: 0 };

    return {
      x: ((event.clientX - rect.left) / rect.width) * SIZE,
      y: ((event.clientY - rect.top) / rect.height) * SIZE,
    };
  }

  function dotSobPonteiro(x: number, y: number) {
    return DOT_POSITIONS.find((p) => distancia(p.x, p.y, x, y) <= HIT_RADIUS)?.dot;
  }

  function iniciarNoPonto(dot: number) {
    if (sequence.includes(dot)) {
      const posicao = sequence.indexOf(dot);
      onChange(sequence.slice(0, posicao).join("-"));
      return false;
    }

    onChange([dot].join("-"));
    return true;
  }

  function handlePointerDown(event: React.PointerEvent<SVGSVGElement>) {
    if (readOnly) return;

    const { x, y } = pontoLocal(event);
    const dot = dotSobPonteiro(x, y);
    if (dot === undefined) return;

    const iniciouNovoTraco = iniciarNoPonto(dot);
    setCursor({ x, y });
    if (iniciouNovoTraco) {
      svgRef.current?.setPointerCapture(event.pointerId);
      setDragging(true);
    }
  }

  function handlePointerMove(event: React.PointerEvent<SVGSVGElement>) {
    if (!dragging) return;

    const { x, y } = pontoLocal(event);
    setCursor({ x, y });

    const dot = dotSobPonteiro(x, y);
    if (dot !== undefined && !sequence.includes(dot)) {
      onChange([...sequence, dot].join("-"));
    }
  }

  function handlePointerUp(event: React.PointerEvent<SVGSVGElement>) {
    if (svgRef.current?.hasPointerCapture(event.pointerId)) {
      svgRef.current.releasePointerCapture(event.pointerId);
    }
    setDragging(false);
    setCursor(null);
  }

  function limpar() {
    onChange("");
  }

  return (
    <div className="inline-flex flex-col items-start gap-3">
      <svg
        ref={svgRef}
        width={SIZE}
        height={SIZE}
        className="touch-none select-none rounded-2xl border border-slate-200 bg-slate-50"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerCancel={handlePointerUp}
      >
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

        {dragging && cursor && sequence.length > 0
          ? (() => {
              const from = posicaoPorDot.get(sequence[sequence.length - 1])!;
              return (
                <line
                  x1={from.x}
                  y1={from.y}
                  x2={cursor.x}
                  y2={cursor.y}
                  stroke="#0f172a"
                  strokeWidth={4}
                  strokeLinecap="round"
                />
              );
            })()
          : null}

        {DOT_POSITIONS.map(({ dot, x, y }) => {
          const ordem = sequence.indexOf(dot);
          const marcado = ordem >= 0;

          return (
            <g key={dot}>
              <circle
                cx={x}
                cy={y}
                r={DOT_RADIUS}
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
                  className="pointer-events-none"
                >
                  {ordem + 1}
                </text>
              ) : null}
            </g>
          );
        })}
      </svg>

      {readOnly ? null : (
        <div className="flex items-center gap-3">
          <button
            type="button"
            className="inline-flex items-center justify-center rounded-2xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            onClick={limpar}
          >
            Limpar e redesenhar
          </button>
          <span className="text-xs text-slate-500">
            {sequence.length > 0
              ? `${sequence.length} pontos marcados`
              : "Arraste pelos pontos para desenhar o padrão"}
          </span>
        </div>
      )}
    </div>
  );
}
