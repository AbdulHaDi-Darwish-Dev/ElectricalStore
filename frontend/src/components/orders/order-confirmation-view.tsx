"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { ApiError } from "@/lib/api";
import { formatPrice, formatSellingUnit } from "@/lib/format";
import {
  formatOrderStatus,
  formatPaymentMethod,
  formatPaymentStatus,
  getGuestOrder,
  getOrderingErrorMessage,
} from "@/features/orders";

export function OrderConfirmationView() {
  const params = useParams<{ id: string }>();
  const orderId = params.id;

  const orderQuery = useQuery({
    queryKey: ["guest-order", orderId],
    queryFn: () => getGuestOrder(orderId),
    enabled: Boolean(orderId),
    staleTime: 0,
    gcTime: 0,
    retry: 1,
    refetchOnWindowFocus: false,
  });

  if (orderQuery.isLoading) {
    return (
      <div className="space-y-4" aria-busy="true">
        <div className="h-8 w-64 animate-pulse rounded bg-muted" />
        <div className="h-40 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (orderQuery.isError || !orderQuery.data) {
    const message =
      orderQuery.error instanceof ApiError
        ? getOrderingErrorMessage(orderQuery.error.code)
        : "تعذر عرض تأكيد الطلب.";

    return (
      <div className="space-y-4 rounded-md border border-border bg-card p-6">
        <h1 className="text-2xl font-semibold text-foreground">تأكيد الطلب</h1>
        <p className="text-muted-foreground" role="alert">
          {message}
        </p>
        <Link href="/products" className="text-sm text-primary hover:underline">
          العودة إلى المنتجات
        </Link>
      </div>
    );
  }

  const order = orderQuery.data;

  return (
    <div className="space-y-8">
      <header className="space-y-3 rounded-md border border-border bg-card p-6">
        <p className="text-sm font-medium text-accent-foreground">تم استلام طلبك</p>
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          رقم الطلب {order.orderNumber}
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
        </dl>
      </header>

      <section className="space-y-3 rounded-md border border-border bg-card p-6">
        <h2 className="text-lg font-medium">بيانات التوصيل</h2>
        <dl className="space-y-2 text-sm">
          <div>
            <dt className="text-muted-foreground">الاسم</dt>
            <dd>{order.customerName}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">الهاتف</dt>
            <dd dir="ltr" className="text-start">
              {order.phone}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">العنوان</dt>
            <dd className="whitespace-pre-wrap">{order.addressText}</dd>
          </div>
          {order.customerNote ? (
            <div>
              <dt className="text-muted-foreground">ملاحظة</dt>
              <dd className="whitespace-pre-wrap">{order.customerNote}</dd>
            </div>
          ) : null}
          <div>
            <dt className="text-muted-foreground">منطقة التوصيل</dt>
            <dd>{order.deliveryZoneName}</dd>
          </div>
        </dl>
      </section>

      <section className="space-y-4 rounded-md border border-border bg-card p-6">
        <h2 className="text-lg font-medium">الأصناف</h2>
        <ul className="divide-y divide-border">
          {order.items.map((item) => (
            <li
              key={item.id}
              className="flex flex-wrap items-baseline justify-between gap-2 py-3 text-sm"
            >
              <div>
                <p className="font-medium">{item.productName}</p>
                <p className="text-muted-foreground">
                  {item.variantName} · {item.quantity}{" "}
                  {formatSellingUnit(item.sellingUnit)}
                </p>
              </div>
              <p className="tabular-nums font-medium">
                {formatPrice(item.lineTotal)}
              </p>
            </li>
          ))}
        </ul>

        <dl className="space-y-2 border-t border-border pt-4 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">مجموع البضاعة</dt>
            <dd className="tabular-nums">
              {formatPrice(order.merchandiseSubtotal)}
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">الشحن</dt>
            <dd className="tabular-nums">{formatPrice(order.shippingFee)}</dd>
          </div>
          <div className="flex justify-between gap-3 text-base font-semibold">
            <dt>الإجمالي</dt>
            <dd className="tabular-nums">{formatPrice(order.total)}</dd>
          </div>
        </dl>
      </section>

      <Link href="/products" className="inline-flex text-sm text-primary hover:underline">
        متابعة التسوق
      </Link>
    </div>
  );
}
