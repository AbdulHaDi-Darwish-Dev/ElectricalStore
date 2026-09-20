import type { SellingUnit } from "@/features/catalog";

/** GET /admin/inventory item — matches InventoryItemDto. */
export type AdminInventoryItemDto = {
  productId: string;
  productName: string;
  variantId: string;
  variantName: string;
  sku: string;
  sellingUnit: SellingUnit;
  onHand: number;
  reserved: number;
  available: number;
  isInStock: boolean;
};

/** POST /admin/inventory/{variantId}/adjust body — delta, not absolute. */
export type AdjustInventoryRequest = {
  quantityDelta: number;
  reason: string;
};

export type AdminInventoryListParams = {
  productId?: string;
  categoryId?: string;
  search?: string;
  /** true = Available > 0; false = Available <= 0; omit = all */
  inStock?: boolean;
};
