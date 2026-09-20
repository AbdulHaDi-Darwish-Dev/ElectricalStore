import type { CartItem } from "@/features/cart";
import type { CheckoutLineRequest, CheckoutPreviewRequest } from "./types";

/** Cart → Preview items (variantId + quantity only — no prices/names). */
export function cartItemsToCheckoutLines(
  items: readonly CartItem[],
): CheckoutLineRequest[] {
  return items.map((item) => ({
    variantId: item.variantId,
    quantity: item.quantity,
  }));
}

export function buildCheckoutPreviewRequest(
  items: readonly CartItem[],
  deliveryZoneId: string,
): CheckoutPreviewRequest {
  return {
    items: cartItemsToCheckoutLines(items),
    deliveryZoneId,
  };
}

/**
 * Stable Preview query key inputs — presentation snapshots excluded.
 * Customer form fields intentionally omitted.
 */
export function buildCheckoutPreviewQueryKey(
  items: readonly CartItem[],
  deliveryZoneId: string | null | undefined,
) {
  const lines = cartItemsToCheckoutLines(items)
    .map((line) => `${line.variantId}:${line.quantity}`)
    .sort();
  return ["checkout-preview", deliveryZoneId ?? "", ...lines] as const;
}

export function hasPriceChanged(
  lastKnownUnitPrice: number,
  previewUnitPrice: number,
): boolean {
  if (!Number.isFinite(lastKnownUnitPrice) || !Number.isFinite(previewUnitPrice)) {
    return false;
  }
  return Math.abs(lastKnownUnitPrice - previewUnitPrice) > 0.009;
}
