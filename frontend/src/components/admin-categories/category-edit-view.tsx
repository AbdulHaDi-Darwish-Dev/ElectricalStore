"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import { AppPermission, adminOperationalQueryDefaults } from "@/features/admin";
import {
  activateAdminCategory,
  adminCategoryKeys,
  categoryFormSchema,
  deactivateAdminCategory,
  deleteAdminCategoryImage,
  emptyCategoryFormValues,
  getAdminCategory,
  getCategoryErrorMessage,
  toUpdateCategoryRequest,
  updateAdminCategory,
  upsertAdminCategoryImage,
  type CategoryFormValues,
} from "@/features/admin-categories";
import { ApiError } from "@/lib/api";
import {
  AdminConfirmDialog,
  AdminFeedback,
} from "./admin-confirm-dialog";
import { CategoryFormFields } from "./category-form-fields";
import { CategoryImageField } from "./category-image-field";
import { CategoryStatusBadge } from "./category-status-badge";

type CategoryEditViewProps = {
  categoryId: string;
  initialStatus?: string | null;
};

export function CategoryEditView({
  categoryId,
  initialStatus,
}: CategoryEditViewProps) {
  return (
    <AdminPermissionGate anyOf={[AppPermission.categories.manage]}>
      <CategoryEditContent
        categoryId={categoryId}
        initialStatus={initialStatus}
      />
    </AdminPermissionGate>
  );
}

