"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import {
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import { AdminFeedback } from "@/components/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import {
  adminSettingsKeys,
  getOrderingSettings,
  getSettingsErrorMessage,
  isNoMinimumMerchandise,
  orderingSettingsFormSchema,
  toOrderingFormValues,
  toUpdateOrderingSettingsRequest,
  updateOrderingSettings,
  type OrderingSettingsFormValues,
} from "@/features/admin-settings";
import { storeConfig } from "@/config/store";
import { ApiError } from "@/lib/api";
import { formatPrice } from "@/lib/format";

export function SettingsView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.settings.manage]}>
      <SettingsContent />
    </AdminPermissionGate>
  );
}

function SettingsContent() {
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState<string | null>(null);

  const settingsQuery = useQuery({
    queryKey: adminSettingsKeys.ordering(),
    queryFn: ({ signal }) => getOrderingSettings(signal),
    ...adminOperationalQueryDefaults,
  });

  const form = useForm<OrderingSettingsFormValues>({
    resolver: zodResolver(orderingSettingsFormSchema),
    defaultValues: { minimumMerchandiseSubtotal: 0 },
    mode: "onBlur",
  });

  useEffect(() => {
    if (!settingsQuery.data) return;
    if (form.formState.isDirty) return;
    form.reset(toOrderingFormValues(settingsQuery.data));
  }, [settingsQuery.data, form, form.formState.isDirty]);

  const saveMutation = useMutation({
    mutationFn: (values: OrderingSettingsFormValues) =>
      updateOrderingSettings(toUpdateOrderingSettingsRequest(values)),
    onSuccess: async (dto) => {
      setFormError(null);
      setFormSuccess("تم حفظ إعدادات الطلب.");
      form.reset(toOrderingFormValues(dto));
      await queryClient.invalidateQueries({
        queryKey: adminSettingsKeys.all(),
      });
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(mapError(error));
    },
  });

  if (settingsQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل الإعدادات…" />;
  }

  if (settingsQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل الإعدادات"
        message={mapError(settingsQuery.error)}
        actions={
          <button
            type="button"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            onClick={() => void settingsQuery.refetch()}
          >
            إعادة المحاولة
          </button>
        }
      />
    );
  }

  const current = settingsQuery.data!;
  const dirty = form.formState.isDirty;
  const submitting = saveMutation.isPending || form.formState.isSubmitting;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="الإعدادات"
        description="إعدادات تشغيل الطلبات. التغييرات تُطبَّق فوراً على معاينة الدفع وإنشاء الطلب عبر الخادم."
      />

      {formSuccess ? (
        <AdminFeedback tone="success">{formSuccess}</AdminFeedback>
      ) : null}
      {formError ? (
        <AdminFeedback tone="error">{formError}</AdminFeedback>
      ) : null}

      <AdminSection title="إعدادات الطلب">
        <p className="mb-4 text-sm leading-6 text-muted-foreground">
          تتحكم هذه القيم في قواعد قبول الطلب قبل الشحن. مناطق الشحن ورسومها تُدار من
          وحدة الشحن، وليست هنا.
        </p>

        <form
          className="max-w-lg space-y-5"
          onSubmit={form.handleSubmit((values) => {
            setFormError(null);
            setFormSuccess(null);
            saveMutation.mutate(values);
          })}
          noValidate
        >
          <div className="space-y-1.5">
            <label
              htmlFor="minimumMerchandiseSubtotal"
              className="block text-sm font-medium"
            >
              الحد الأدنى لقيمة المنتجات ({storeConfig.currencyDisplay})
            </label>
            <Controller
              name="minimumMerchandiseSubtotal"
              control={form.control}
              render={({ field, fieldState }) => {
                const value =
                  typeof field.value === "number" && Number.isFinite(field.value)
                    ? field.value
                    : null;
                return (
                  <>
                    <input
                      id="minimumMerchandiseSubtotal"
                      type="number"
                      min={0}
                      step="0.01"
                      inputMode="decimal"
                      disabled={submitting}
                      className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
                      aria-invalid={fieldState.error ? true : undefined}
                      aria-describedby="minimum-hint"
                      name={field.name}
                      ref={field.ref}
                      onBlur={field.onBlur}
                      value={value ?? ""}
                      onChange={(e) => {
                        const raw = e.target.value;
                        if (raw === "") {
                          field.onChange(undefined);
                          return;
                        }
                        field.onChange(e.target.valueAsNumber);
                      }}
                    />
                    <p
                      id="minimum-hint"
                      className="text-xs leading-5 text-muted-foreground"
                    >
                      الحد الأدنى لمجموع قيمة المنتجات في الطلب قبل رسوم الشحن.
                      القيمة الحالية المحفوظة:{" "}
                      {formatPrice(current.minimumMerchandiseSubtotal)}
                      {isNoMinimumMerchandise(
                        current.minimumMerchandiseSubtotal,
                      )
                        ? " (لا يوجد حد أدنى)."
                        : "."}
                    </p>
                    {value !== null &&
                    isNoMinimumMerchandise(value) &&
                    dirty ? (
                      <p className="text-xs leading-5 text-muted-foreground">
                        صفر يعني عدم فرض حد أدنى على قيمة المنتجات.
                      </p>
                    ) : null}
                    {fieldState.error ? (
                      <p className="text-sm text-destructive" role="alert">
                        {fieldState.error.message}
                      </p>
                    ) : null}
                  </>
                );
              }}
            />
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <button
              type="submit"
              disabled={submitting || !dirty}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
            >
              {submitting ? "جاري الحفظ…" : "حفظ الإعدادات"}
            </button>
            {dirty ? (
              <button
                type="button"
                disabled={submitting}
                onClick={() => {
                  form.reset(toOrderingFormValues(current));
                  setFormError(null);
                  setFormSuccess(null);
                }}
                className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
              >
                إلغاء التعديلات
              </button>
            ) : (
              <span className="text-xs text-muted-foreground">لا تغييرات غير محفوظة</span>
            )}
          </div>
        </form>
      </AdminSection>
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getSettingsErrorMessage(error.code, error.status);
  }
  return getSettingsErrorMessage(undefined);
}
