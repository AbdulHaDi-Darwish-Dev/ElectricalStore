"use client";

import Image from "next/image";
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
  activateAdminCategory,
  adminCategoryKeys,
  deactivateAdminCategory,
  getCategoryErrorMessage,
  listAdminCategories,
  type AdminCategoryDto,
} from "@/features/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import { ApiError } from "@/lib/api";
import {
  AdminConfirmDialog,
  AdminFeedback,
} from "./admin-confirm-dialog";
import { CategoryStatusBadge } from "./category-status-badge";

export function CategoriesListView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.categories.manage]}>
      <CategoriesListContent />
    </AdminPermissionGate>
  );
}

function CategoriesListContent() {
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const flash = searchParams.get("status");

  const listQuery = useQuery({
    queryKey: adminCategoryKeys.list(),
    queryFn: ({ signal }) => listAdminCategories(signal),
    ...adminOperationalQueryDefaults,
  });

  const [pendingDeactivate, setPendingDeactivate] =
    useState<AdminCategoryDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateAdminCategory(id),
    onSuccess: async () => {
      setActionError(null);
      setActionSuccess("تم تفعيل التصنيف.");
      await queryClient.invalidateQueries({ queryKey: adminCategoryKeys.all() });
    },
    onError: (error) => {
      setActionSuccess(null);
      setActionError(mapError(error));
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateAdminCategory(id),
    onSuccess: async () => {
      setPendingDeactivate(null);
      setActionError(null);
      setActionSuccess("تم إيقاف تفعيل التصنيف. لن يظهر في الكتالوج العام.");
      await queryClient.invalidateQueries({ queryKey: adminCategoryKeys.all() });
    },
    onError: (error) => {
      setActionSuccess(null);
      setActionError(mapError(error));
    },
  });

  const busy =
    activateMutation.isPending || deactivateMutation.isPending;

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل التصنيفات…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل التصنيفات"
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

  const categories = listQuery.data ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="التصنيفات"
        description="إدارة تصنيفات الكتالوج. التعديلات تظهر في المتجر العام بعد إعادة التحقق (حتى دقيقة تقريباً)."
        actions={
          <Link
            href="/admin/categories/new"
            className="inline-flex items-center justify-center rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
          >
            تصنيف جديد
          </Link>
        }
      />

      {flash === "created" ? (
        <AdminFeedback tone="success">تم إنشاء التصنيف بنجاح.</AdminFeedback>
      ) : null}
      {flash === "updated" ? (
        <AdminFeedback tone="success">تم حفظ تعديلات التصنيف.</AdminFeedback>
      ) : null}
      {actionSuccess ? (
        <AdminFeedback tone="success">{actionSuccess}</AdminFeedback>
      ) : null}
      {actionError ? (
        <AdminFeedback tone="error">{actionError}</AdminFeedback>
      ) : null}

      {categories.length === 0 ? (
        <AdminEmptyState
          title="لا توجد تصنيفات بعد"
          description="أنشئ أول تصنيف لبدء تنظيم كتالوج المنتجات."
          actions={
            <Link
              href="/admin/categories/new"
              className="inline-flex rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
            >
              إنشاء تصنيف
            </Link>
          }
        />
      ) : (
        <>
          {/* Desktop table */}
          <div className="hidden overflow-hidden rounded-md border border-border md:block">
            <table className="w-full text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">التصنيف</th>
                  <th className="px-3 py-2.5 font-medium">الحالة</th>
                  <th className="px-3 py-2.5 font-medium">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {categories.map((category) => (
                  <tr
                    key={category.id}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-3">
                        <CategoryThumb category={category} />
                        <div className="min-w-0">
                          <p className="truncate font-medium text-foreground">
                            {category.name}
                          </p>
                          {category.description ? (
                            <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                              {category.description}
                            </p>
                          ) : null}
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      <CategoryStatusBadge
                        isActive={category.isActive}
                        hasImage={category.hasImage}
                      />
                    </td>
                    <td className="px-3 py-3">
                      <CategoryRowActions
                        category={category}
                        busy={busy}
                        onActivate={() => {
                          setActionError(null);
                          activateMutation.mutate(category.id);
                        }}
                        onRequestDeactivate={() => {
                          setActionError(null);
                          setPendingDeactivate(category);
                        }}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <ul className="space-y-3 md:hidden">
            {categories.map((category) => (
              <li
                key={category.id}
                className="rounded-md border border-border bg-card p-3"
              >
                <div className="flex gap-3">
                  <CategoryThumb category={category} />
                  <div className="min-w-0 flex-1 space-y-2">
                    <p className="font-medium text-foreground">
                      {category.name}
                    </p>
                    {category.description ? (
                      <p className="line-clamp-2 text-xs text-muted-foreground">
                        {category.description}
                      </p>
                    ) : null}
                    <CategoryStatusBadge
                      isActive={category.isActive}
                      hasImage={category.hasImage}
                    />
                    <CategoryRowActions
                      category={category}
                      busy={busy}
                      onActivate={() => {
                        setActionError(null);
                        activateMutation.mutate(category.id);
                      }}
                      onRequestDeactivate={() => {
                        setActionError(null);
                        setPendingDeactivate(category);
                      }}
                    />
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </>
      )}

      <AdminConfirmDialog
        open={pendingDeactivate != null}
        title="إيقاف تفعيل التصنيف؟"
        description={
          pendingDeactivate
            ? `سيتم إخفاء «${pendingDeactivate.name}» عن الكتالوج العام. هذا ليس حذفاً نهائياً — يمكن إعادة التفعيل لاحقاً.`
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

function CategoryThumb({ category }: { category: AdminCategoryDto }) {
  return (
    <div className="relative size-12 shrink-0 overflow-hidden rounded-md border border-border bg-muted">
      {category.imageUrl ? (
        <Image
          src={category.imageUrl}
          alt=""
          fill
          sizes="48px"
          className="object-cover"
        />
      ) : (
        <span className="flex size-full items-center justify-center text-[10px] text-muted-foreground">
          —
        </span>
      )}
    </div>
  );
}

function CategoryRowActions({
  category,
  busy,
  onActivate,
  onRequestDeactivate,
}: {
  category: AdminCategoryDto;
  busy: boolean;
  onActivate: () => void;
  onRequestDeactivate: () => void;
}) {
  return (
    <div className="flex flex-wrap gap-2">
      <Link
        href={`/admin/categories/${category.id}`}
        className="rounded-md border border-border px-2.5 py-1.5 text-xs font-medium hover:bg-muted"
      >
        تعديل
      </Link>
      {category.isActive ? (
        <button
          type="button"
          disabled={busy}
          onClick={onRequestDeactivate}
          className="rounded-md border border-border px-2.5 py-1.5 text-xs font-medium hover:bg-muted disabled:opacity-60"
        >
          إيقاف التفعيل
        </button>
      ) : (
        <button
          type="button"
          disabled={busy}
          onClick={onActivate}
          className="rounded-md border border-border px-2.5 py-1.5 text-xs font-medium hover:bg-muted disabled:opacity-60"
        >
          تفعيل
        </button>
      )}
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getCategoryErrorMessage(error.code, error.status);
  }
  return getCategoryErrorMessage(undefined);
}
