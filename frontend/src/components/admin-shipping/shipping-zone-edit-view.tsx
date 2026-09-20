"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { FormProvider, useForm } from "react-hook-form";
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
import { AppPermission, adminOperationalQueryDefaults } from "@/features/admin";
import {
  activateAdminShippingZone,
  adminShippingKeys,
  deactivateAdminShippingZone,
  emptyShippingZoneFormValues,
  getAdminShippingZone,
  getShippingErrorMessage,
  shippingZoneFormSchema,
  toUpdateShippingZoneRequest,
  updateAdminShippingZone,
  type ShippingZoneFormValues,
} from "@/features/admin-shipping";
import { ApiError } from "@/lib/api";
import { ShippingZoneFormFields } from "./shipping-zone-form-fields";
import { ShippingZoneStatusBadge } from "./shipping-zone-status-badge";

type ShippingZoneEditViewProps = {
  zoneId: string;
};

export function ShippingZoneEditView({ zoneId }: ShippingZoneEditViewProps) {
  return (
    <AdminPermissionGate anyOf={[AppPermission.shipping.manage]}>
      <ShippingZoneEditContent zoneId={zoneId} />
    </AdminPermissionGate>
  );
}

function ShippingZoneEditContent({ zoneId }: ShippingZoneEditViewProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState<string | null>(null);
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);

  const detailQuery = useQuery({
    queryKey: adminShippingKeys.detail(zoneId),
    queryFn: ({ signal }) => getAdminShippingZone(zoneId, signal),
    ...adminOperationalQueryDefaults,
  });

  const form = useForm<ShippingZoneFormValues>({
    resolver: zodResolver(shippingZoneFormSchema),
    defaultValues: emptyShippingZoneFormValues(),
    mode: "onBlur",
  });

  useEffect(() => {
    if (!detailQuery.data) return;
    form.reset({
      name: detailQuery.data.name,
      fee: detailQuery.data.fee,
      isActive: detailQuery.data.isActive,
    });
  }, [detailQuery.data, form]);

  async function invalidateAll() {
    await queryClient.invalidateQueries({ queryKey: adminShippingKeys.all() });
  }

  const updateMutation = useMutation({
    mutationFn: (values: ShippingZoneFormValues) =>
      updateAdminShippingZone(zoneId, toUpdateShippingZoneRequest(values)),
    onSuccess: async () => {
      setFormError(null);
      setFormSuccess("تم حفظ التعديلات.");
      await invalidateAll();
      router.replace("/admin/shipping?status=updated");
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(mapError(error));
    },
  });

  const activateMutation = useMutation({
    mutationFn: () => activateAdminShippingZone(zoneId),
    onSuccess: async (zone) => {
      setFormError(null);
      setFormSuccess("تم تفعيل المنطقة.");
      form.setValue("isActive", zone.isActive);
      await invalidateAll();
      await detailQuery.refetch();
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(mapError(error));
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: () => deactivateAdminShippingZone(zoneId),
    onSuccess: async (zone) => {
      setConfirmDeactivate(false);
      setFormError(null);
      setFormSuccess("تم إيقاف المنطقة. لن تظهر في الدفع.");
      form.setValue("isActive", zone.isActive);
      await invalidateAll();
      await detailQuery.refetch();
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(mapError(error));
    },
  });

  const submitting =
    updateMutation.isPending ||
    activateMutation.isPending ||
    deactivateMutation.isPending;

  if (detailQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل المنطقة…" />;
  }

  if (detailQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل منطقة الشحن"
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
              href="/admin/shipping"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            >
              العودة للقائمة
            </Link>
          </>
        }
      />
    );
  }

  const zone = detailQuery.data!;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title={zone.name}
        description="تعديل الاسم ورسوم الشحن. التفعيل/الإيقاف منفصل عن الحفظ."
        actions={
          <Link
            href="/admin/shipping"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع للقائمة
          </Link>
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <ShippingZoneStatusBadge isActive={zone.isActive} />
        {zone.isActive ? (
          <button
            type="button"
            disabled={submitting}
            onClick={() => setConfirmDeactivate(true)}
            className="rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted disabled:opacity-60"
          >
            إيقاف التفعيل
          </button>
        ) : (
          <button
            type="button"
            disabled={submitting}
            onClick={() => {
              setFormError(null);
              activateMutation.mutate();
            }}
            className="rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted disabled:opacity-60"
          >
            تفعيل
          </button>
        )}
      </div>

      {formSuccess ? (
        <AdminFeedback tone="success">{formSuccess}</AdminFeedback>
      ) : null}
      {formError ? (
        <AdminFeedback tone="error">{formError}</AdminFeedback>
      ) : null}

      <AdminSection title="بيانات المنطقة">
        <FormProvider {...form}>
          <form
            className="max-w-lg space-y-6"
            onSubmit={form.handleSubmit((values) => {
              setFormError(null);
              updateMutation.mutate(values);
            })}
            noValidate
          >
            <ShippingZoneFormFields disabled={submitting} />
            <button
              type="submit"
              disabled={submitting}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
            >
              {updateMutation.isPending ? "جاري الحفظ…" : "حفظ التعديلات"}
            </button>
          </form>
        </FormProvider>
      </AdminSection>

      <AdminConfirmDialog
        open={confirmDeactivate}
        title="إيقاف منطقة الشحن؟"
        description={`سيتم إخفاء «${zone.name}» من خيارات الشحن عند الدفع.`}
        confirmLabel="إيقاف التفعيل"
        tone="danger"
        busy={deactivateMutation.isPending}
        onCancel={() => {
          if (!deactivateMutation.isPending) setConfirmDeactivate(false);
        }}
        onConfirm={() => deactivateMutation.mutate()}
      />
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getShippingErrorMessage(error.code, error.status);
  }
  return getShippingErrorMessage(undefined);
}