function CategoryEditContent({
  categoryId,
  initialStatus,
}: CategoryEditViewProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [formError, setFormError] = useState<string | null>(() =>
    initialStatus === "created-image-failed"
      ? "تم إنشاء التصنيف، لكن رفع الصورة فشل. يمكنك إعادة المحاولة من هنا."
      : null,
  );
  const [formSuccess, setFormSuccess] = useState<string | null>(null);
  const [confirmRemoveImage, setConfirmRemoveImage] = useState(false);
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);

  const detailQuery = useQuery({
    queryKey: adminCategoryKeys.detail(categoryId),
    queryFn: ({ signal }) => getAdminCategory(categoryId, signal),
    ...adminOperationalQueryDefaults,
  });

  const form = useForm<CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: emptyCategoryFormValues(),
    mode: "onBlur",
  });

  useEffect(() => {
    if (!detailQuery.data) return;
    form.reset({
      name: detailQuery.data.name,
      description: detailQuery.data.description ?? "",
      isActive: detailQuery.data.isActive,
    });
  }, [detailQuery.data, form]);

  async function invalidateAll() {
    await queryClient.invalidateQueries({ queryKey: adminCategoryKeys.all() });
  }

  const updateMutation = useMutation({
    mutationFn: async (values: CategoryFormValues) => {
      const updated = await updateAdminCategory(
        categoryId,
        toUpdateCategoryRequest(values),
      );
      if (selectedFile) {
        return upsertAdminCategoryImage(categoryId, selectedFile);
      }
      return updated;
    },
    onSuccess: async () => {
      setSelectedFile(null);
      setFormError(null);
      setFormSuccess("تم حفظ التعديلات.");
      await invalidateAll();
      router.replace(`/admin/categories?status=updated`);
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(
        error instanceof ApiError
          ? getCategoryErrorMessage(error.code, error.status)
          : getCategoryErrorMessage(undefined),
      );
    },
  });

  const imageDeleteMutation = useMutation({
    mutationFn: () => deleteAdminCategoryImage(categoryId),
    onSuccess: async () => {
      setConfirmRemoveImage(false);
      setFormError(null);
      setFormSuccess("تمت إزالة صورة التصنيف.");
      await invalidateAll();
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(
        error instanceof ApiError
          ? getCategoryErrorMessage(error.code, error.status)
          : getCategoryErrorMessage(undefined),
      );
    },
  });

  const activateMutation = useMutation({
    mutationFn: () => activateAdminCategory(categoryId),
    onSuccess: async () => {
      setFormError(null);
      setFormSuccess("تم تفعيل التصنيف.");
      await invalidateAll();
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(
        error instanceof ApiError
          ? getCategoryErrorMessage(error.code, error.status)
          : getCategoryErrorMessage(undefined),
      );
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: () => deactivateAdminCategory(categoryId),
    onSuccess: async () => {
      setConfirmDeactivate(false);
      setFormError(null);
      setFormSuccess("تم إيقاف تفعيل التصنيف.");
      await invalidateAll();
    },
    onError: (error) => {
      setFormSuccess(null);
      setFormError(
        error instanceof ApiError
          ? getCategoryErrorMessage(error.code, error.status)
          : getCategoryErrorMessage(undefined),
      );
    },
  });

  const submitting =
    updateMutation.isPending ||
    imageDeleteMutation.isPending ||
    activateMutation.isPending ||
    deactivateMutation.isPending;

  if (detailQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل التصنيف…" />;
  }

  if (detailQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل التصنيف"
        message={
          detailQuery.error instanceof ApiError
            ? getCategoryErrorMessage(
                detailQuery.error.code,
                detailQuery.error.status,
              )
            : getCategoryErrorMessage(undefined)
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
              href="/admin/categories"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            >
              العودة للقائمة
            </Link>
          </>
        }
      />
    );
  }

  const category = detailQuery.data!;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title={category.name}
        description="تعديل بيانات التصنيف والصورة وحالة التفعيل."
        actions={
          <Link
            href="/admin/categories"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع للقائمة
          </Link>
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <CategoryStatusBadge
          isActive={category.isActive}
          hasImage={category.hasImage}
        />
        {category.isActive ? (
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

      {formSuccess ? (
        <AdminFeedback tone="success">{formSuccess}</AdminFeedback>
      ) : null}
      {formError ? <AdminFeedback tone="error">{formError}</AdminFeedback> : null}

      <form
        className="space-y-6"
        noValidate
        onSubmit={form.handleSubmit((values) => {
          setFormError(null);
          setFormSuccess(null);
          updateMutation.mutate(values);
        })}
      >
        <AdminSection title="البيانات الأساسية">
          <CategoryFormFields form={form} disabled={submitting} />
        </AdminSection>

        <AdminSection title="الصورة">
          <CategoryImageField
            currentImageUrl={category.imageUrl}
            selectedFile={selectedFile}
            onFileChange={setSelectedFile}
            disabled={submitting}
            removingCurrent={imageDeleteMutation.isPending}
            onRequestRemoveCurrent={
              category.hasImage
                ? () => setConfirmRemoveImage(true)
                : undefined
            }
          />
        </AdminSection>

        <div className="flex flex-wrap gap-2">
          <button
            type="submit"
            disabled={submitting}
            className="rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
          >
            {updateMutation.isPending ? "جاري الحفظ…" : "حفظ التعديلات"}
          </button>
          <Link
            href="/admin/categories"
            className="rounded-md border border-border px-4 py-2.5 text-sm hover:bg-muted"
          >
            إلغاء
          </Link>
        </div>
      </form>

      <AdminConfirmDialog
        open={confirmRemoveImage}
        title="إزالة صورة التصنيف؟"
        description="بدون صورة لن يظهر التصنيف في الكتالوج العام حتى لو كان نشطاً. يمكن رفع صورة جديدة لاحقاً."
        confirmLabel="إزالة الصورة"
        tone="danger"
        busy={imageDeleteMutation.isPending}
        onCancel={() => {
          if (!imageDeleteMutation.isPending) setConfirmRemoveImage(false);
        }}
        onConfirm={() => imageDeleteMutation.mutate()}
      />

      <AdminConfirmDialog
        open={confirmDeactivate}
        title="إيقاف تفعيل التصنيف؟"
        description={`سيتم إخفاء «${category.name}» عن الكتالوج العام. هذا ليس حذفاً نهائياً.`}
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
