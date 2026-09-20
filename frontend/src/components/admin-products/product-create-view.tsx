"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import {
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import { AdminFeedback } from "@/components/admin-categories";
import { AppPermission } from "@/features/admin";
import {
  adminProductKeys,
  createAdminProduct,
  emptyProductCreateFormValues,
  emptyVariantFormValues,
  getProductErrorMessage,
  productCreateFormSchema,
  toCreateProductRequest,
  defaultQuantityIncrement,
  SELLING_UNITS,
  type ProductCreateFormValues,
} from "@/features/admin-products";
import { ApiError } from "@/lib/api";
import { formatSellingUnit } from "@/lib/format";
import { Controller } from "react-hook-form";
import { CategorySelect } from "./category-select";

export function ProductCreateView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.products.manage]}>
      <ProductCreateContent />
    </AdminPermissionGate>
  );
}

function ProductCreateContent() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);

  const form = useForm<ProductCreateFormValues>({
    resolver: zodResolver(productCreateFormSchema),
    defaultValues: emptyProductCreateFormValues(),
    mode: "onBlur",
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: "variants",
  });

  const createMutation = useMutation({
    mutationFn: (values: ProductCreateFormValues) =>
      createAdminProduct(toCreateProductRequest(values)),
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: adminProductKeys.all() });
      router.replace(`/admin/products/${created.id}?status=created`);
    },
    onError: (error) => {
      setFormError(
        error instanceof ApiError
          ? getProductErrorMessage(error.code, error.status)
          : getProductErrorMessage(undefined),
      );
    },
  });

  const submitting = createMutation.isPending || form.formState.isSubmitting;
  const { errors } = form.formState;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="منتج جديد"
        description="يتطلب الإنشاء خياراً واحداً على الأقل. بعد الإنشاء يمكنك إضافة الصور وخيارات إضافية."
        actions={
          <Link
            href="/admin/products"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع للقائمة
          </Link>
        }
      />

      {formError ? <AdminFeedback tone="error">{formError}</AdminFeedback> : null}

      <form
        className="space-y-6"
        noValidate
        onSubmit={form.handleSubmit((values) => {
          setFormError(null);
          createMutation.mutate(values);
        })}
      >
        <AdminSection title="البيانات الأساسية">
          <div className="space-y-5">
            <div className="space-y-2">
              <label htmlFor="product-name" className="text-sm font-medium">
                اسم المنتج
              </label>
              <input
                id="product-name"
                disabled={submitting}
                className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
                {...form.register("name")}
              />
              {errors.name ? (
                <p className="text-sm text-destructive" role="alert">
                  {errors.name.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-2">
              <label htmlFor="product-description" className="text-sm font-medium">
                الوصف{" "}
                <span className="font-normal text-muted-foreground">(اختياري)</span>
              </label>
              <textarea
                id="product-description"
                rows={3}
                disabled={submitting}
                className="w-full resize-y rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
                {...form.register("description")}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="product-category" className="text-sm font-medium">
                التصنيف
              </label>
              <Controller
                name="categoryId"
                control={form.control}
                render={({ field }) => (
                  <CategorySelect
                    id="product-category"
                    value={field.value}
                    onChange={field.onChange}
                    disabled={submitting}
                    invalid={!!errors.categoryId}
                  />
                )}
              />
              {errors.categoryId ? (
                <p className="text-sm text-destructive" role="alert">
                  {errors.categoryId.message}
                </p>
              ) : null}
            </div>

            <label className="flex items-start gap-3 rounded-md border border-border bg-card px-3 py-3 text-sm">
              <input
                type="checkbox"
                className="mt-1 size-4 accent-primary"
                disabled={submitting}
                {...form.register("isActive")}
              />
              <span>
                <span className="font-medium">نشط عند الإنشاء</span>
                <span className="mt-1 block text-muted-foreground">
                  لن يظهر المنتج في المتجر حتى تُضاف صورة وخيار فعّال وتصنيف فعّال.
                </span>
              </span>
            </label>
          </div>
        </AdminSection>

        <AdminSection
          title="الخيارات (Variants)"
          description="مطلوب خيار واحد على الأقل. رمز SKU فريد عالمياً."
        >
          <div className="space-y-4">
            {fields.map((field, index) => (
                <div
                  key={field.id}
                  className="space-y-3 rounded-md border border-border bg-card p-4"
                >
                  <div className="flex items-center justify-between gap-2">
                    <p className="text-sm font-medium">خيار {index + 1}</p>
                    {fields.length > 1 ? (
                      <button
                        type="button"
                        disabled={submitting}
                        className="text-sm text-destructive hover:underline disabled:opacity-60"
                        onClick={() => remove(index)}
                      >
                        إزالة
                      </button>
                    ) : null}
                  </div>

                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="space-y-1">
                      <label className="text-xs font-medium">الاسم</label>
                      <input
                        disabled={submitting}
                        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        {...form.register(`variants.${index}.name`)}
                      />
                      {errors.variants?.[index]?.name ? (
                        <p className="text-xs text-destructive" role="alert">
                          {errors.variants[index]?.name?.message}
                        </p>
                      ) : null}
                    </div>
                    <div className="space-y-1">
                      <label className="text-xs font-medium">SKU</label>
                      <input
                        disabled={submitting}
                        className="w-full rounded-md border border-border bg-background px-3 py-2 font-mono text-sm"
                        {...form.register(`variants.${index}.sku`)}
                      />
                      {errors.variants?.[index]?.sku ? (
                        <p className="text-xs text-destructive" role="alert">
                          {errors.variants[index]?.sku?.message}
                        </p>
                      ) : null}
                    </div>
                  </div>

                  <div className="grid gap-3 sm:grid-cols-3">
                    <div className="space-y-1">
                      <label className="text-xs font-medium">السعر</label>
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        disabled={submitting}
                        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        {...form.register(`variants.${index}.price`, {
                          valueAsNumber: true,
                        })}
                      />
                      {errors.variants?.[index]?.price ? (
                        <p className="text-xs text-destructive" role="alert">
                          {errors.variants[index]?.price?.message}
                        </p>
                      ) : null}
                    </div>
                    <div className="space-y-1">
                      <label className="text-xs font-medium">وحدة البيع</label>
                      <select
                        disabled={submitting}
                        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        {...form.register(`variants.${index}.sellingUnit`, {
                          onChange: (e) => {
                            const u = e.target.value as "Piece" | "Meter";
                            form.setValue(
                              `variants.${index}.quantityIncrement`,
                              defaultQuantityIncrement(u),
                              { shouldValidate: true },
                            );
                          },
                        })}
                      >
                        {SELLING_UNITS.map((u) => (
                          <option key={u} value={u}>
                            {formatSellingUnit(u)}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="space-y-1">
                      <label className="text-xs font-medium">خطوة الكمية</label>
                      <Controller
                        control={form.control}
                        name={`variants.${index}.quantityIncrement`}
                        render={({ field }) => {
                          const unit = form.getValues(
                            `variants.${index}.sellingUnit`,
                          );
                          return (
                            <input
                              type="number"
                              step="any"
                              min="0"
                              disabled={submitting || unit === "Piece"}
                              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
                              name={field.name}
                              ref={field.ref}
                              value={Number.isFinite(field.value) ? field.value : ""}
                              onBlur={field.onBlur}
                              onChange={(e) =>
                                field.onChange(
                                  e.target.value === ""
                                    ? Number.NaN
                                    : e.target.valueAsNumber,
                                )
                              }
                            />
                          );
                        }}
                      />
                      {errors.variants?.[index]?.quantityIncrement ? (
                        <p className="text-xs text-destructive" role="alert">
                          {errors.variants[index]?.quantityIncrement?.message}
                        </p>
                      ) : (
                        <p className="text-[11px] text-muted-foreground">
                          أقل خطوة زيادة عند الشراء
                        </p>
                      )}
                    </div>
                  </div>

                  <label className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      className="size-4 accent-primary"
                      disabled={submitting}
                      {...form.register(`variants.${index}.isActive`)}
                    />
                    خيار فعّال
                  </label>
                </div>
              ))}

            {errors.variants?.root || errors.variants?.message ? (
              <p className="text-sm text-destructive" role="alert">
                {errors.variants.root?.message ?? errors.variants.message}
              </p>
            ) : null}

            <button
              type="button"
              disabled={submitting}
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
              onClick={() => append(emptyVariantFormValues())}
            >
              إضافة خيار آخر
            </button>
          </div>
        </AdminSection>

        <div className="flex flex-wrap gap-2">
          <button
            type="submit"
            disabled={submitting}
            className="rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
          >
            {submitting ? "جاري الإنشاء…" : "إنشاء المنتج"}
          </button>
          <Link
            href="/admin/products"
            className="rounded-md border border-border px-4 py-2.5 text-sm hover:bg-muted"
          >
            إلغاء
          </Link>
        </div>
      </form>
    </div>
  );
}
