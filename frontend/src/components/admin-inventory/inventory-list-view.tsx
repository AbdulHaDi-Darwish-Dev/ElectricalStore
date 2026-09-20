"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "next/navigation";
import { useMemo, useState } from "react";
import {
  AdminEmptyState,
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
} from "@/components/admin";
import { AdminFeedback } from "@/components/admin-categories";
import {
  AppPermission,
  adminOperationalQueryDefaults,
} from "@/features/admin";
import {
  adjustAdminInventory,
  adminInventoryKeys,
  availabilityLabel,
  formatStockQuantity,
  getInventoryErrorMessage,
  listAdminInventory,
  toAdjustInventoryRequest,
  type AdjustInventoryFormValues,
  type AdminInventoryItemDto,
  type AdminInventoryListParams,
} from "@/features/admin-inventory";
import { ApiError } from "@/lib/api";
import { hasPermission, useAuthStore } from "@/lib/auth";
import { formatSellingUnit } from "@/lib/format";
import { InventoryAdjustDialog } from "./inventory-adjust-dialog";

type StockFilter = "all" | "in" | "out";

export function InventoryListView() {
  return (
    <AdminPermissionGate
      anyOf={[AppPermission.inventory.read]}
      deniedTitle="غير مصرح بعرض المخزون"
      deniedMessage="عرض المخزون يتطلب صلاحية Inventory.Read. صلاحية التعديل وحدها لا تتيح قائمة المخزون."
    >
      <InventoryListContent />
    </AdminPermissionGate>
  );
}

