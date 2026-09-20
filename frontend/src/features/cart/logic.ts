import type { AddToCartInput, AddToCartResult, CartItem } from "./types";
import {
  clampToAvailable,
  defaultQuantity,
  isValidQuantity,
  normalizeQuantity,
  roundQuantity,
} from "./quantity";

/**
 * Pure add/merge for a cart line.
 *
 * Rules:
 * - Out-of-stock variants cannot be added (known at add time).
 * - Same variantId merges: existing quantity + requested quantity.
 * - Snapshot display fields refresh from the newest AddToCartInput.
 * - availableQuantity clamps only when provided at add time (not persisted truth).
 */
export function mergeAddToCart(
  items: readonly CartItem[],
  input: AddToCartInput,
): { result: AddToCartResult; items: CartItem[] } {
  if (!input.isInStock) {
    return { result: { ok: false, reason: "out_of_stock" }, items: [...items] };
  }

  const increment = input.quantityIncrement;
  if (!Number.isFinite(increment) || increment <= 0) {
    return {
      result: { ok: false, reason: "invalid_quantity" },
      items: [...items],
    };
  }

  const requestedRaw =
    input.quantity === undefined ? defaultQuantity(increment) : input.quantity;
  const requested = normalizeQuantity(requestedRaw, increment);

  if (!isValidQuantity(requested, increment)) {
    return {
      result: { ok: false, reason: "invalid_quantity" },
      items: [...items],
    };
  }

  const existing = items.find((item) => item.variantId === input.variantId);
  const combined = existing
    ? normalizeQuantity(existing.quantity + requested, increment)
    : requested;

  if (
    input.availableQuantity !== undefined &&
    Number.isFinite(input.availableQuantity)
  ) {
    if (existing && existing.quantity >= input.availableQuantity) {
      return {
        result: { ok: false, reason: "exceeds_available" },
        items: [...items],
      };
    }
  }

  const clamped = clampToAvailable(combined, increment, input.availableQuantity);

  if (!Number.isFinite(clamped) || clamped <= 0) {
    return {
      result: { ok: false, reason: "exceeds_available" },
      items: [...items],
    };
  }

  // No room to add anything beyond current line.
  if (existing && clamped <= existing.quantity && requested > 0) {
    if (
      input.availableQuantity !== undefined &&
      clamped >= input.availableQuantity
    ) {
      return {
        result: { ok: false, reason: "exceeds_available" },
        items: [...items],
      };
    }
  }

  const nextItem: CartItem = {
    productId: input.productId,
    variantId: input.variantId,
    productName: input.productName,
    variantName: input.variantName,
    primaryImageUrl: input.primaryImageUrl,
    sku: input.sku,
    sellingUnit: input.sellingUnit,
    quantityIncrement: increment,
    quantity: clamped,
    lastKnownUnitPrice: roundQuantity(input.unitPrice, 0.01),
  };

  const without = items.filter((item) => item.variantId !== input.variantId);
  return {
    result: { ok: true, item: nextItem },
    items: [...without, nextItem],
  };
}

export function setCartItemQuantity(
  items: readonly CartItem[],
  variantId: string,
  quantity: number,
): CartItem[] | null {
  const index = items.findIndex((item) => item.variantId === variantId);
  if (index < 0) {
    return null;
  }
  const current = items[index]!;
  if (!isValidQuantity(quantity, current.quantityIncrement)) {
    return null;
  }
  const next = [...items];
  next[index] = {
    ...current,
    quantity: normalizeQuantity(quantity, current.quantityIncrement),
  };
  return next;
}

export function removeCartItem(
  items: readonly CartItem[],
  variantId: string,
): CartItem[] {
  return items.filter((item) => item.variantId !== variantId);
}
