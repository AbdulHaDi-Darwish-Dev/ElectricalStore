"use client";

import {
  formatOrderStatus,
  formatPaymentStatus,
} from "@/features/admin-orders";

type OrderStatusBadgeProps = {
  status: string;
};

export function OrderStatusBadge({ status }: OrderStatusBadgeProps) {
  const label = formatOrderStatus(status);
  const muted =
    status === "Cancelled" || status === "Delivered";

  return (
    <span
      className={
        muted
          ? "inline-flex items-center gap-1.5 rounded-md border border-border bg-card px-2 py-0.5 text-xs font-medium text-muted-foreground"
          : "inline-flex items-center gap-1.5 rounded-md border border-primary/25 bg-accent/60 px-2 py-0.5 text-xs font-medium text-foreground"
      }
    >
      <span
        aria-hidden
        className={
          muted
            ? "size-1.5 rounded-full border border-muted-foreground"
            : "size-1.5 rounded-full bg-primary"
        }
      />
      {label}
    </span>
  );
}

type PaymentStatusBadgeProps = {
  paymentStatus: string;
};

export function PaymentStatusBadge({ paymentStatus }: PaymentStatusBadgeProps) {
  const paid = paymentStatus === "Paid";
  return (
    <span
      className={
        paid
          ? "inline-flex items-center gap-1.5 rounded-md border border-primary/25 bg-accent/60 px-2 py-0.5 text-xs font-medium text-foreground"
          : "inline-flex items-center gap-1.5 rounded-md border border-border bg-muted/50 px-2 py-0.5 text-xs font-medium text-foreground"
      }
    >
      <span
        aria-hidden
        className={
          paid
            ? "size-1.5 rounded-full bg-primary"
            : "size-1.5 rounded-full border border-muted-foreground"
        }
      />
      {formatPaymentStatus(paymentStatus)}
    </span>
  );
}