function InventoryListContent() {
  const queryClient = useQueryClient();
  const searchParams = useSearchParams();
  const productIdFromUrl = searchParams.get("productId")?.trim() || undefined;
  const permissions = useAuthStore((s) => s.permissions);
  const canAdjust = hasPermission(
    permissions,
    AppPermission.inventory.adjust,
  );

  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [stockFilter, setStockFilter] = useState<StockFilter>("all");
  const [adjusting, setAdjusting] = useState<AdminInventoryItemDto | null>(
    null,
  );
  const [adjustError, setAdjustError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const listParams: AdminInventoryListParams = useMemo(() => {
    const params: AdminInventoryListParams = {};
    if (productIdFromUrl) params.productId = productIdFromUrl;
    if (appliedSearch) params.search = appliedSearch;
    if (stockFilter === "in") params.inStock = true;
    if (stockFilter === "out") params.inStock = false;
    return params;
  }, [productIdFromUrl, appliedSearch, stockFilter]);

  const listQuery = useQuery({
    queryKey: adminInventoryKeys.list(listParams),
    queryFn: ({ signal }) => listAdminInventory(listParams, signal),
    ...adminOperationalQueryDefaults,
  });

  const adjustMutation = useMutation({
    mutationFn: ({
      variantId,
      values,
    }: {
      variantId: string;
      values: AdjustInventoryFormValues;
    }) =>
      adjustAdminInventory(
        variantId,
        toAdjustInventoryRequest(values.quantityDelta, values.reason),
      ),
    onSuccess: async () => {
      setAdjustError(null);
      setAdjusting(null);
      setActionError(null);
      setActionSuccess("تم تعديل المخزون الفعلي بنجاح.");
      await queryClient.invalidateQueries({
        queryKey: adminInventoryKeys.all(),
      });
    },
    onError: async (error) => {
      setActionSuccess(null);
      const message = mapError(error);
      setAdjustError(message);
      if (
        error instanceof ApiError &&
        error.code === "Inventory.ConcurrencyConflict"
      ) {
        await queryClient.invalidateQueries({
          queryKey: adminInventoryKeys.all(),
        });
        setAdjusting(null);
        setActionError(message);
      }
    },
  });

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل المخزون…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل المخزون"
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

  const items = listQuery.data ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="المخزون"
        description="مخزون كل خيار منتج (Variant). المحجوز والمتاح للبيع للعرض فقط — التعديل يخص المخزون الفعلي بإضافة أو خصم. عرض المتجر العام قد يتأخر حتى دقيقة؛ الدفع يبقى فورياً مقابل الخادم."
      />

      <p className="text-sm leading-6 text-muted-foreground">
        <span className="font-medium text-foreground">المخزون الفعلي</span>{" "}
        (OnHand) ·{" "}
        <span className="font-medium text-foreground">المحجوز</span> (Reserved)
        ·{" "}
        <span className="font-medium text-foreground">المتاح للبيع</span>{" "}
        (Available = الفعلي − المحجوز)
      </p>

      <form
        className="flex flex-wrap items-end gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setAppliedSearch(search.trim());
        }}
      >
        <div className="min-w-[12rem] flex-1 space-y-1">
          <label htmlFor="inventory-search" className="sr-only">
            بحث في المخزون
          </label>
          <input
            id="inventory-search"
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="بحث بالمنتج أو الخيار أو SKU…"
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
          />
        </div>
        <div className="space-y-1">
          <label htmlFor="inventory-stock" className="sr-only">
            تصفية التوفر
          </label>
          <select
            id="inventory-stock"
            value={stockFilter}
            onChange={(e) => setStockFilter(e.target.value as StockFilter)}
            className="rounded-md border border-border bg-background px-3 py-2 text-sm"
          >
            <option value="all">كل الحالات</option>
            <option value="in">متوفر للبيع</option>
            <option value="out">غير متوفر</option>
          </select>
        </div>
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          بحث
        </button>
      </form>

      {productIdFromUrl ? (
        <AdminFeedback tone="info">
          عرض مخزون منتج محدد. أزل التصفية من الرابط لعرض كل الخيارات.
        </AdminFeedback>
      ) : null}
      {actionSuccess ? (
        <AdminFeedback tone="success">{actionSuccess}</AdminFeedback>
      ) : null}
      {actionError ? (
        <AdminFeedback tone="error">{actionError}</AdminFeedback>
      ) : null}

      {items.length === 0 ? (
        <AdminEmptyState
          title="لا توجد عناصر مخزون"
          description={
            appliedSearch || stockFilter !== "all" || productIdFromUrl
              ? "لا نتائج مطابقة للتصفية الحالية."
              : "لا توجد خيارات منتجات بعد. أنشئ منتجات وخيارات أولاً."
          }
        />
      ) : (
        <>
          <div className="hidden overflow-x-auto rounded-md border border-border md:block">
            <table className="w-full min-w-[52rem] text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">المنتج / الخيار</th>
                  <th className="px-3 py-2.5 font-medium">SKU</th>
                  <th className="px-3 py-2.5 font-medium">الوحدة</th>
                  <th className="px-3 py-2.5 font-medium">الفعلي</th>
                  <th className="px-3 py-2.5 font-medium">المحجوز</th>
                  <th className="px-3 py-2.5 font-medium">المتاح</th>
                  <th className="px-3 py-2.5 font-medium">الحالة</th>
                  {canAdjust ? (
                    <th className="px-3 py-2.5 font-medium">إجراء</th>
                  ) : null}
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr
                    key={item.variantId}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3 align-top">
                      <div className="font-medium">{item.productName}</div>
                      <div className="text-muted-foreground">
                        {item.variantName}
                      </div>
                    </td>
                    <td className="px-3 py-3 align-top font-mono text-xs">
                      {item.sku}
                    </td>
                    <td className="px-3 py-3 align-top">
                      {formatSellingUnit(item.sellingUnit)}
                    </td>
                    <td className="px-3 py-3 align-top tabular-nums">
                      {formatStockQuantity(item.onHand)}
                    </td>
                    <td className="px-3 py-3 align-top tabular-nums">
                      {formatStockQuantity(item.reserved)}
                    </td>
                    <td className="px-3 py-3 align-top tabular-nums font-medium">
                      {formatStockQuantity(item.available)}
                    </td>
                    <td className="px-3 py-3 align-top">
                      <AvailabilityText isInStock={item.isInStock} />
                    </td>
                    {canAdjust ? (
                      <td className="px-3 py-3 align-top">
                        <button
                          type="button"
                          className="rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted"
                          onClick={() => {
                            setAdjustError(null);
                            setAdjusting(item);
                          }}
                        >
                          تعديل
                        </button>
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="space-y-3 md:hidden">
            {items.map((item) => (
              <li
                key={item.variantId}
                className="rounded-md border border-border p-4"
              >
                <div className="space-y-1">
                  <p className="font-medium">{item.productName}</p>
                  <p className="text-sm text-muted-foreground">
                    {item.variantName}
                  </p>
                  <p className="font-mono text-xs text-muted-foreground">
                    {item.sku} · {formatSellingUnit(item.sellingUnit)}
                  </p>
                </div>
                <dl className="mt-3 grid grid-cols-3 gap-2 text-sm">
                  <div>
                    <dt className="text-xs text-muted-foreground">الفعلي</dt>
                    <dd className="tabular-nums font-medium">
                      {formatStockQuantity(item.onHand)}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-xs text-muted-foreground">المحجوز</dt>
                    <dd className="tabular-nums">
                      {formatStockQuantity(item.reserved)}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-xs text-muted-foreground">المتاح</dt>
                    <dd className="tabular-nums font-medium">
                      {formatStockQuantity(item.available)}
                    </dd>
                  </div>
                </dl>
                <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
                  <AvailabilityText isInStock={item.isInStock} />
                  {canAdjust ? (
                    <button
                      type="button"
                      className="rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted"
                      onClick={() => {
                        setAdjustError(null);
                        setAdjusting(item);
                      }}
                    >
                      تعديل
                    </button>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        </>
      )}

      {adjusting && canAdjust ? (
        <InventoryAdjustDialog
          item={adjusting}
          open
          busy={adjustMutation.isPending}
          formError={adjustError}
          onCancel={() => {
            if (!adjustMutation.isPending) {
              setAdjusting(null);
              setAdjustError(null);
            }
          }}
          onSubmit={(values) => {
            setAdjustError(null);
            adjustMutation.mutate({
              variantId: adjusting.variantId,
              values,
            });
          }}
        />
      ) : null}
    </div>
  );
}

function AvailabilityText({ isInStock }: { isInStock: boolean }) {
  return (
    <span
      className={
        isInStock
          ? "text-sm text-foreground"
          : "text-sm font-medium text-destructive"
      }
    >
      {availabilityLabel(isInStock)}
    </span>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getInventoryErrorMessage(error.code, error.status);
  }
  return getInventoryErrorMessage(undefined);
}
