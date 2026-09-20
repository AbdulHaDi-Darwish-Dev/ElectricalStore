"use client";

import Image from "next/image";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useId, useState } from "react";
import {
  AdminConfirmDialog,
} from "@/components/admin-categories";
import { AdminEmptyState, AdminSection } from "@/components/admin";
import {
  addAdminProductImage,
  adminProductKeys,
  deleteAdminProductImage,
  getProductErrorMessage,
  PRODUCT_MAX_IMAGES,
  productImageAcceptAttribute,
  reorderAdminProductImages,
  setPrimaryAdminProductImage,
  validateProductImageFile,
  type AdminProductDto,
  type AdminProductImageDto,
} from "@/features/admin-products";
import { ApiError } from "@/lib/api";

type ProductMediaSectionProps = {
  product: AdminProductDto;
  onFeedback: (tone: "success" | "error", message: string) => void;
};

export function ProductMediaSection({
  product,
  onFeedback,
}: ProductMediaSectionProps) {
  const queryClient = useQueryClient();
  const inputId = useId();
  const [localError, setLocalError] = useState<string | null>(null);
  const [pendingDelete, setPendingDelete] = useState<AdminProductImageDto | null>(
    null,
  );

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: adminProductKeys.all() });
  }

  const uploadMutation = useMutation({
    mutationFn: (file: File) => addAdminProductImage(product.id, file),
    onSuccess: async () => {
      setLocalError(null);
      onFeedback("success", "تم رفع الصورة.");
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

  const deleteMutation = useMutation({
    mutationFn: (imageId: string) =>
      deleteAdminProductImage(product.id, imageId),
    onSuccess: async () => {
      setPendingDelete(null);
      onFeedback("success", "تم حذف الصورة.");
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

  const primaryMutation = useMutation({
    mutationFn: (imageId: string) =>
      setPrimaryAdminProductImage(product.id, imageId),
    onSuccess: async () => {
      onFeedback("success", "تم تعيين الصورة الأساسية.");
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

  const reorderMutation = useMutation({
    mutationFn: (orderedImageIds: string[]) =>
      reorderAdminProductImages(product.id, orderedImageIds),
    onSuccess: async () => {
      onFeedback("success", "تم تحديث ترتيب الصور.");
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
    uploadMutation.isPending ||
    deleteMutation.isPending ||
    primaryMutation.isPending ||
    reorderMutation.isPending;

  const images = [...product.images].sort(
    (a, b) => a.sortOrder - b.sortOrder || a.id.localeCompare(b.id),
  );
  const atLimit = images.length >= PRODUCT_MAX_IMAGES;

  function handleFile(fileList: FileList | null) {
    setLocalError(null);
    const file = fileList?.[0];
    if (!file) return;
    const validation = validateProductImageFile(file, images.length);
    if (!validation.ok) {
      setLocalError(validation.message);
      return;
    }
    uploadMutation.mutate(file);
  }

  function moveImage(imageId: string, direction: -1 | 1) {
    const ids = images.map((i) => i.id);
    const index = ids.indexOf(imageId);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= ids.length) return;
    const next = [...ids];
    const [item] = next.splice(index, 1);
    next.splice(target, 0, item);
    reorderMutation.mutate(next);
  }

  return (
    <AdminSection
      title="الصور"
      description={`حتى ${PRODUCT_MAX_IMAGES} صور · JPEG / PNG / WebP · بحد أقصى 5 ميغابايت. بدون صورة لن يظهر المنتج في المتجر.`}
    >
      {!atLimit ? (
        <div className="mb-4 space-y-2">
          <label htmlFor={inputId} className="text-sm font-medium">
            رفع صورة
          </label>
          <input
            id={inputId}
            type="file"
            accept={productImageAcceptAttribute()}
            disabled={busy}
            className="block w-full text-sm file:me-3 file:rounded-md file:border file:border-border file:bg-card file:px-3 file:py-1.5 file:text-sm"
            onChange={(e) => {
              handleFile(e.target.files);
              e.target.value = "";
            }}
          />
          {localError ? (
            <p className="text-sm text-destructive" role="alert">
              {localError}
            </p>
          ) : null}
          {uploadMutation.isPending ? (
            <p className="text-sm text-muted-foreground" aria-live="polite">
              جاري الرفع…
            </p>
          ) : null}
        </div>
      ) : (
        <p className="mb-4 text-sm text-muted-foreground">
          تم الوصول للحد الأقصى ({PRODUCT_MAX_IMAGES} صور). احذف صورة لإضافة
          أخرى.
        </p>
      )}

      {images.length === 0 ? (
        <AdminEmptyState
          title="لا توجد صور"
          description="ارفع صورة واحدة على الأقل ليكون المنتج مرشحاً للظهور في المتجر العام."
        />
      ) : (
        <ul className="space-y-3">
          {images.map((image, index) => (
            <li
              key={image.id}
              className="flex flex-col gap-3 rounded-md border border-border bg-card p-3 sm:flex-row sm:items-center"
            >
              <div className="relative size-20 shrink-0 overflow-hidden rounded-md border border-border bg-muted">
                <Image
                  src={image.url}
                  alt=""
                  fill
                  sizes="80px"
                  className="object-cover"
                />
              </div>
              <div className="min-w-0 flex-1 space-y-1">
                <p className="text-sm font-medium">
                  {image.isPrimary ? "الصورة الأساسية" : `صورة ${index + 1}`}
                </p>
                <p className="text-xs text-muted-foreground">
                  ترتيب: {image.sortOrder}
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  disabled={busy || index === 0}
                  className="rounded-md border border-border px-2.5 py-1.5 text-xs hover:bg-muted disabled:opacity-60"
                  onClick={() => moveImage(image.id, -1)}
                  aria-label="نقل لأعلى"
                >
                  أعلى
                </button>
                <button
                  type="button"
                  disabled={busy || index === images.length - 1}
                  className="rounded-md border border-border px-2.5 py-1.5 text-xs hover:bg-muted disabled:opacity-60"
                  onClick={() => moveImage(image.id, 1)}
                  aria-label="نقل لأسفل"
                >
                  أسفل
                </button>
                {!image.isPrimary ? (
                  <button
                    type="button"
                    disabled={busy}
                    className="rounded-md border border-border px-2.5 py-1.5 text-xs hover:bg-muted disabled:opacity-60"
                    onClick={() => primaryMutation.mutate(image.id)}
                  >
                    جعلها أساسية
                  </button>
                ) : null}
                <button
                  type="button"
                  disabled={busy}
                  className="rounded-md border border-border px-2.5 py-1.5 text-xs text-destructive hover:bg-muted disabled:opacity-60"
                  onClick={() => setPendingDelete(image)}
                >
                  حذف
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <AdminConfirmDialog
        open={pendingDelete != null}
        title="حذف الصورة؟"
        description={
          pendingDelete
            ? images.length <= 1
              ? "هذه آخر صورة للمنتج. بدونه لن يظهر المنتج في المتجر العام حتى تُرفع صورة جديدة."
              : "سيتم حذف الصورة نهائياً من المنتج."
            : ""
        }
        confirmLabel="حذف الصورة"
        tone="danger"
        busy={deleteMutation.isPending}
        onCancel={() => {
          if (!deleteMutation.isPending) setPendingDelete(null);
        }}
        onConfirm={() => {
          if (pendingDelete) deleteMutation.mutate(pendingDelete.id);
        }}
      />
    </AdminSection>
  );
}
