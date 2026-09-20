type ProductStatusBadgeProps = {
  isActive: boolean;
  likelyPublic?: boolean | null;
};

export function ProductStatusBadge({
  isActive,
  likelyPublic,
}: ProductStatusBadgeProps) {
  if (!isActive) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-border bg-card px-2 py-0.5 text-xs font-medium text-muted-foreground">
        <span
          aria-hidden
          className="size-1.5 rounded-full border border-muted-foreground"
        />
        غير نشط
      </span>
    );
  }

  if (likelyPublic === true) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-primary/25 bg-accent/60 px-2 py-0.5 text-xs font-medium text-foreground">
        <span aria-hidden className="size-1.5 rounded-full bg-primary" />
        نشط · جاهز للظهور
      </span>
    );
  }

  if (likelyPublic === false) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-border bg-muted/60 px-2 py-0.5 text-xs font-medium text-foreground">
        <span aria-hidden className="size-1.5 rounded-full bg-muted-foreground" />
        نشط · غير ظاهر في المتجر
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-1.5 rounded-md border border-primary/25 bg-accent/40 px-2 py-0.5 text-xs font-medium text-foreground">
      <span aria-hidden className="size-1.5 rounded-full bg-primary" />
      نشط
    </span>
  );
}
