type CategoryStatusBadgeProps = {
  isActive: boolean;
  hasImage: boolean;
};

/**
 * Status presentation: text + visual cue (not color alone).
 */
export function CategoryStatusBadge({
  isActive,
  hasImage,
}: CategoryStatusBadgeProps) {
  if (isActive && hasImage) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-primary/25 bg-accent/60 px-2 py-0.5 text-xs font-medium text-foreground">
        <span aria-hidden className="size-1.5 rounded-full bg-primary" />
        نشط · ظاهر في الكتالوج
      </span>
    );
  }

  if (isActive && !hasImage) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-border bg-muted/60 px-2 py-0.5 text-xs font-medium text-foreground">
        <span aria-hidden className="size-1.5 rounded-full bg-muted-foreground" />
        نشط · يحتاج صورة للظهور عاماً
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-1.5 rounded-md border border-border bg-card px-2 py-0.5 text-xs font-medium text-muted-foreground">
      <span
        aria-hidden
        className="size-1.5 rounded-full border border-muted-foreground"
      />
      غير نشط · مخفي عن الكتالوج
    </span>
  );
}
