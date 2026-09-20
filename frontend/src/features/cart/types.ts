import type { SellingUnit } from "@/features/catalog";

/**
 * Frontend-only cart line. Not a business authority.
 *
 * Backend checkout later receives variantId + quantity only.
 * Prices/names/images here are display snapshots that may become stale.
 */
export type CartItem = {
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  /** May be null when the product has no usable image URL. */
  primaryImageUrl: string | null;
  sku: string;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  quantity: number;
  /**
   * Display-only unit price snapshot from the last add/update.
   * NOT authoritative — Checkout Preview (F5) revalidates prices.
   */
  lastKnownUnitPrice: number;
};

/** Current persisted cart document version. */
export const CART_PERSISTENCE_VERSION = 1 as const;

export type CartPersistenceVersion = typeof CART_PERSISTENCE_VERSION;

/**
 * Persisted cart document shape (inside Zustand partialize / custom storage).
 * Untrusted browser input — validate on read.
 */
export type PersistedCartV1 = {
  version: 1;
  items: CartItem[];
};

export type CartState = {
  items: CartItem[];
  /** True after client rehydration from localStorage completes. */
  hasHydrated: boolean;
};

/** Input for adding/updating a cart line from live catalog data. */
export type AddToCartInput = {
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  primaryImageUrl: string | null;
  sku: string;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  /** Optional; defaults to quantityIncrement. */
  quantity?: number;
  /** Live catalog unit price — stored as lastKnownUnitPrice. */
  unitPrice: number;
  /**
   * Current-known stock from product detail at add time.
   * Used only for immediate client-side guards; not persisted as truth.
   */
  isInStock: boolean;
  availableQuantity?: number;
};

export type AddToCartResult =
  | { ok: true; item: CartItem }
  | { ok: false; reason: AddToCartFailureReason };

export type AddToCartFailureReason =
  | "out_of_stock"
  | "invalid_quantity"
  | "exceeds_available";

export const CART_STORAGE_KEY = "electricalstore.cart";
