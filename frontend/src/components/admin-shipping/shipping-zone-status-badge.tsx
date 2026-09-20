type ShippingZoneStatusBadgeProps = {
  isActive: boolean;
};

/** Text + cue — not color alone. */
export function ShippingZoneStatusBadge({
  isActive,
}: ShippingZoneStatusBadgeProps) {
  if (isActive) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-primary/25 bg-accent/60 px-2 py-0.5 text-xs font-medium text-foreground">
        <span aria-hidden className="size-1.5 rounded-full bg-primary" />
        نشط · ظاهر في الدفع
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-1.5 rounded-md border border-border bg-card px-2 py-0.5 text-xs font-medium text-muted-foreground">
      <span
        aria-hidden
        className="size-1.5 rounded-full border border-muted-foreground"
      />
      غير نشط · مخفي عن الدفع
    </span>
  );
}
