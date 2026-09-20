"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import { AdminFeedback } from "@/components/admin-categories";
import { AppPermission } from "@/features/admin";
import {
  adminShippingKeys,
  createAdminShippingZone,
  emptyShippingZoneFormValues,
  getShippingErrorMessage,
  shippingZoneFormSchema,
  toCreateShippingZoneRequest,
  type ShippingZoneFormValues,
} from "@/features/admin-shipping";
import { ApiError } from "@/lib/api";
import { ShippingZoneFormFields } from "./shipping-zone-form-fields";

export function ShippingZoneCreateView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.shipping.manage]}>
      <ShippingZoneCreateContent />
    </AdminPermissionGate>
  );
}

function ShippingZoneCreateContent() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);

  const form = useForm<ShippingZoneFormValues>({
    resolver: zodResolver(shippingZoneFormSchema),
    defaultValues: emptyShippingZoneFormValues({ isActive: true }),
    mode: "onBlur",
  });

  const createMutation = useMutation({
    mutationFn: (values: ShippingZoneFormValues) =>
      createAdminShippingZone(toCreateShippingZoneRequest(values)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: adminShippingKeys.all(),
      });
      router.replace("/admin/shipping?status=created");
    },
    onError: (error) => {
      setFormError(
        error instanceof ApiError
          ? getShippingErrorMessage(error.code, error.status)
          : getShippingErrorMessage(undefined),
      );
    },
  });

  const submitting = createMutation.isPending || form.formState.isSubmitting;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="منطقة شحن جديدة"
        description="أضف منطقة توصيل برسوم ثابتة. يمكن تفعيلها فوراً أو لاحقاً."
        actions={
          <Link
            href="/admin/shipping"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع للقائمة
          </Link>
        }
      />

      {formError ? (
        <AdminFeedback tone="error">{formError}</AdminFeedback>
      ) : null}

      <AdminSection title="بيانات المنطقة">
        <FormProvider {...form}>
          <form
            className="max-w-lg space-y-6"
            onSubmit={form.handleSubmit((values) => {
              setFormError(null);
              createMutation.mutate(values);
            })}
            noValidate
          >
            <ShippingZoneFormFields
              showActiveToggle
              disabled={submitting}
            />
            <div className="flex flex-wrap gap-2">
              <button
                type="submit"
                disabled={submitting}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
              >
                {submitting ? "جاري الإنشاء…" : "إنشاء المنطقة"}
              </button>
              <Link
                href="/admin/shipping"
                className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
              >
                إلغاء
              </Link>
            </div>
          </form>
        </FormProvider>
      </AdminSection>
    </div>
  );
}
