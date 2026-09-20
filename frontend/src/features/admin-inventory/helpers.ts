import type { SellingUnit } from "@/features/catalog";
import { formatSellingUnit } from "@/lib/format";
import type { AdjustInventoryRequest } from "./types";

/** Backend inventory scale is decimal(18,3). */
export const INVENTORY_QUANTITY_MAX_FRACTION_DIGITS = 3;
export const INVENTORY_REASON_MAX_LENGTH = 500;

const stockQuantityFormatter = new Intl.NumberFormat("ar-SY", {
  maximumFractionDigits: INVENTORY_QUANTITY_MAX_FRACTION_DIGITS,
});

/** Display stock quantities without floating-point noise. */
export function formatStockQuantity(value: number): string {
  if (!Number.isFinite(value)) return "—";
  return stockQuantityFormatter.format(value);
}

export function formatStockWithUnit(
  value: number,
  sellingUnit: SellingUnit,
): string {
  return `${formatStockQuantity(value)} ${formatSellingUnit(sellingUnit)}`;
}

export function availabilityLabel(isInStock: boolean): string {
  return isInStock ? "متوفر" : "غير متوفر";
}

/** Preview resulting OnHand after a delta (client display only). */
export function previewOnHandAfterDelta(
  currentOnHand: number,
  quantityDelta: number,
): number {
  return currentOnHand + quantityDelta;
}

export function wouldViolateReserved(
  currentOnHand: number,
  reserved: number,
  quantityDelta: number,
): boolean {
  return previewOnHandAfterDelta(currentOnHand, quantityDelta) < reserved;
}

export function wouldBeNegative(
  currentOnHand: number,
  quantityDelta: number,
): boolean {
  return previewOnHandAfterDelta(currentOnHand, quantityDelta) < 0;
}

export function toAdjustInventoryRequest(
  quantityDelta: number,
  reason: string,
): AdjustInventoryRequest {
  return {
    quantityDelta,
    reason: reason.trim(),
  };
}
