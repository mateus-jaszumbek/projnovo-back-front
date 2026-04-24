import type { LucideIcon } from "lucide-react";

type StatCardProps = {
  title: string;
  value: string | number;
  description?: string;
  icon?: LucideIcon;
  tone?: "default" | "success" | "warning" | "danger";
};

function toneClasses(tone: StatCardProps["tone"]) {
  switch (tone) {
    case "success":
      return "border border-emerald-200/80 bg-emerald-50 text-emerald-700";
    case "warning":
      return "border border-amber-200/80 bg-amber-50 text-amber-700";
    case "danger":
      return "border border-rose-200/80 bg-rose-50 text-rose-700";
    default:
      return "border border-cyan-200/80 bg-cyan-50 text-cyan-700";
  }
}

export function StatCard({
  title,
  value,
  description,
  icon: Icon,
  tone = "default",
}: StatCardProps) {
  return (
    <div className="app-panel p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <span className="block text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-500">
            {title}
          </span>
          <strong className="mt-3 block text-3xl font-semibold tracking-tight text-slate-950">
            {value}
          </strong>

          {description ? (
            <p className="mt-2 text-sm leading-6 text-slate-600">{description}</p>
          ) : null}
        </div>

        {Icon ? (
          <div
            className={[
              "flex h-12 w-12 shrink-0 items-center justify-center rounded-lg shadow-sm",
              toneClasses(tone),
            ].join(" ")}
          >
            <Icon size={22} />
          </div>
        ) : null}
      </div>
    </div>
  );
}
