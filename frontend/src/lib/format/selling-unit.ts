import type { SellingUnit } from "@/features/catalog";

const SELLING_UNIT_LABELS: Record<SellingUnit, string> = {
  Piece: "قطعة",
  Meter: "متر",
};

/** Presentation mapper — does not alter serialized contract values. */
export function formatSellingUnit(unit: SellingUnit): string {
  return SELLING_UNIT_LABELS[unit] ?? unit;
}

/** e.g. "يُباع بزيادة 0.5 متر" */
export function formatQuantityIncrement(
  increment: number,
  unit: SellingUnit,
): string {
  if (!Number.isFinite(increment) || increment <= 0) {
    return formatSellingUnit(unit);
  }

  const formatted = new Intl.NumberFormat("ar-SY", {
    maximumFractionDigits: 3,
  }).format(increment);

  return `يُباع بزيادة ${formatted} ${formatSellingUnit(unit)}`;
}
