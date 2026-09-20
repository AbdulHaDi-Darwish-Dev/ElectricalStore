"use client";

import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import {
  AdminConfirmDialog,
  AdminFeedback,
} from "@/components/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import { adminInventoryKeys } from "@/features/admin-inventory";
import {
  actionAffectsInventory,
  actionLabel,
  adminOrderKeys,
  cancelAdminOrder,
  cancelOrderFormSchema,
  cancelReleasesReservation,
  confirmAdminOrder,
  deliverAdminOrder,
  formatOrderDateTime,
  formatOrderQuantityWithUnit,
  formatOrderStatus,
  formatPaymentMethod,
  formatPaymentStatus,
  getAdminOrder,
  getAdminOrderErrorMessage,
  getAvailableAdminOrderActions,
  markPaidAdminOrder,
  outForDeliveryAdminOrder,
  prepareAdminOrder,
  type AdminOrderAction,
  type AdminOrderDto,
  type CancelOrderFormValues,
} from "@/features/admin-orders";
import { ApiError } from "@/lib/api";
import { hasPermission, useAuthStore } from "@/lib/auth";
import { formatPrice } from "@/lib/format";
import {
  OrderStatusBadge,
  PaymentStatusBadge,
} from "./order-status-badges";

type OrderDetailViewProps = {
  orderId: string;
};

export function OrderDetailView({ orderId }: OrderDetailViewProps) {
  return (
    <AdminPermissionGate
      anyOf={[AppPermission.orders.read]}
      deniedTitle="غير مصرح بعرض الطلبات"
      deniedMessage="عرض الطلبات يتطلب صلاحية Orders.Read."
    >
      <OrderDetailContent orderId={orderId} />
    </AdminPermissionGate>
  );
}

