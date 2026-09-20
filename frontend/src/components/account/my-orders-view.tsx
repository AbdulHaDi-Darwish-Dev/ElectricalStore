"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ApiError } from "@/lib/api";
import { formatPrice } from "@/lib/format";
import {
  formatOrderStatus,
  formatPaymentStatus,
  getOrderingErrorMessage,
  listMyOrders,
} from "@/features/orders";

function formatCreatedAt(iso: string): string {
  const ms = Date.parse(iso);
  if (Number.isNaN(ms)) return iso;
  try {
    return new Intl.DateTimeFormat("ar", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(new Date(ms));
  } catch {
    return iso;
  }
}

export function MyOrdersView() {
  const ordersQuery = useQuery({
    queryKey: ["my-orders"],
    queryFn: listMyOrders,
    staleTime: 15_000,
    gcTime: 60_000,
    refetchOnWindowFocus: false,
    retry: 1,
  });

  if (ordersQuery.isLoading) {
    return (
      <div className="space-y-4" aria-busy="true">
        <div className="h-8 w-40 animate-pulse rounded bg-muted" />
        <div className="h-24 animate-pulse rounded-md bg-muted" />
        <div className="h-24 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (ordersQuery.isError) {
    const message =
      ordersQuery.error instanceof ApiError
        ? getOrderingErrorMessage(ordersQuery.error.code)
        : "تعذر تحميل الطلبات.";
    return (
      <p className="text-sm text-muted-foreground" role="alert">
        {message}
      </p>
    );
  }

  const orders = ordersQuery.data ?? [];

  if (orders.length === 0) {
    return (
      <div className="rounded-md border border-border bg-card p-6">
        <p className="text-muted-foreground">لا توجد طلبات بعد.</p>
        <Link href="/products" className="mt-3 inline-block text-sm text-primary hover:underline">
          تصفح المنتجات
        </Link>
      </div>
    );
  }

  return (
    <ul className="space-y-3">
      {orders.map((order) => (
        <li key={order.id}>
          <Link
            href={`/account/orders/${order.id}`}
            className="block rounded-md border border-border bg-card p-4 transition hover:border-primary/40"
          >
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <p className="font-medium text-foreground">{order.orderNumber}</p>
              <p className="text-sm font-medium">{formatPrice(order.total)}</p>
            </div>
            <dl className="mt-3 grid gap-1 text-sm text-muted-foreground sm:grid-cols-2">
              <div>
                <dt className="inline">الحالة: </dt>
                <dd className="inline text-foreground">
                  {formatOrderStatus(order.status)}
                </dd>
              </div>
              <div>
                <dt className="inline">الدفع: </dt>
                <dd className="inline text-foreground">
                  {formatPaymentStatus(order.paymentStatus)}
                </dd>
              </div>
              <div>
                <dt className="inline">الاسم: </dt>
                <dd className="inline text-foreground">{order.customerName}</dd>
              </div>
              <div>
                <dt className="inline">الهاتف: </dt>
                <dd className="inline text-foreground" dir="ltr">
                  {order.phone}
                </dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="inline">تاريخ الإنشاء: </dt>
                <dd className="inline text-foreground">
                  {formatCreatedAt(order.createdAtUtc)}
                </dd>
              </div>
            </dl>
          </Link>
        </li>
      ))}
    </ul>
  );
}
