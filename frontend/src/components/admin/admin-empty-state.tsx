import type { ReactNode } from "react";

type AdminEmptyStateProps = {
  title: string;
  description?: string;
  actions?: ReactNode;
};

export function AdminEmptyState({
  title,
  description,
  actions,
}: AdminEmptyStateProps) {
  return (
    <div className="rounded-md border border-dashed border-border bg-card px-4 py-10 text-center">
      <p className="font-medium text-foreground">{title}</p>
      {description ? (
        <p className="mx-auto mt-2 max-w-md text-sm text-muted-foreground">
          {description}
        </p>
      ) : null}
      {actions ? (
        <div className="mt-4 flex flex-wrap items-center justify-center gap-2">
          {actions}
        </div>
      ) : null}
    </div>
  );
}
