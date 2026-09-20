import type { OrderStatusCode, PaymentMethodCode, PaymentStatusCode } from "./types";

const ORDER_STATUS_LABELS: Record<string, string> = {
  PendingConfirmation: "بانتظار التأكيد",
  Confirmed: "مؤكد",
  Preparing: "قيد التجهيز",
  OutForDelivery: "خارج للتوصيل",
  Delivered: "تم التسليم",
  Cancelled: "ملغى",
};

const PAYMENT_STATUS_LABELS: Record<string, string> = {
  Unpaid: "غير مدفوع",
  Paid: "مدفوع",
};

const PAYMENT_METHOD_LABELS: Record<string, string> = {
  CashOnDelivery: "الدفع عند الاستلام",
};

export function formatOrderStatus(status: string): string {
  return ORDER_STATUS_LABELS[status] ?? status;
}

export function formatPaymentStatus(status: string): string {
  return PAYMENT_STATUS_LABELS[status] ?? status;
}

export function formatPaymentMethod(method: string): string {
  return PAYMENT_METHOD_LABELS[method] ?? method;
}

export function isKnownOrderStatus(status: string): status is OrderStatusCode {
  return status in ORDER_STATUS_LABELS;
}

export function isKnownPaymentStatus(status: string): status is PaymentStatusCode {
  return status in PAYMENT_STATUS_LABELS;
}

export function isKnownPaymentMethod(method: string): method is PaymentMethodCode {
  return method in PAYMENT_METHOD_LABELS;
}
