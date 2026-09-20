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
  ROLE_LEVEL_HELP,
  adminIamKeys,
  canCreateIamRoles,
  formatRoleLevel,
  getIamErrorMessage,
  listIamRoles,
} from "@/features/admin-iam";
import { ApiError } from "@/lib/api";
import { useAuthStore } from "@/lib/auth";
import { CreateRoleDialog } from "./create-role-dialog";

export function IamRolesListView() {
  return (
    <AdminPermissionGate anyOf={[IamPermission.roles.read]}>
      <IamRolesListContent />
    </AdminPermissionGate>
  );
}

function IamRolesListContent() {
  const permissions = useAuthStore((s) => s.permissions);
  const canCreate = canCreateIamRoles(permissions);
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  const listQuery = useQuery({
    queryKey: adminIamKeys.roleList(appliedSearch || undefined),
    queryFn: ({ signal }) =>
      listIamRoles(appliedSearch || undefined, signal),
    ...adminOperationalQueryDefaults,
  });

  const sorted = useMemo(() => {
    const items = listQuery.data ?? [];
    return [...items].sort((a, b) => a.roleLevel - b.roleLevel);
  }, [listQuery.data]);

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل الأدوار…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل الأدوار"
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

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="الأدوار"
        description="الأدوار مرتبة حسب المستوى الإداري. الترخيص يعتمد على رموز الصلاحيات وليس أسماء الأدوار."
        actions={
          <div className="flex flex-wrap gap-2">
            {canCreate ? (
              <button
                type="button"
                onClick={() => setCreateOpen(true)}
                className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
              >
                دور جديد
              </button>
            ) : null}
            <Link
              href="/admin/access"
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            >
              رجوع
            </Link>
          </div>
        }
      />

      <p className="text-xs leading-5 text-muted-foreground">{ROLE_LEVEL_HELP}</p>

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
          placeholder="بحث باسم الدور…"
          className="min-w-[12rem] flex-1 rounded-md border border-border bg-background px-3 py-2 text-sm"
          aria-label="بحث عن دور"
        />
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          بحث
        </button>
      </form>

      {sorted.length === 0 ? (
        <AdminEmptyState
          title="لا توجد أدوار"
          description="لا نتائج مطابقة أو لا أدوار ضمن نطاق إدارتك."
        />
      ) : (
        <>
          <div className="hidden overflow-x-auto rounded-md border border-border md:block">
            <table className="w-full min-w-[28rem] text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">الدور</th>
                  <th className="px-3 py-2.5 font-medium">المستوى</th>
                  <th className="px-3 py-2.5 font-medium"> </th>
                </tr>
              </thead>
              <tbody>
                {sorted.map((r) => (
                  <tr
                    key={r.id}
                    className="border-b border-border last:border-b-0"
                  >
                    <td className="px-3 py-3 font-medium">{r.name}</td>
                    <td className="px-3 py-3">
                      {formatRoleLevel(r.roleLevel)}
                    </td>
                    <td className="px-3 py-3">
                      <Link
                        href={`/admin/access/roles/${r.id}`}
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
            {sorted.map((r) => (
              <li
                key={r.id}
                className="space-y-2 rounded-md border border-border p-4"
              >
                <p className="font-medium">{r.name}</p>
                <p className="text-xs text-muted-foreground">
                  {formatRoleLevel(r.roleLevel)}
                </p>
                <Link
                  href={`/admin/access/roles/${r.id}`}
                  className="inline-flex rounded-md border border-border px-3 py-1.5 text-sm hover:bg-muted"
                >
                  عرض
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}

      {canCreate ? (
        <CreateRoleDialog
          open={createOpen}
          referenceRoles={sorted}
          onClose={() => setCreateOpen(false)}
        />
      ) : null}
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getIamErrorMessage(error.code, error.status);
  }
  return getIamErrorMessage(undefined);
}