function OrderDetailContent({ orderId }: OrderDetailViewProps) {
  const queryClient = useQueryClient();
  const permissions = useAuthStore((s) => s.permissions);
  const canManage = hasPermission(permissions, AppPermission.orders.manage);

  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<AdminOrderAction | null>(
    null,
  );
  const [showCancelForm, setShowCancelForm] = useState(false);

  const detailQuery = useQuery({
    queryKey: adminOrderKeys.detail(orderId),
    queryFn: ({ signal }) => getAdminOrder(orderId, signal),
    ...adminOperationalQueryDefaults,
  });

  const cancelForm = useForm<CancelOrderFormValues>({
    resolver: zodResolver(cancelOrderFormSchema),
    defaultValues: { reason: "" },
  });

  async function invalidateAfter(action: AdminOrderAction) {
    await queryClient.invalidateQueries({ queryKey: adminOrderKeys.all() });
    if (actionAffectsInventory(action)) {
      await queryClient.invalidateQueries({
        queryKey: adminInventoryKeys.all(),
      });
    }
  }

  const runMutation = useMutation({
    mutationFn: async ({
      action,
      reason,
    }: {
      action: AdminOrderAction;
      reason?: string;
    }) => {
      switch (action) {
        case "confirm":
          return confirmAdminOrder(orderId);
        case "prepare":
          return prepareAdminOrder(orderId);
        case "outForDelivery":
          return outForDeliveryAdminOrder(orderId);
        case "deliver":
          return deliverAdminOrder(orderId);
        case "markPaid":
          return markPaidAdminOrder(orderId);
        case "cancel":
          return cancelAdminOrder(orderId, { reason: reason!.trim() });
      }
    },
    onSuccess: async (_data, vars) => {
      setActionError(null);
      setActionSuccess(`تم: ${actionLabel(vars.action)}.`);
      setPendingAction(null);
      setShowCancelForm(false);
      cancelForm.reset({ reason: "" });
      await invalidateAfter(vars.action);
      await detailQuery.refetch();
    },
    onError: async (error, vars) => {
      setActionSuccess(null);
      setActionError(mapError(error));
      setPendingAction(null);
      if (
        error instanceof ApiError &&
        (error.code === "Ordering.ConcurrencyConflict" ||
          error.code === "Ordering.InvalidTransition")
      ) {
        await invalidateAfter(vars.action);
        await detailQuery.refetch();
      }
    },
  });

  if (detailQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل الطلب…" />;
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <AdminErrorState
        title="تعذر تحميل الطلب"
        message={mapError(detailQuery.error)}
        actions={
          <>
            <button
              type="button"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
              onClick={() => void detailQuery.refetch()}
            >
              إعادة المحاولة
            </button>
            <Link
              href="/admin/orders"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            >
              العودة للقائمة
            </Link>
          </>
        }
      />
    );
  }

  const order = detailQuery.data;
  const actions = canManage
    ? getAvailableAdminOrderActions(order.status, order.paymentStatus)
    : [];
  const busy = runMutation.isPending;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title={`طلب ${order.orderNumber}`}
        description={`${formatOrderDateTime(order.createdAtUtc)} · ${formatPaymentMethod(order.paymentMethod)}`}
        actions={
          <Link
            href="/admin/orders"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع للقائمة
          </Link>
        }
      />

      <div className="flex flex-wrap gap-2">
        <OrderStatusBadge status={order.status} />
        <PaymentStatusBadge paymentStatus={order.paymentStatus} />
      </div>

      {actionSuccess ? (
        <AdminFeedback tone="success">{actionSuccess}</AdminFeedback>
      ) : null}
      {actionError ? (
        <AdminFeedback tone="error">{actionError}</AdminFeedback>
      ) : null}

      {actions.length > 0 ? (
        <AdminSection title="إجراءات التشغيل">
          <div className="flex flex-wrap gap-2">
            {actions.map((action) =>
              action === "cancel" ? (
                <button
                  key={action}
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    setActionError(null);
                    setShowCancelForm(true);
                  }}
                  className="rounded-md border border-destructive/40 px-3 py-2 text-sm text-destructive hover:bg-destructive/5 disabled:opacity-60"
                >
                  {actionLabel(action)}
                </button>
              ) : (
                <button
                  key={action}
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    setActionError(null);
                    setPendingAction(action);
                  }}
                  className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
                >
                  {actionLabel(action)}
                </button>
              ),
            )}
          </div>
          {actions.includes("confirm") ? (
            <p className="mt-3 text-xs leading-5 text-muted-foreground">
              التأكيد يحجز المخزون فوراً لجميع بنود الطلب.
            </p>
          ) : null}
          {order.status === "Preparing" && actions.includes("outForDelivery") ? (
            <p className="mt-3 text-xs leading-5 text-muted-foreground">
              الإرسال للتوصيل يخصم المخزون الفعلي والمحجوز معاً.
            </p>
          ) : null}
        </AdminSection>
      ) : null}

      {showCancelForm && canManage ? (
        <AdminSection title="إلغاء الطلب">
          <form
            className="max-w-lg space-y-4"
            onSubmit={cancelForm.handleSubmit((values) => {
              setPendingAction("cancel");
              runMutation.mutate({ action: "cancel", reason: values.reason });
            })}
            noValidate
          >
            <p className="text-sm leading-6 text-muted-foreground">
              {cancelReleasesReservation(order.status)
                ? "سيُحرَّر المخزون المحجوز لهذا الطلب."
                : "هذا الطلب لم يحجز مخزوناً بعد (بانتظار التأكيد)."}
            </p>
            <div className="space-y-1.5">
              <label htmlFor="cancel-reason" className="block text-sm font-medium">
                سبب الإلغاء
              </label>
              <textarea
                id="cancel-reason"
                rows={3}
                disabled={busy}
                className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
                aria-invalid={
                  cancelForm.formState.errors.reason ? true : undefined
                }
                {...cancelForm.register("reason")}
              />
              {cancelForm.formState.errors.reason ? (
                <p className="text-sm text-destructive" role="alert">
                  {cancelForm.formState.errors.reason.message}
                </p>
              ) : null}
            </div>
            <div className="flex flex-wrap gap-2">
              <button
                type="submit"
                disabled={busy}
                className="rounded-md bg-destructive px-3 py-2 text-sm font-medium text-destructive-foreground hover:opacity-95 disabled:opacity-60"
              >
                {busy && pendingAction === "cancel"
                  ? "جاري الإلغاء…"
                  : "تأكيد الإلغاء"}
              </button>
              <button
                type="button"
                disabled={busy}
                onClick={() => {
                  setShowCancelForm(false);
                  cancelForm.reset({ reason: "" });
                }}
                className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
              >
                إغلاق
              </button>
            </div>
          </form>
        </AdminSection>
      ) : null}

      <AdminSection title="العميل والتوصيل">
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <Field label="الاسم" value={order.customerName} />
          <Field label="الهاتف" value={order.phone} dir="ltr" />
          <Field label="العنوان" value={order.addressText} className="sm:col-span-2" />
          {order.customerNote ? (
            <Field
              label="ملاحظة العميل"
              value={order.customerNote}
              className="sm:col-span-2"
            />
          ) : null}
          <Field label="منطقة التوصيل" value={order.deliveryZoneName} />
          <Field label="رسوم الشحن" value={formatPrice(order.shippingFee)} />
          {order.cancellationReason ? (
            <Field
              label="سبب الإلغاء"
              value={order.cancellationReason}
              className="sm:col-span-2"
            />
          ) : null}
        </dl>
      </AdminSection>

      <AdminSection title="البنود">
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full min-w-[36rem] text-sm">
            <thead className="border-b border-border bg-muted/40 text-start">
              <tr>
                <th className="px-3 py-2 font-medium">المنتج</th>
                <th className="px-3 py-2 font-medium">SKU</th>
                <th className="px-3 py-2 font-medium">الكمية</th>
                <th className="px-3 py-2 font-medium">سعر الوحدة</th>
                <th className="px-3 py-2 font-medium">الإجمالي</th>
              </tr>
            </thead>
            <tbody>
              {order.items.map((item) => (
                <tr
                  key={item.id}
                  className="border-b border-border last:border-b-0"
                >
                  <td className="px-3 py-2">
                    <div className="font-medium">{item.productName}</div>
                    <div className="text-muted-foreground">{item.variantName}</div>
                  </td>
                  <td className="px-3 py-2 font-mono text-xs">{item.sku}</td>
                  <td className="px-3 py-2 tabular-nums">
                    {formatOrderQuantityWithUnit(
                      item.quantity,
                      item.sellingUnit,
                    )}
                  </td>
                  <td className="px-3 py-2 tabular-nums">
                    {formatPrice(item.unitPrice)}
                  </td>
                  <td className="px-3 py-2 tabular-nums font-medium">
                    {formatPrice(item.lineTotal)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </AdminSection>

      <AdminSection title="الملخص المالي">
        <dl className="max-w-sm space-y-2 text-sm">
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">مجموع المنتجات</dt>
            <dd className="tabular-nums">
              {formatPrice(order.merchandiseSubtotal)}
            </dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">الحد الأدنى المطبق</dt>
            <dd className="tabular-nums">
              {formatPrice(order.appliedMinimumOrderAmount)}
            </dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">الشحن</dt>
            <dd className="tabular-nums">{formatPrice(order.shippingFee)}</dd>
          </div>
          <div className="flex justify-between gap-4 border-t border-border pt-2 font-medium">
            <dt>الإجمالي</dt>
            <dd className="tabular-nums">{formatPrice(order.total)}</dd>
          </div>
          <div className="flex justify-between gap-4 text-muted-foreground">
            <dt>الدفع</dt>
            <dd>
              {formatPaymentMethod(order.paymentMethod)} —{" "}
              {formatPaymentStatus(order.paymentStatus)}
            </dd>
          </div>
        </dl>
      </AdminSection>

      <AdminSection title="الجدول الزمني">
        <Timeline order={order} />
      </AdminSection>

      {order.modificationAudits && order.modificationAudits.length > 0 ? (
        <AdminSection title="تعديلات الموظفين (قبل التأكيد)">
          <ul className="space-y-2 text-sm">
            {order.modificationAudits.map((audit) => (
              <li
                key={audit.id}
                className="rounded-md border border-border px-3 py-2"
              >
                <p className="font-medium">{audit.summary}</p>
                <p className="text-muted-foreground">{audit.reason}</p>
                <p className="text-xs text-muted-foreground">
                  {formatOrderDateTime(audit.createdAtUtc)}
                </p>
              </li>
            ))}
          </ul>
        </AdminSection>
      ) : null}

      {pendingAction && pendingAction !== "cancel" ? (
        <AdminConfirmDialog
          open
          title={actionLabel(pendingAction)}
          description={confirmDescription(pendingAction, order)}
          confirmLabel={actionLabel(pendingAction)}
          tone={
            pendingAction === "outForDelivery" ? "danger" : "default"
          }
          busy={busy}
          onCancel={() => {
            if (!busy) setPendingAction(null);
          }}
          onConfirm={() => {
            runMutation.mutate({ action: pendingAction });
          }}
        />
      ) : null}
    </div>
  );
}

function Field({
  label,
  value,
  dir,
  className,
}: {
  label: string;
  value: string;
  dir?: "ltr" | "rtl";
  className?: string;
}) {
  return (
    <div className={className}>
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="font-medium" dir={dir}>
        {value}
      </dd>
    </div>
  );
}

function Timeline({ order }: { order: AdminOrderDto }) {
  const rows: { label: string; at: string | null }[] = [
    { label: "إنشاء", at: order.createdAtUtc },
    { label: formatOrderStatus("Confirmed"), at: order.confirmedAtUtc },
    { label: formatOrderStatus("Preparing"), at: order.preparingAtUtc },
    {
      label: formatOrderStatus("OutForDelivery"),
      at: order.outForDeliveryAtUtc,
    },
    { label: formatOrderStatus("Delivered"), at: order.deliveredAtUtc },
    { label: formatOrderStatus("Cancelled"), at: order.cancelledAtUtc },
  ];

  return (
    <ul className="space-y-2 text-sm">
      {rows
        .filter((r) => r.at)
        .map((r) => (
          <li key={r.label} className="flex flex-wrap justify-between gap-2">
            <span>{r.label}</span>
            <span className="text-muted-foreground">
              {formatOrderDateTime(r.at)}
            </span>
          </li>
        ))}
    </ul>
  );
}

function confirmDescription(
  action: AdminOrderAction,
  order: AdminOrderDto,
): string {
  switch (action) {
    case "confirm":
      return `تأكيد الطلب ${order.orderNumber} سيحجز المخزون لجميع البنود. لا يمكن التراجع عن الحجز إلا بالإلغاء قبل الإرسال للتوصيل.`;
    case "prepare":
      return `نقل الطلب ${order.orderNumber} إلى قيد التجهيز.`;
    case "outForDelivery":
      return `إرسال الطلب ${order.orderNumber} للتوصيل سيخصم المخزون الفعلي والمحجوز. هذه خطوة تشغيلية مهمة.`;
    case "deliver":
      return `تأكيد تسليم الطلب ${order.orderNumber}. لا يغيّر حالة الدفع تلقائياً.`;
    case "markPaid":
      return `تسجيل استلام الدفع نقداً عند التسليم للطلب ${order.orderNumber}. لا يمكن عكس هذه العملية من الواجهة.`;
    case "cancel":
      return "";
  }
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getAdminOrderErrorMessage(error.code, error.status);
  }
  return getAdminOrderErrorMessage(undefined);
}
