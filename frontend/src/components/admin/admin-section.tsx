import type { ReactNode } from "react";

type AdminSectionProps = {
  title?: string;
  description?: string;
  children: ReactNode;
};

export function AdminSection({ title, description, children }: AdminSectionProps) {
  return (
    <section className="space-y-3">
      {title || description ? (
        <div className="space-y-1">
          {title ? (
            <h2 className="text-base font-semibold text-foreground">{title}</h2>
          ) : null}
          {description ? (
            <p className="text-sm text-muted-foreground">{description}</p>
          ) : null}
        </div>
      ) : null}
      {children}
    </section>
  );
}
