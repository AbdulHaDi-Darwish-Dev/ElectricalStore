import type { SellingUnit } from "@/features/catalog/types";

/**
 * Checkout preview DTOs — POST /checkout/preview only.
 * Place Order belongs to a later phase.
 */

export type CheckoutLineRequest = {
  variantId: string;
  quantity: number;
};

export type CheckoutPreviewRequest = {
  items: CheckoutLineRequest[];
  deliveryZoneId: string;
};

/** Preview line — matches backend CheckoutLineDto JSON. */
export type CheckoutPreviewItemDto = {
  productId: string;
  productName: string;
  primaryImageUrl: string | null;
  variantId: string;
  variantName: string;
  sku: string;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  quantity: number;
  unitPrice: number;
  availableQuantity: number;
  lineTotal: number;
};

export type CheckoutPreviewDto = {
  items: CheckoutPreviewItemDto[];
  deliveryZoneId: string;
  deliveryZoneName: string;
  shippingFee: number;
  merchandiseSubtotal: number;
  appliedMinimumOrderAmount: number;
  total: number;
  meetsMinimumOrder: boolean;
};
