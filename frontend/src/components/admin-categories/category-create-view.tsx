"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import { AppPermission } from "@/features/admin";
import {
  adminCategoryKeys,
  categoryFormSchema,
  createAdminCategory,
  emptyCategoryFormValues,
  getCategoryErrorMessage,
  toCreateCategoryRequest,
  upsertAdminCategoryImage,
  type CategoryFormValues,
} from "@/features/admin-categories";
import { ApiError } from "@/lib/api";
import { AdminFeedback } from "./admin-confirm-dialog";
import { CategoryFormFields } from "./category-form-fields";
import { CategoryImageField } from "./category-image-field";

export function CategoryCreateView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.categories.manage]}>
      <CategoryCreateContent />
    </AdminPermissionGate>
  );
}

function CategoryCreateContent() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const form = useForm<CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: emptyCategoryFormValues({ isActive: true }),
    mode: "onBlur",
  });

  const createMutation = useMutation({
    mutationFn: async (values: CategoryFormValues) => {
      const created = await createAdminCategory(toCreateCategoryRequest(values));
      if (selectedFile) {
        try {
          await upsertAdminCategoryImage(created.id, selectedFile);
        } catch (imageError) {
          const message =
            imageError instanceof ApiError
              ? getCategoryErrorMessage(imageError.code, imageError.status)
              : getCategoryErrorMessage(undefined);
          throw Object.assign(new Error(message), {
            kind: "image-after-create" as const,
            categoryId: created.id,
            userMessage: message,
          });
        }
      }
      return created;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: adminCategoryKeys.all() });
      router.replace("/admin/categories?status=created");
    },
    onError: (error) => {
      const imageFail = error as {
        kind?: string;
        categoryId?: string;
        userMessage?: string;
      };
      if (imageFail.kind === "image-after-create" && imageFail.categoryId) {
        void queryClient.invalidateQueries({
          queryKey: adminCategoryKeys.all(),
        });
        router.replace(
          `/admin/categories/${imageFail.categoryId}?status=created-image-failed`,
        );
        return;
      }
      setFormError(
        error instanceof ApiError
          ? getCategoryErrorMessage(error.code, error.status)
          : getCategoryErrorMessage(undefined),
      );
    },
  });

  const submitting = createMutation.isPending || form.formState.isSubmitting;

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="تصنيف جديد"
        description="أنشئ تصنيفاً للكتالوج. يمكن رفع الصورة الآن أو لاحقاً من صفحة التعديل."
        actions={
          <Link
            href="/admin/categories"
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
          <CategoryFormFields
            form={form}
            showActiveToggle
            disabled={submitting}
          />
        </AdminSection>

        <AdminSection title="الصورة">
          <CategoryImageField
            selectedFile={selectedFile}
            onFileChange={setSelectedFile}
            disabled={submitting}
          />
        </AdminSection>

        <div className="flex flex-wrap gap-2">
          <button
            type="submit"
            disabled={submitting}
            className="rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
          >
            {submitting ? "جاري الإنشاء…" : "إنشاء التصنيف"}
          </button>
          <Link
            href="/admin/categories"
            className="rounded-md border border-border px-4 py-2.5 text-sm hover:bg-muted"
          >
            إلغاء
          </Link>
        </div>
      </form>
    </div>
  );
}
