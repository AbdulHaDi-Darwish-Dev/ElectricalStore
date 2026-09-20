import type { ReactNode } from "react";

type AdminErrorStateProps = {
  title?: string;
  message: string;
  actions?: ReactNode;
};

export function AdminErrorState({
  title = "تعذر إكمال العملية",
  message,
  actions,
}: AdminErrorStateProps) {
  return (
    <div
      className="rounded-md border border-border bg-card px-4 py-6"
      role="alert"
    >
      <p className="font-medium text-foreground">{title}</p>
      <p className="mt-2 text-sm text-muted-foreground">{message}</p>
      {actions ? (
        <div className="mt-4 flex flex-wrap gap-2">{actions}</div>
      ) : null}
    </div>
  );
}
