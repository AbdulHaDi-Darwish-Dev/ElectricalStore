"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ApiError, isNotFoundError } from "@/lib/api";
import { formatPrice, formatSellingUnit } from "@/lib/format";
import {
  formatOrderStatus,
  formatPaymentMethod,
  formatPaymentStatus,
  getMyOrder,
  getOrderingErrorMessage,
} from "@/features/orders";

export function MyOrderDetailView() {
  const params = useParams<{ id: string }>();
  const orderId = params.id;

  const orderQuery = useQuery({
    queryKey: ["my-order", orderId],
    queryFn: () => getMyOrder(orderId),
    enabled: Boolean(orderId),
    staleTime: 15_000,
    gcTime: 60_000,
    refetchOnWindowFocus: false,
    retry: (count, error) => {
      if (isNotFoundError(error)) return false;
      return count < 1;
    },
  });

  if (orderQuery.isLoading) {
    return (
      <div className="space-y-4" aria-busy="true">
        <div className="h-8 w-56 animate-pulse rounded bg-muted" />
        <div className="h-40 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (orderQuery.isError || !orderQuery.data) {
    if (isNotFoundError(orderQuery.error)) {
      return (
        <div className="space-y-3 rounded-md border border-border bg-card p-6">
          <h1 className="text-2xl font-semibold">الطلب غير موجود</h1>
          <p className="text-sm text-muted-foreground" role="alert">
            تعذر العثور على هذا الطلب.
          </p>
          <Link href="/account/orders" className="text-sm text-primary hover:underline">
            العودة إلى طلباتي
          </Link>
        </div>
      );
    }

    const message =
      orderQuery.error instanceof ApiError
        ? getOrderingErrorMessage(orderQuery.error.code)
        : "تعذر عرض الطلب.";

    return (
      <p className="text-sm text-muted-foreground" role="alert">
        {message}
      </p>
    );
  }

  const order = orderQuery.data;

  return (
    <div className="space-y-8">
      <header className="space-y-3 rounded-md border border-border bg-card p-6">
        <p className="text-sm text-muted-foreground">
          <Link href="/account/orders" className="text-primary hover:underline">
            طلباتي
          </Link>
        </p>
        <h1 className="text-3xl font-semibold tracking-tight">
          طلب {order.orderNumber}
        </h1>
        <dl className="grid gap-2 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">الحالة</dt>
            <dd className="font-medium">{formatOrderStatus(order.status)}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">الدفع</dt>
            <dd className="font-medium">
              {formatPaymentMethod(order.paymentMethod)} —{" "}
              {formatPaymentStatus(order.paymentStatus)}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">الاسم</dt>
            <dd className="font-medium">{order.customerName}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">الهاتف</dt>
            <dd className="font-medium" dir="ltr">
              {order.phone}
            </dd>
          </div>
          <div className="sm:col-span-2">
            <dt className="text-muted-foreground">العنوان</dt>
            <dd className="font-medium">{order.addressText}</dd>
          </div>
          {order.customerNote ? (
            <div className="sm:col-span-2">
              <dt className="text-muted-foreground">ملاحظة</dt>
              <dd className="font-medium">{order.customerNote}</dd>
            </div>
          ) : null}
        </dl>
      </header>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">الأصناف</h2>
        <ul className="divide-y divide-border rounded-md border border-border bg-card">
          {order.items.map((item) => (
            <li key={item.id} className="flex flex-wrap justify-between gap-2 px-4 py-3 text-sm">
              <div>
                <p className="font-medium">
                  {item.productName} — {item.variantName}
                </p>
                <p className="text-muted-foreground">
                  {item.quantity} {formatSellingUnit(item.sellingUnit)} ×{" "}
                  {formatPrice(item.unitPrice)}
                </p>
              </div>
              <p className="font-medium">{formatPrice(item.lineTotal)}</p>
            </li>
          ))}
        </ul>
      </section>

      <section className="rounded-md border border-border bg-card p-4 text-sm">
        <dl className="space-y-2">
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">مجموع البضاعة</dt>
            <dd>{formatPrice(order.merchandiseSubtotal)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">
              الشحن ({order.deliveryZoneName})
            </dt>
            <dd>{formatPrice(order.shippingFee)}</dd>
          </div>
          <div className="flex justify-between gap-4 border-t border-border pt-2 font-medium">
            <dt>الإجمالي</dt>
            <dd>{formatPrice(order.total)}</dd>
          </div>
        </dl>
      </section>
    </div>
  );
}
