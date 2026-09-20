"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import {
  AdminConfirmDialog,
} from "@/components/admin-categories";
import { AdminEmptyState, AdminSection } from "@/components/admin";
import {
  activateAdminProductVariant,
  addAdminProductVariant,
  adminProductKeys,
  deactivateAdminProductVariant,
  emptyVariantFormValues,
  getProductErrorMessage,
  productVariantEditFormSchema,
  productVariantFormSchema,
  toCreateVariantRequest,
  toUpdateVariantRequest,
  updateAdminProductVariant,
  type AdminProductDto,
  type AdminProductVariantDto,
  type ProductVariantEditFormValues,
  type ProductVariantFormValues,
} from "@/features/admin-products";
import { ApiError } from "@/lib/api";
import { formatPrice, formatQuantityIncrement, formatSellingUnit } from "@/lib/format";
import { VariantFields } from "./variant-fields";

type ProductVariantsSectionProps = {
  product: AdminProductDto;
  onFeedback: (tone: "success" | "error", message: string) => void;
};

export function ProductVariantsSection({
  product,
  onFeedback,
}: ProductVariantsSectionProps) {
  const queryClient = useQueryClient();
  const [mode, setMode] = useState<"closed" | "add" | "edit">("closed");
  const [editing, setEditing] = useState<AdminProductVariantDto | null>(null);
  const [pendingDeactivate, setPendingDeactivate] =
    useState<AdminProductVariantDto | null>(null);

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: adminProductKeys.all() });
  }

  const addForm = useForm<ProductVariantFormValues>({
    resolver: zodResolver(productVariantFormSchema),
    defaultValues: emptyVariantFormValues(),
    mode: "onBlur",
  });

  const editForm = useForm<ProductVariantEditFormValues>({
    resolver: zodResolver(productVariantEditFormSchema),
    defaultValues: {
      name: "",
      sku: "",
      price: 0,
      sellingUnit: "Piece",
      quantityIncrement: 1,
    },
    mode: "onBlur",
  });

  const addMutation = useMutation({
    mutationFn: (values: ProductVariantFormValues) =>
      addAdminProductVariant(product.id, toCreateVariantRequest(values)),
    onSuccess: async () => {
      setMode("closed");
      addForm.reset(emptyVariantFormValues());
      onFeedback("success", "تمت إضافة الخيار.");
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

  const updateMutation = useMutation({
    mutationFn: (values: ProductVariantEditFormValues) => {
      if (!editing) throw new Error("missing variant");
      return updateAdminProductVariant(
        product.id,
        editing.id,
        toUpdateVariantRequest(values),
      );
    },
    onSuccess: async () => {
      setMode("closed");
      setEditing(null);
      onFeedback("success", "تم تحديث الخيار.");
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

  const activateMutation = useMutation({
    mutationFn: (variantId: string) =>
      activateAdminProductVariant(product.id, variantId),
    onSuccess: async () => {
      onFeedback("success", "تم تفعيل الخيار.");
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
    mutationFn: (variantId: string) =>
      deactivateAdminProductVariant(product.id, variantId),
    onSuccess: async () => {
      setPendingDeactivate(null);
      onFeedback("success", "تم إيقاف تفعيل الخيار.");
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

  const busy =
    addMutation.isPending ||
    updateMutation.isPending ||
    activateMutation.isPending ||
    deactivateMutation.isPending;

  function openEdit(variant: AdminProductVariantDto) {
    setEditing(variant);
    editForm.reset({
      name: variant.name,
      sku: variant.sku,
      price: variant.price,
      sellingUnit: variant.sellingUnit,
      quantityIncrement: variant.quantityIncrement,
    });
    setMode("edit");
  }

  return (
    <AdminSection
      title="الخيارات (Variants)"
      description="كل منتج يحتاج خياراً فعّالاً واحداً على الأقل ليكون قابلاً للشراء."
    >
      <div className="mb-3">
        <button
          type="button"
          disabled={busy || mode !== "closed"}
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
          onClick={() => {
            addForm.reset(emptyVariantFormValues());
            setMode("add");
          }}
        >
          إضافة خيار
        </button>
      </div>

      {mode === "add" ? (
        <form
          className="mb-4 space-y-4 rounded-md border border-border bg-card p-4"
          noValidate
          onSubmit={addForm.handleSubmit((values) => addMutation.mutate(values))}
        >
          <p className="text-sm font-medium">خيار جديد</p>
          <VariantFields
            register={addForm.register}
            errors={addForm.formState.errors}
            watch={addForm.watch}
            setValue={addForm.setValue}
            disabled={busy}
            showActiveToggle
            idPrefix="add-variant"
          />
          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              disabled={busy}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-60"
            >
              {addMutation.isPending ? "جاري الإضافة…" : "حفظ الخيار"}
            </button>
            <button
              type="button"
              disabled={busy}
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
              onClick={() => setMode("closed")}
            >
              إلغاء
            </button>
          </div>
        </form>
      ) : null}

      {mode === "edit" && editing ? (
        <form
          className="mb-4 space-y-4 rounded-md border border-border bg-card p-4"
          noValidate
          onSubmit={editForm.handleSubmit((values) =>
            updateMutation.mutate(values),
          )}
        >
          <p className="text-sm font-medium">تعديل الخيار</p>
          <VariantFields
            register={editForm.register}
            errors={editForm.formState.errors}
            watch={editForm.watch}
            setValue={editForm.setValue}
            disabled={busy}
            showActiveToggle={false}
            idPrefix="edit-variant"
          />
          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              disabled={busy}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground disabled:opacity-60"
            >
              {updateMutation.isPending ? "جاري الحفظ…" : "حفظ التعديلات"}
            </button>
            <button
              type="button"
              disabled={busy}
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
              onClick={() => {
                setMode("closed");
                setEditing(null);
              }}
            >
              إلغاء
            </button>
          </div>
        </form>
      ) : null}

      {product.variants.length === 0 ? (
        <AdminEmptyState
          title="لا توجد خيارات"
          description="المنتج غير قابل للشراء في المتجر حتى يُضاف خيار فعّال."
        />
      ) : (
        <ul className="space-y-2">
          {product.variants.map((variant) => (
            <li
              key={variant.id}
              className="rounded-md border border-border bg-card px-3 py-3"
            >
              <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0 space-y-1">
                  <p className="font-medium text-foreground">{variant.name}</p>
                  <p className="font-mono text-xs text-muted-foreground">
                    SKU: {variant.sku}
                  </p>
                  <p className="text-sm text-foreground">
                    {formatPrice(variant.price)} ·{" "}
                    {formatSellingUnit(variant.sellingUnit)} ·{" "}
                    {formatQuantityIncrement(
                      variant.quantityIncrement,
                      variant.sellingUnit,
                    )}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {variant.isActive ? "فعّال" : "غير فعّال"}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    disabled={busy || mode !== "closed"}
                    className="rounded-md border border-border px-2.5 py-1.5 text-xs hover:bg-muted disabled:opacity-60"
                    onClick={() => openEdit(variant)}
                  >
                    تعديل
                  </button>
                  {variant.isActive ? (
                    <button
                      type="button"
                      disabled={busy}
                      className="rounded-md border border-border px-2.5 py-1.5 text-xs hover:bg-muted disabled:opacity-60"
                      onClick={() => setPendingDeactivate(variant)}
                    >
                      إيقاف التفعيل
                    </button>
                  ) : (
                    <button
                      type="button"
                      disabled={busy}
                      className="rounded-md border border-border px-2.5 py-1.5 text-xs hover:bg-muted disabled:opacity-60"
                      onClick={() => activateMutation.mutate(variant.id)}
                    >
                      تفعيل
                    </button>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}

      <AdminConfirmDialog
        open={pendingDeactivate != null}
        title="إيقاف تفعيل الخيار؟"
        description={
          pendingDeactivate
            ? `سيبقى الخيار «${pendingDeactivate.name}» في الإدارة لكن لن يكون قابلاً للشراء.`
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
    </AdminSection>
  );
}
