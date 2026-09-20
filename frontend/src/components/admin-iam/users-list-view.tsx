"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import {
  AdminEmptyState,
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
} from "@/components/admin";
import { IamPermission, adminOperationalQueryDefaults } from "@/features/admin";
import {
  adminIamKeys,
  formatRoleLevel,
  getIamErrorMessage,
  listIamUsers,
} from "@/features/admin-iam";
import { ApiError } from "@/lib/api";

export function IamUsersListView() {
  return (
    <AdminPermissionGate anyOf={[IamPermission.users.read]}>
      <IamUsersListContent />
    </AdminPermissionGate>
  );
}

function IamUsersListContent() {
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const params = useMemo(
    () => ({
      page,
      pageSize,
      search: appliedSearch || undefined,
    }),
    [page, pageSize, appliedSearch],
  );

  const listQuery = useQuery({
    queryKey: adminIamKeys.userList(params),
    queryFn: ({ signal }) => listIamUsers(params, signal),
    ...adminOperationalQueryDefaults,
  });

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل المستخدمين…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل المستخدمين"
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

  const data = listQuery.data!;
  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="المستخدمون"
        description="يظهر فقط المستخدمون ضمن نطاق إدارتك حسب مستوى الدور. إدارة الوصول لا تستخدم أسماء الأدوار للترخيص."
        actions={
          <Link
            href="/admin/access"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع
          </Link>
        }
      />

      <form
        className="flex flex-wrap gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setPage(1);
          setAppliedSearch(search.trim());
        }}
      >
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="بحث بالاسم أو البريد…"
          className="min-w-[12rem] flex-1 rounded-md border border-border bg-background px-3 py-2 text-sm"
          aria-label="بحث عن مستخدم"
        />
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          بحث
        </button>
      </form>

      {data.items.length === 0 ? (
        <AdminEmptyState
          title="لا يوجد مستخدمون"
          description="لا نتائج مطابقة أو لا مستخدمين ضمن نطاق إدارتك."
        />
      ) : (
        <>
          <div className="hidden overflow-x-auto rounded-md border border-border md:block">
            <table className="w-full min-w-[40rem] text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">المستخدم</th>
                  <th className="px-3 py-2.5 font-medium">البريد</th>
                  <th className="px-3 py-2.5 font-medium">المستوى</th>
                  <th className="px-3 py-2.5 font-medium">الحالة</th>
                  <th className="px-3 py-2.5 font-medium"> </th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((u) => (
                  <tr
                    key={u.id}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3 font-medium">{u.userName}</td>
                    <td className="px-3 py-3" dir="ltr">
                      {u.email}
                    </td>
                    <td className="px-3 py-3">
                      {formatRoleLevel(u.effectiveRoleLevel)}
                    </td>
                    <td className="px-3 py-3 text-muted-foreground">
                      {u.isLocked
                        ? "مقفل"
                        : u.isDisabled
                          ? "معطّل"
                          : "نشط"}
                    </td>
                    <td className="px-3 py-3">
                      <Link
                        href={`/admin/access/users/${u.id}`}
                        className="rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted"
                      >
                        عرض
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="space-y-3 md:hidden">
            {data.items.map((u) => (
              <li
                key={u.id}
                className="space-y-2 rounded-md border border-border p-4"
              >
                <p className="font-medium">{u.userName}</p>
                <p className="text-sm" dir="ltr">
                  {u.email}
                </p>
                <p className="text-xs text-muted-foreground">
                  {formatRoleLevel(u.effectiveRoleLevel)} ·{" "}
                  {u.isLocked ? "مقفل" : u.isDisabled ? "معطّل" : "نشط"}
                </p>
                <Link
                  href={`/admin/access/users/${u.id}`}
                  className="inline-flex rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted"
                >
                  عرض
                </Link>
              </li>
            ))}
          </ul>

          <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
            <p className="text-muted-foreground">
              صفحة {data.page} من {totalPages} · {data.totalCount} مستخدم
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="rounded-md border border-border px-3 py-1.5 hover:bg-muted disabled:opacity-50"
              >
                السابق
              </button>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
                className="rounded-md border border-border px-3 py-1.5 hover:bg-muted disabled:opacity-50"
              >
                التالي
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getIamErrorMessage(error.code, error.status);
  }
  return getIamErrorMessage(undefined);
}
