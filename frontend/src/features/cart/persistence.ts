import { z } from "zod";
import {
  CART_PERSISTENCE_VERSION,
  type CartItem,
  type PersistedCartV1,
} from "./types";
import { isValidQuantity, roundQuantity } from "./quantity";

const sellingUnitSchema = z.enum(["Piece", "Meter"]);

const cartItemSchema = z.object({
  productId: z.string().min(1),
  variantId: z.string().min(1),
  productName: z.string().min(1),
  variantName: z.string().min(1),
  primaryImageUrl: z.string().min(1).nullable(),
  sku: z.string().min(1),
  sellingUnit: sellingUnitSchema,
  quantityIncrement: z.number().finite().positive(),
  quantity: z.number().finite().positive(),
  lastKnownUnitPrice: z.number().finite().nonnegative(),
});

export const persistedCartV1Schema = z.object({
  version: z.literal(CART_PERSISTENCE_VERSION),
  items: z.array(cartItemSchema),
});

/**
 * Sanitize a candidate cart item.
 * Drops invalid lines; does not invent data.
 */
export function sanitizeCartItem(raw: unknown): CartItem | null {
  const parsed = cartItemSchema.safeParse(raw);
  if (!parsed.success) {
    return null;
  }
  const item = parsed.data;
  if (!isValidQuantity(item.quantity, item.quantityIncrement)) {
    return null;
  }
  return {
    ...item,
    quantity: roundQuantity(item.quantity, item.quantityIncrement),
    lastKnownUnitPrice: roundQuantity(item.lastKnownUnitPrice, 0.01),
    primaryImageUrl: item.primaryImageUrl,
  };
}

/**
 * Parse/migrate persisted cart JSON.
 * Invalid or unmigratable payloads → empty cart (documented F4 behavior).
 */
export function parsePersistedCart(raw: unknown): PersistedCartV1 {
  const empty: PersistedCartV1 = {
    version: CART_PERSISTENCE_VERSION,
    items: [],
  };

  if (raw === null || raw === undefined) {
    return empty;
  }

  // Zustand persist may wrap as { state: { items }, version } — handle both.
  let candidate = raw;
  if (
    typeof raw === "object" &&
    raw !== null &&
    "state" in raw &&
    typeof (raw as { state: unknown }).state === "object"
  ) {
    const wrapped = raw as { state: Record<string, unknown>; version?: number };
    if ("items" in wrapped.state && !("version" in wrapped.state)) {
      candidate = {
        version: CART_PERSISTENCE_VERSION,
        items: wrapped.state.items,
      };
    } else {
      candidate = wrapped.state;
    }
  }

  if (
    typeof candidate === "object" &&
    candidate !== null &&
    "version" in candidate &&
    (candidate as { version: unknown }).version !== CART_PERSISTENCE_VERSION
  ) {
    // Future versions: explicit migrations land here. Unknown → reset.
    return empty;
  }

  const parsed = persistedCartV1Schema.safeParse(candidate);
  if (!parsed.success) {
    // Try to salvage items array if version wrapper is wrong but items exist.
    if (
      typeof candidate === "object" &&
      candidate !== null &&
      Array.isArray((candidate as { items?: unknown }).items)
    ) {
      const items = (candidate as { items: unknown[] }).items
        .map(sanitizeCartItem)
        .filter((item): item is CartItem => item !== null);
      // Dedupe by variantId (keep last occurrence).
      const byVariant = new Map<string, CartItem>();
      for (const item of items) {
        byVariant.set(item.variantId, item);
      }
      return {
        version: CART_PERSISTENCE_VERSION,
        items: [...byVariant.values()],
      };
    }
    return empty;
  }

  const byVariant = new Map<string, CartItem>();
  for (const rawItem of parsed.data.items) {
    const item = sanitizeCartItem(rawItem);
    if (item) {
      byVariant.set(item.variantId, item);
    }
  }

  return {
    version: CART_PERSISTENCE_VERSION,
    items: [...byVariant.values()],
  };
}
