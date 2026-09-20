import type { SellingUnit } from "@/features/catalog";
import type {
  OrderStatusCode,
  PaymentMethodCode,
  PaymentStatusCode,
} from "@/features/orders";

/** Matches Order.CancellationReasonMaxLength. */
export const ORDER_CANCELLATION_REASON_MAX_LENGTH = 500;

export type AdminOrderListItemDto = {
  id: string;
  orderNumber: string;
  status: string;
  paymentStatus: string;
  customerName: string;
  phone: string;
  total: number;
  createdAtUtc: string;
};

export type AdminOrderItemDto = {
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

export type AdminOrderModificationAuditDto = {
  id: string;
  performedByUserId: string;
  reason: string;
  summary: string;
  createdAtUtc: string;
};

/** Admin GET detail — OrderDto with modificationAudits. Never expects guestAccessToken. */
export type AdminOrderDto = {
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
  items: AdminOrderItemDto[];
  modificationAudits?: AdminOrderModificationAuditDto[] | null;
};

export type AdminOrderListParams = {
  status?: OrderStatusCode;
  paymentStatus?: PaymentStatusCode;
  search?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
};

export type CancelAdminOrderRequest = {
  reason: string;
};

export type {
  OrderStatusCode,
  PaymentStatusCode,
  PaymentMethodCode,
};

/** Explicit lifecycle actions (not a free-form status dropdown). */
export type AdminOrderAction =
  | "confirm"
  | "prepare"
  | "outForDelivery"
  | "deliver"
  | "markPaid"
  | "cancel";
