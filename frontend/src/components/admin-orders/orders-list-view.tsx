"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import {
  AdminEmptyState,
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
} from "@/components/admin";
import { AdminFeedback } from "@/components/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import {
  ORDER_STATUS_FILTER_OPTIONS,
  PAYMENT_STATUS_FILTER_OPTIONS,
  adminOrderKeys,
  formatOrderDateTime,
  getAdminOrderErrorMessage,
  listAdminOrders,
  type AdminOrderListParams,
  type OrderStatusCode,
  type PaymentStatusCode,
} from "@/features/admin-orders";
import { ApiError } from "@/lib/api";
import { formatPrice } from "@/lib/format";
import {
  OrderStatusBadge,
  PaymentStatusBadge,
} from "./order-status-badges";

export function OrdersListView() {
  return (
    <AdminPermissionGate
      anyOf={[AppPermission.orders.read]}
      deniedTitle="غير مصرح بعرض الطلبات"
      deniedMessage="عرض الطلبات يتطلب صلاحية Orders.Read. صلاحية الإدارة وحدها لا تتيح قائمة الطلبات."
    >
      <OrdersListContent />
    </AdminPermissionGate>
  );
}

function OrdersListContent() {
  const searchParams = useSearchParams();
  const flash = searchParams.get("status");

  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<OrderStatusCode | "">("");
  const [paymentFilter, setPaymentFilter] = useState<PaymentStatusCode | "">(
    "",
  );

  const listParams: AdminOrderListParams = useMemo(() => {
    const params: AdminOrderListParams = {};
    if (appliedSearch) params.search = appliedSearch;
    if (statusFilter) params.status = statusFilter;
    if (paymentFilter) params.paymentStatus = paymentFilter;
    return params;
  }, [appliedSearch, statusFilter, paymentFilter]);

  const listQuery = useQuery({
    queryKey: adminOrderKeys.list(listParams),
    queryFn: ({ signal }) => listAdminOrders(listParams, signal),
    ...adminOperationalQueryDefaults,
  });

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل الطلبات…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل الطلبات"
        message={mapError(listQuery.error)}
        actions={
          <button
            type="button"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            onClick={() => void listQuery.refetch()}
          >
            إعادة المحاولة
          </button>
        }
      />
    );
  }

  const orders = listQuery.data ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="الطلبات"
        description="دورة حياة الطلبات التشغيلية. التأكيد يحجز المخزون؛ الإرسال للتوصيل يخصم المخزون. قائمة الطلبات غير مقسّمة صفحات (كل النتائج المطابقة)."
      />

      <form
        className="flex flex-wrap items-end gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setAppliedSearch(search.trim());
        }}
      >
        <div className="min-w-[12rem] flex-1 space-y-1">
          <label htmlFor="orders-search" className="sr-only">
            بحث في الطلبات
          </label>
          <input
            id="orders-search"
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="رقم الطلب أو الاسم أو الهاتف…"
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
          />
        </div>
        <select
          aria-label="تصفية حالة الطلب"
          value={statusFilter}
          onChange={(e) =>
            setStatusFilter(e.target.value as OrderStatusCode | "")
          }
          className="rounded-md border border-border bg-background px-3 py-2 text-sm"
        >
          {ORDER_STATUS_FILTER_OPTIONS.map((opt) => (
            <option key={opt.value || "all"} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
        <select
          aria-label="تصفية حالة الدفع"
          value={paymentFilter}
          onChange={(e) =>
            setPaymentFilter(e.target.value as PaymentStatusCode | "")
          }
          className="rounded-md border border-border bg-background px-3 py-2 text-sm"
        >
          {PAYMENT_STATUS_FILTER_OPTIONS.map((opt) => (
            <option key={opt.value || "all-pay"} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          بحث
        </button>
      </form>

      {flash === "updated" ? (
        <AdminFeedback tone="success">تم تحديث الطلب.</AdminFeedback>
      ) : null}

      {orders.length === 0 ? (
        <AdminEmptyState
          title="لا توجد طلبات"
          description={
            appliedSearch || statusFilter || paymentFilter
              ? "لا نتائج مطابقة للتصفية الحالية."
              : "ستظهر الطلبات هنا بعد إنشائها من المتجر."
          }
        />
      ) : (
        <>
          <div className="hidden overflow-x-auto rounded-md border border-border md:block">
            <table className="w-full min-w-[48rem] text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">الطلب</th>
                  <th className="px-3 py-2.5 font-medium">التاريخ</th>
                  <th className="px-3 py-2.5 font-medium">العميل</th>
                  <th className="px-3 py-2.5 font-medium">الإجمالي</th>
                  <th className="px-3 py-2.5 font-medium">الحالة</th>
                  <th className="px-3 py-2.5 font-medium">الدفع</th>
                  <th className="px-3 py-2.5 font-medium"> </th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr
                    key={order.id}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3 font-medium">
                      {order.orderNumber}
                    </td>
                    <td className="px-3 py-3 text-muted-foreground">
                      {formatOrderDateTime(order.createdAtUtc)}
                    </td>
                    <td className="px-3 py-3">
                      <div>{order.customerName}</div>
                      <div className="text-xs text-muted-foreground" dir="ltr">
                        {order.phone}
                      </div>
                    </td>
                    <td className="px-3 py-3 tabular-nums">
                      {formatPrice(order.total)}
                    </td>
                    <td className="px-3 py-3">
                      <OrderStatusBadge status={order.status} />
                    </td>
                    <td className="px-3 py-3">
                      <PaymentStatusBadge paymentStatus={order.paymentStatus} />
                    </td>
                    <td className="px-3 py-3">
                      <Link
                        href={`/admin/orders/${order.id}`}
                        className="rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted"
                      >
                        عرض
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="space-y-3 md:hidden">
            {orders.map((order) => (
              <li
                key={order.id}
                className="space-y-3 rounded-md border border-border p-4"
              >
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div>
                    <p className="font-medium">{order.orderNumber}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatOrderDateTime(order.createdAtUtc)}
                    </p>
                  </div>
                  <p className="tabular-nums font-medium">
                    {formatPrice(order.total)}
                  </p>
                </div>
                <p className="text-sm">
                  {order.customerName}
                  <span className="ms-2 text-muted-foreground" dir="ltr">
                    {order.phone}
                  </span>
                </p>
                <div className="flex flex-wrap gap-2">
                  <OrderStatusBadge status={order.status} />
                  <PaymentStatusBadge paymentStatus={order.paymentStatus} />
                </div>
                <Link
                  href={`/admin/orders/${order.id}`}
                  className="inline-flex rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted"
                >
                  عرض التفاصيل
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getAdminOrderErrorMessage(error.code, error.status);
  }
  return getAdminOrderErrorMessage(undefined);
}
