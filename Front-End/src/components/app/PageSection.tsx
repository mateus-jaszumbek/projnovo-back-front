type PageSectionProps = {
  title?: string;
  description?: string;
  actions?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
};

export function PageSection({
  title,
  description,
  actions,
  children,
  className,
}: PageSectionProps) {
  return (
    <section
      className={[
        "app-panel overflow-hidden p-5",
        className ?? "",
      ].join(" ")}
    >
      {title || description || actions ? (
        <div className="mb-5 flex flex-col gap-3 border-b border-emerald-100/80 pb-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0">
            {title ? (
              <h2 className="text-base font-semibold text-slate-950">{title}</h2>
            ) : null}

            {description ? (
              <p className="mt-1 text-sm leading-6 text-slate-600">{description}</p>
            ) : null}
          </div>

          {actions ? (
            <div className="flex shrink-0 flex-wrap items-center gap-2 lg:justify-end">
              {actions}
            </div>
          ) : null}
        </div>
      ) : null}

      {children}
    </section>
  );
}
