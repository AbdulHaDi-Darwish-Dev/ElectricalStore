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
  AdminConfirmDialog,
  AdminFeedback,
} from "@/components/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import {
  activateAdminProduct,
  adminProductKeys,
  deactivateAdminProduct,
  getProductErrorMessage,
  listAdminProducts,
  type AdminProductListItemDto,
} from "@/features/admin-products";
import { ApiError } from "@/lib/api";
import { ProductStatusBadge } from "./product-status-badge";

export function ProductsListView() {
  return (
    <AdminPermissionGate anyOf={[AppPermission.products.manage]}>
      <ProductsListContent />
    </AdminPermissionGate>
  );
}

function ProductsListContent() {
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const flash = searchParams.get("status");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [pendingDeactivate, setPendingDeactivate] =
    useState<AdminProductListItemDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const listQuery = useQuery({
    queryKey: adminProductKeys.list({ search: appliedSearch || undefined }),
    queryFn: ({ signal }) =>
      listAdminProducts(
        { search: appliedSearch || undefined },
        signal,
      ),
    ...adminOperationalQueryDefaults,
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateAdminProduct(id),
    onSuccess: async () => {
      setActionError(null);
      setActionSuccess("تم تفعيل المنتج.");
      await queryClient.invalidateQueries({ queryKey: adminProductKeys.all() });
    },
    onError: (error) => {
      setActionSuccess(null);
      setActionError(mapError(error));
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateAdminProduct(id),
    onSuccess: async () => {
      setPendingDeactivate(null);
      setActionError(null);
      setActionSuccess("تم إيقاف تفعيل المنتج.");
      await queryClient.invalidateQueries({ queryKey: adminProductKeys.all() });
    },
    onError: (error) => {
      setActionSuccess(null);
      setActionError(mapError(error));
    },
  });

  const busy = activateMutation.isPending || deactivateMutation.isPending;

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل المنتجات…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل المنتجات"
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

  const products = listQuery.data ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="المنتجات"
        description="إدارة المنتجات والخيارات والصور. التغييرات في المتجر العام قد تتأخر حتى دقيقة تقريباً."
        actions={
          <Link
            href="/admin/products/new"
            className="inline-flex items-center justify-center rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
          >
            منتج جديد
          </Link>
        }
      />

      <form
        className="flex flex-wrap gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setAppliedSearch(search.trim());
        }}
      >
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="بحث بالاسم…"
          className="min-w-[12rem] flex-1 rounded-md border border-border bg-background px-3 py-2 text-sm"
          aria-label="بحث عن منتج"
        />
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          بحث
        </button>
      </form>

      {flash === "created" ? (
        <AdminFeedback tone="success">تم إنشاء المنتج بنجاح.</AdminFeedback>
      ) : null}
      {flash === "updated" ? (
        <AdminFeedback tone="success">تم حفظ تعديلات المنتج.</AdminFeedback>
      ) : null}
      {actionSuccess ? (
        <AdminFeedback tone="success">{actionSuccess}</AdminFeedback>
      ) : null}
      {actionError ? (
        <AdminFeedback tone="error">{actionError}</AdminFeedback>
      ) : null}

      {products.length === 0 ? (
        <AdminEmptyState
          title="لا توجد منتجات"
          description={
            appliedSearch
              ? "لا نتائج مطابقة لبحثك."
              : "أنشئ أول منتج لبدء بناء الكتالوج."
          }
          actions={
            <Link
              href="/admin/products/new"
              className="inline-flex rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
            >
              إنشاء منتج
            </Link>
          }
        />
      ) : (
        <>
          <div className="hidden overflow-hidden rounded-md border border-border md:block">
            <table className="w-full text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">المنتج</th>
                  <th className="px-3 py-2.5 font-medium">التصنيف</th>
                  <th className="px-3 py-2.5 font-medium">الحالة</th>
                  <th className="px-3 py-2.5 font-medium">خيارات / صور</th>
                  <th className="px-3 py-2.5 font-medium">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {products.map((product) => (
                  <tr
                    key={product.id}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-3">
                        <ProductThumb product={product} />
                        <div className="min-w-0">
                          <p className="truncate font-medium">{product.name}</p>
                          {product.description ? (
                            <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                              {product.description}
                            </p>
                          ) : null}
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-3 text-muted-foreground">
                      {product.categoryName}
                    </td>
                    <td className="px-3 py-3">
                      <ProductStatusBadge isActive={product.isActive} />
                    </td>
                    <td className="px-3 py-3 text-xs text-muted-foreground">
                      {product.variantCount} خيار · {product.imageCount} صورة
                    </td>
                    <td className="px-3 py-3">
                      <RowActions
                        product={product}
                        busy={busy}
                        onActivate={() => {
                          setActionError(null);
                          activateMutation.mutate(product.id);
                        }}
                        onRequestDeactivate={() => {
                          setActionError(null);
                          setPendingDeactivate(product);
                        }}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="space-y-3 md:hidden">
            {products.map((product) => (
              <li
                key={product.id}
                className="rounded-md border border-border bg-card p-3"
              >
                <div className="flex gap-3">
                  <ProductThumb product={product} />
                  <div className="min-w-0 flex-1 space-y-2">
                    <p className="font-medium">{product.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {product.categoryName} · {product.variantCount} خيار ·{" "}
                      {product.imageCount} صورة
                    </p>
                    <ProductStatusBadge isActive={product.isActive} />
                    <RowActions
                      product={product}
                      busy={busy}
                      onActivate={() => {
                        setActionError(null);
                        activateMutation.mutate(product.id);
                      }}
                      onRequestDeactivate={() => {
                        setActionError(null);
                        setPendingDeactivate(product);
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
        title="إيقاف تفعيل المنتج؟"
        description={
          pendingDeactivate
            ? `سيتم إخفاء «${pendingDeactivate.name}» عن الكتالوج العام. هذا ليس حذفاً نهائياً.`
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

function ProductThumb({ product }: { product: AdminProductListItemDto }) {
  return (
    <div className="relative size-12 shrink-0 overflow-hidden rounded-md border border-border bg-muted">
      {product.primaryImageUrl ? (
        <Image
          src={product.primaryImageUrl}
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

function RowActions({
  product,
  busy,
  onActivate,
  onRequestDeactivate,
}: {
  product: AdminProductListItemDto;
  busy: boolean;
  onActivate: () => void;
  onRequestDeactivate: () => void;
}) {
  return (
    <div className="flex flex-wrap gap-2">
      <Link
        href={`/admin/products/${product.id}`}
        className="rounded-md border border-border px-2.5 py-1.5 text-xs font-medium hover:bg-muted"
      >
        إدارة
      </Link>
      {product.isActive ? (
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
    return getProductErrorMessage(error.code, error.status);
  }
  return getProductErrorMessage(undefined);
}
