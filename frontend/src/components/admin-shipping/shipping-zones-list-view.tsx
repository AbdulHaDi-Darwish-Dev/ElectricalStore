"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  AdminEmptyState,
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
} from "@/components/admin";
import {
  AdminConfirmDialog,
  AdminFeedback,
} from "@/components/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import {
  activateAdminShippingZone,
  adminShippingKeys,
  deactivateAdminShippingZone,
  getShippingErrorMessage,
  isFreeShippingFee,
  listAdminShippingZones,
  type AdminDeliveryZoneDto,
} from "@/features/admin-shipping";
import { ApiError } from "@/lib/api";
import { formatPrice } from "@/lib/format";
import { ShippingZoneStatusBadge } from "./shipping-zone-status-badge";

export function ShippingZonesListView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.shipping.manage]}>
      <ShippingZonesListContent />
    </AdminPermissionGate>
  );
}

function ShippingZonesListContent() {
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const flash = searchParams.get("status");
  const [pendingDeactivate, setPendingDeactivate] =
    useState<AdminDeliveryZoneDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const listQuery = useQuery({
    queryKey: adminShippingKeys.list(),
    queryFn: ({ signal }) => listAdminShippingZones(signal),
    ...adminOperationalQueryDefaults,
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateAdminShippingZone(id),
    onSuccess: async () => {
      setActionError(null);
      setActionSuccess("تم تفعيل منطقة الشحن.");
      await queryClient.invalidateQueries({
        queryKey: adminShippingKeys.all(),
      });
    },
    onError: (error) => {
      setActionSuccess(null);
      setActionError(mapError(error));
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateAdminShippingZone(id),
    onSuccess: async () => {
      setPendingDeactivate(null);
      setActionError(null);
      setActionSuccess(
        "تم إيقاف المنطقة. لن تظهر في خيارات الشحن عند الدفع (حتى دقيقة تقريباً).",
      );
      await queryClient.invalidateQueries({
        queryKey: adminShippingKeys.all(),
      });
    },
    onError: (error) => {
      setActionSuccess(null);
      setActionError(mapError(error));
    },
  });

  const busy = activateMutation.isPending || deactivateMutation.isPending;

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل مناطق الشحن…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل مناطق الشحن"
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

  const zones = listQuery.data ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="الشحن"
        description="مناطق التوصيل ذات الرسوم الثابتة. الحد الأدنى لقيمة الطلب إعداد عام (الإعدادات) وليس لكل منطقة. قائمة الشحن العامة قد تتأخر حتى دقيقة؛ معاينة الدفع تبقى فورية من الخادم."
        actions={
          <Link
            href="/admin/shipping/new"
            className="inline-flex items-center justify-center rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
          >
            منطقة جديدة
          </Link>
        }
      />

      {flash === "created" ? (
        <AdminFeedback tone="success">تم إنشاء منطقة الشحن بنجاح.</AdminFeedback>
      ) : null}
      {flash === "updated" ? (
        <AdminFeedback tone="success">تم حفظ تعديلات المنطقة.</AdminFeedback>
      ) : null}
      {actionSuccess ? (
        <AdminFeedback tone="success">{actionSuccess}</AdminFeedback>
      ) : null}
      {actionError ? (
        <AdminFeedback tone="error">{actionError}</AdminFeedback>
      ) : null}

      {zones.length === 0 ? (
        <AdminEmptyState
          title="لا توجد مناطق شحن"
          description="أنشئ أول منطقة توصيل لتفعيل الشحن عند الدفع."
          actions={
            <Link
              href="/admin/shipping/new"
              className="inline-flex rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
            >
              إنشاء منطقة
            </Link>
          }
        />
      ) : (
        <>
          <div className="hidden overflow-hidden rounded-md border border-border md:block">
            <table className="w-full text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">المنطقة</th>
                  <th className="px-3 py-2.5 font-medium">رسوم الشحن</th>
                  <th className="px-3 py-2.5 font-medium">الحالة</th>
                  <th className="px-3 py-2.5 font-medium">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {zones.map((zone) => (
                  <tr
                    key={zone.id}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3 font-medium">{zone.name}</td>
                    <td className="px-3 py-3 tabular-nums">
                      <FeeCell fee={zone.fee} />
                    </td>
                    <td className="px-3 py-3">
                      <ShippingZoneStatusBadge isActive={zone.isActive} />
                    </td>
                    <td className="px-3 py-3">
                      <ZoneRowActions
                        zone={zone}
                        busy={busy}
                        onActivate={() => {
                          setActionError(null);
                          activateMutation.mutate(zone.id);
                        }}
                        onRequestDeactivate={() => {
                          setActionError(null);
                          setPendingDeactivate(zone);
                        }}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="space-y-3 md:hidden">
            {zones.map((zone) => (
              <li
                key={zone.id}
                className="space-y-3 rounded-md border border-border p-4"
              >
                <div className="space-y-1">
                  <p className="font-medium">{zone.name}</p>
                  <FeeCell fee={zone.fee} />
                  <ShippingZoneStatusBadge isActive={zone.isActive} />
                </div>
                <ZoneRowActions
                  zone={zone}
                  busy={busy}
                  onActivate={() => {
                    setActionError(null);
                    activateMutation.mutate(zone.id);
                  }}
                  onRequestDeactivate={() => {
                    setActionError(null);
                    setPendingDeactivate(zone);
                  }}
                />
              </li>
            ))}
          </ul>
        </>
      )}

      <AdminConfirmDialog
        open={pendingDeactivate !== null}
        title="إيقاف منطقة الشحن؟"
        description={
          pendingDeactivate
            ? `سيتم إخفاء «${pendingDeactivate.name}» من خيارات الشحن عند الدفع. الطلبات السابقة تحتفظ برسومها المحفوظة.`
            : ""
        }
        confirmLabel="إيقاف التفعيل"
        tone="danger"
        busy={deactivateMutation.isPending}
        onCancel={() => {
          if (!deactivateMutation.isPending) setPendingDeactivate(null);
        }}
        onConfirm={() => {
          if (pendingDeactivate) {
            deactivateMutation.mutate(pendingDeactivate.id);
          }
        }}
      />
    </div>
  );
}

function FeeCell({ fee }: { fee: number }) {
  if (isFreeShippingFee(fee)) {
    return (
      <span>
        {formatPrice(0)}{" "}
        <span className="text-muted-foreground">(مجاني)</span>
      </span>
    );
  }
  return <span>{formatPrice(fee)}</span>;
}

function ZoneRowActions({
  zone,
  busy,
  onActivate,
  onRequestDeactivate,
}: {
  zone: AdminDeliveryZoneDto;
  busy: boolean;
  onActivate: () => void;
  onRequestDeactivate: () => void;
}) {
  return (
    <div className="flex flex-wrap gap-2">
      <Link
        href={`/admin/shipping/${zone.id}`}
        className="rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted"
      >
        تعديل
      </Link>
      {zone.isActive ? (
        <button
          type="button"
          disabled={busy}
          onClick={onRequestDeactivate}
          className="rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted disabled:opacity-60"
        >
          إيقاف
        </button>
      ) : (
        <button
          type="button"
          disabled={busy}
          onClick={onActivate}
          className="rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted disabled:opacity-60"
        >
          تفعيل
        </button>
      )}
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getShippingErrorMessage(error.code, error.status);
  }
  return getShippingErrorMessage(undefined);
}
