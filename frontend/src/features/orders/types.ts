import type { SellingUnit } from "@/features/catalog";
import type { CheckoutLineRequest } from "@/features/checkout";

/**
 * Place Order + Order DTOs — camelCase JSON from ASP.NET Core.
 * Matches OrderingDtos.cs. No invented fields.
 */

export type PlaceOrderRequest = {
  items: CheckoutLineRequest[];
  deliveryZoneId: string;
  customerName: string;
  phone: string;
  addressText: string;
  customerNote?: string | null;
};

export type OrderItemDto = {
  id: string;
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  sku: string;
  sellingUnit: SellingUnit;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

/**
 * Public OrderDto. After Place Order through the Next security boundary,
 * `guestAccessToken` is stripped before reaching browser JS.
 */
export type OrderDto = {
  id: string;
  orderNumber: string;
  status: string;
  paymentMethod: string;
  paymentStatus: string;
  customerName: string;
  phone: string;
  addressText: string;
  customerNote: string | null;
  deliveryZoneId: string;
  deliveryZoneName: string;
  shippingFee: number;
  merchandiseSubtotal: number;
  appliedMinimumOrderAmount: number;
  total: number;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  preparingAtUtc: string | null;
  outForDeliveryAtUtc: string | null;
  deliveredAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
  items: OrderItemDto[];
  /** Raw guest bearer — never persist in browser storage; BFF strips on place. */
  guestAccessToken?: string | null;
  trackingHint?: string | null;
};

export const IDEMPOTENCY_KEY_HEADER = "Idempotency-Key";
export const GUEST_ORDER_TOKEN_HEADER = "X-Order-Token";

/** Backend OrderStatus.ToString() values. */
export type OrderStatusCode =
  | "PendingConfirmation"
  | "Confirmed"
  | "Preparing"
  | "OutForDelivery"
  | "Delivered"
  | "Cancelled";

export type PaymentStatusCode = "Unpaid" | "Paid";

export type PaymentMethodCode = "CashOnDelivery";
