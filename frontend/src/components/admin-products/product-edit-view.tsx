"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
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
  canAccessInventory,
} from "@/features/admin";
import {
  activateAdminProduct,
  adminProductKeys,
  deactivateAdminProduct,
  emptyProductBasicsFormValues,
  getAdminProduct,
  getProductErrorMessage,
  getProductReadiness,
  productBasicsFormSchema,
  toUpdateProductRequest,
  updateAdminProduct,
  type ProductBasicsFormValues,
} from "@/features/admin-products";
import { ApiError } from "@/lib/api";
import { useAuthStore } from "@/lib/auth";
import { CategorySelect } from "./category-select";
import { ProductMediaSection } from "./product-media-section";
import { ProductReadinessPanel } from "./product-readiness";
import { ProductStatusBadge } from "./product-status-badge";
import { ProductVariantsSection } from "./product-variants-section";

type ProductEditViewProps = {
  productId: string;
  initialStatus?: string | null;
};

export function ProductEditView({
  productId,
  initialStatus,
}: ProductEditViewProps) {
  return (
    <AdminPermissionGate anyOf={[AppPermission.products.manage]}>
      <ProductEditContent
        productId={productId}
        initialStatus={initialStatus}
      />
    </AdminPermissionGate>
  );
}

function ProductEditContent({
  productId,
  initialStatus,
}: ProductEditViewProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const permissions = useAuthStore((s) => s.permissions);
  const showInventoryLink = canAccessInventory(permissions);
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState<string | null>(() =>
    initialStatus === "created"
      ? "تم إنشاء المنتج. أضف صوراً وراجع الخيارات لإكماله."
      : null,
  );
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);

  const detailQuery = useQuery({
    queryKey: adminProductKeys.detail(productId),
    queryFn: ({ signal }) => getAdminProduct(productId, signal),
    ...adminOperationalQueryDefaults,
  });

  const form = useForm<ProductBasicsFormValues>({
    resolver: zodResolver(productBasicsFormSchema),
    defaultValues: emptyProductBasicsFormValues(),
    mode: "onBlur",
  });

  useEffect(() => {
    if (!detailQuery.data) return;
    form.reset({
      name: detailQuery.data.name,
      description: detailQuery.data.description ?? "",
      categoryId: detailQuery.data.categoryId,
      isActive: detailQuery.data.isActive,
    });
  }, [detailQuery.data, form]);

  function onFeedback(tone: "success" | "error", message: string) {
    if (tone === "success") {
      setFormError(null);
      setFormSuccess(message);
    } else {
      setFormSuccess(null);
      setFormError(message);
    }
  }

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: adminProductKeys.all() });
  }

  const updateMutation = useMutation({
    mutationFn: (values: ProductBasicsFormValues) =>
      updateAdminProduct(productId, toUpdateProductRequest(values)),
    onSuccess: async () => {
      setFormError(null);
      setFormSuccess("تم حفظ البيانات الأساسية.");
      await invalidate();
      router.replace(`/admin/products?status=updated`);
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(
        error instanceof ApiError
          ? getProductErrorMessage(error.code, error.status)
          : getProductErrorMessage(undefined),
      );
    },
  });

  const activateMutation = useMutation({
    mutationFn: () => activateAdminProduct(productId),
    onSuccess: async () => {
      onFeedback("success", "تم تفعيل المنتج.");
      await invalidate();
    },
    onError: (error) => {
      onFeedback(
        "error",
        error instanceof ApiError
          ? getProductErrorMessage(error.code, error.status)
          : getProductErrorMessage(undefined),
      );
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: () => deactivateAdminProduct(productId),
    onSuccess: async () => {
      setConfirmDeactivate(false);
      onFeedback("success", "تم إيقاف تفعيل المنتج.");
      await invalidate();
    },
    onError: (error) => {
      onFeedback(
        "error",
        error instanceof ApiError
          ? getProductErrorMessage(error.code, error.status)
          : getProductErrorMessage(undefined),
      );
    },
  });

  const submitting =
    updateMutation.isPending ||
    activateMutation.isPending ||
    deactivateMutation.isPending;

  if (detailQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل المنتج…" />;
  }

  if (detailQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل المنتج"
        message={
          detailQuery.error instanceof ApiError
            ? getProductErrorMessage(
                detailQuery.error.code,
                detailQuery.error.status,
              )
            : getProductErrorMessage(undefined)
        }
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
              href="/admin/products"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            >
              العودة للقائمة
            </Link>
          </>
        }
      />
    );
  }

  const product = detailQuery.data!;
  const readiness = getProductReadiness(product);

  return (
    <div className="space-y-8">
      <AdminPageHeader
        title={product.name}
        description={`التصنيف: ${product.categoryName}${product.categoryIsActive ? "" : " (غير نشط)"}`}
        actions={
          <div className="flex flex-wrap gap-2">
            {showInventoryLink ? (
              <Link
                href={`/admin/inventory?productId=${encodeURIComponent(productId)}`}
                className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
              >
                مخزون هذا المنتج
              </Link>
            ) : null}
            <Link
              href="/admin/products"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            >
              رجوع للقائمة
            </Link>
          </div>
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <ProductStatusBadge
          isActive={product.isActive}
          likelyPublic={readiness.likelyPublic}
        />
        {product.isActive ? (
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
            onClick={() => activateMutation.mutate()}
            className="rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted disabled:opacity-60"
          >
            {activateMutation.isPending ? "جاري التفعيل…" : "تفعيل"}
          </button>
        )}
      </div>

      <ProductReadinessPanel product={product} />

      {formSuccess ? (
        <AdminFeedback tone="success">{formSuccess}</AdminFeedback>
      ) : null}
      {formError ? <AdminFeedback tone="error">{formError}</AdminFeedback> : null}

      <form
        className="space-y-4"
        noValidate
        onSubmit={form.handleSubmit((values) => {
          setFormError(null);
          setFormSuccess(null);
          updateMutation.mutate(values);
        })}
      >
        <AdminSection title="البيانات الأساسية">
          <div className="space-y-5">
            <div className="space-y-2">
              <label htmlFor="edit-product-name" className="text-sm font-medium">
                اسم المنتج
              </label>
              <input
                id="edit-product-name"
                disabled={submitting}
                className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
                {...form.register("name")}
              />
              {form.formState.errors.name ? (
                <p className="text-sm text-destructive" role="alert">
                  {form.formState.errors.name.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-2">
              <label
                htmlFor="edit-product-description"
                className="text-sm font-medium"
              >
                الوصف{" "}
                <span className="font-normal text-muted-foreground">
                  (اختياري)
                </span>
              </label>
              <textarea
                id="edit-product-description"
                rows={3}
                disabled={submitting}
                className="w-full resize-y rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
                {...form.register("description")}
              />
            </div>

            <div className="space-y-2">
              <label
                htmlFor="edit-product-category"
                className="text-sm font-medium"
              >
                التصنيف
              </label>
              <Controller
                name="categoryId"
                control={form.control}
                render={({ field }) => (
                  <CategorySelect
                    id="edit-product-category"
                    value={field.value}
                    onChange={field.onChange}
                    disabled={submitting}
                    invalid={!!form.formState.errors.categoryId}
                  />
                )}
              />
              {form.formState.errors.categoryId ? (
                <p className="text-sm text-destructive" role="alert">
                  {form.formState.errors.categoryId.message}
                </p>
              ) : null}
            </div>
          </div>
        </AdminSection>

        <button
          type="submit"
          disabled={submitting}
          className="rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
        >
          {updateMutation.isPending ? "جاري الحفظ…" : "حفظ البيانات الأساسية"}
        </button>
      </form>

      <ProductVariantsSection product={product} onFeedback={onFeedback} />
      <ProductMediaSection product={product} onFeedback={onFeedback} />

      <AdminConfirmDialog
        open={confirmDeactivate}
        title="إيقاف تفعيل المنتج؟"
        description={`سيتم إخفاء «${product.name}» عن الكتالوج العام. هذا ليس حذفاً نهائياً.`}
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
