import Link from "next/link";

type EmptyStateProps = {
  title: string;
  description?: string;
  actionHref?: string;
  actionLabel?: string;
};

export function EmptyState({
  title,
  description,
  actionHref,
  actionLabel,
}: EmptyStateProps) {
  return (
    <div
      role="status"
      className="flex flex-col items-start gap-3 rounded-md border border-dashed border-border bg-muted/40 px-5 py-10"
    >
      <h2 className="text-lg font-medium text-foreground">{title}</h2>
      {description ? (
        <p className="max-w-lg text-sm leading-6 text-muted-foreground">
          {description}
        </p>
      ) : null}
      {actionHref && actionLabel ? (
        <Link
          href={actionHref}
          className="mt-1 rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground hover:opacity-90"
        >
          {actionLabel}
        </Link>
      ) : null}
    </div>
  );
}
