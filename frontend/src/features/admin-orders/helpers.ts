import type { SellingUnit } from "@/features/catalog";
import {
  formatOrderStatus,
  formatPaymentMethod,
  formatPaymentStatus,
} from "@/features/orders";
import { formatSellingUnit } from "@/lib/format";
import type { AdminOrderAction, OrderStatusCode, PaymentStatusCode } from "./types";
import { ORDER_CANCELLATION_REASON_MAX_LENGTH } from "./types";

export {
  formatOrderStatus,
  formatPaymentMethod,
  formatPaymentStatus,
  ORDER_CANCELLATION_REASON_MAX_LENGTH,
};

const quantityFormatter = new Intl.NumberFormat("ar-SY", {
  maximumFractionDigits: 3,
});

const dateTimeFormatter = new Intl.DateTimeFormat("ar-SY", {
  dateStyle: "medium",
  timeStyle: "short",
});

export function formatOrderQuantity(value: number): string {
  if (!Number.isFinite(value)) return "—";
  return quantityFormatter.format(value);
}

export function formatOrderQuantityWithUnit(
  value: number,
  sellingUnit: SellingUnit,
): string {
  return `${formatOrderQuantity(value)} ${formatSellingUnit(sellingUnit)}`;
}

/** Parse backend UTC ISO and present in the operator's local timezone. */
export function formatOrderDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "—";
  return dateTimeFormatter.format(d);
}

/**
 * Allowed Admin actions for the current fulfillment + payment state.
 * Mirrors domain Order methods — not enum order.
 */
export function getAvailableAdminOrderActions(
  status: string,
  paymentStatus: string,
): AdminOrderAction[] {
  const actions: AdminOrderAction[] = [];

  switch (status as OrderStatusCode) {
    case "PendingConfirmation":
      actions.push("confirm", "cancel");
      break;
    case "Confirmed":
      actions.push("prepare", "cancel");
      break;
    case "Preparing":
      actions.push("outForDelivery", "cancel");
      break;
    case "OutForDelivery":
      actions.push("deliver");
      break;
    case "Delivered":
    case "Cancelled":
      break;
    default:
      break;
  }

  if (
    paymentStatus === ("Unpaid" satisfies PaymentStatusCode) &&
    (status === "OutForDelivery" || status === "Delivered")
  ) {
    actions.push("markPaid");
  }

  return actions;
}

/** Transitions that mutate inventory on the backend. */
export function actionAffectsInventory(action: AdminOrderAction): boolean {
  return (
    action === "confirm" ||
    action === "outForDelivery" ||
    action === "cancel"
  );
}

export function cancelReleasesReservation(status: string): boolean {
  return status === "Confirmed" || status === "Preparing";
}

export const ORDER_STATUS_FILTER_OPTIONS: {
  value: OrderStatusCode | "";
  label: string;
}[] = [
  { value: "", label: "كل الحالات" },
  { value: "PendingConfirmation", label: formatOrderStatus("PendingConfirmation") },
  { value: "Confirmed", label: formatOrderStatus("Confirmed") },
  { value: "Preparing", label: formatOrderStatus("Preparing") },
  { value: "OutForDelivery", label: formatOrderStatus("OutForDelivery") },
  { value: "Delivered", label: formatOrderStatus("Delivered") },
  { value: "Cancelled", label: formatOrderStatus("Cancelled") },
];

export const PAYMENT_STATUS_FILTER_OPTIONS: {
  value: PaymentStatusCode | "";
  label: string;
}[] = [
  { value: "", label: "كل حالات الدفع" },
  { value: "Unpaid", label: formatPaymentStatus("Unpaid") },
  { value: "Paid", label: formatPaymentStatus("Paid") },
];

export function actionLabel(action: AdminOrderAction): string {
  switch (action) {
    case "confirm":
      return "تأكيد الطلب";
    case "prepare":
      return "بدء التجهيز";
    case "outForDelivery":
      return "إرسال للتوصيل";
    case "deliver":
      return "تأكيد التسليم";
    case "markPaid":
      return "تسجيل الدفع";
    case "cancel":
      return "إلغاء الطلب";
  }
}
