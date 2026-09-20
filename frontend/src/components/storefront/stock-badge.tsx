type StockBadgeProps = {
  inStock: boolean;
  /** Optional secondary quantity text — keep visually quiet. */
  availableQuantity?: number;
  className?: string;
};

/**
 * Availability is primary; exact quantity stays secondary when provided.
 * Text + styling together (not color alone).
 */
export function StockBadge({
  inStock,
  availableQuantity,
  className = "",
}: StockBadgeProps) {
  return (
    <span
      className={`inline-flex flex-wrap items-baseline gap-x-2 gap-y-0.5 text-sm ${className}`}
    >
      <span
        className={
          inStock
            ? "font-medium text-accent-foreground"
            : "font-medium text-muted-foreground"
        }
      >
        {inStock ? "متوفر" : "غير متوفر حالياً"}
      </span>
      {inStock &&
      availableQuantity !== undefined &&
      Number.isFinite(availableQuantity) ? (
        <span className="text-xs text-muted-foreground">
          الكمية المتاحة:{" "}
          {new Intl.NumberFormat("ar-SY", { maximumFractionDigits: 3 }).format(
            availableQuantity,
          )}
        </span>
      ) : null}
    </span>
  );
}
