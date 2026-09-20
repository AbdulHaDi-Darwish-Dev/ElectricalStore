"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
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
  getIamErrorMessage,
  listIamPermissionCatalog,
} from "@/features/admin-iam";
import { ApiError } from "@/lib/api";

export function IamPermissionsCatalogView() {
  return (
    <AdminPermissionGate anyOf={[IamPermission.permissions.read]}>
      <IamPermissionsCatalogContent />
    </AdminPermissionGate>
  );
}

function IamPermissionsCatalogContent() {
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");

  const catalogQuery = useQuery({
    queryKey: adminIamKeys.permissionCatalog(appliedSearch || undefined),
    queryFn: ({ signal }) =>
      listIamPermissionCatalog(appliedSearch || undefined, signal),
    ...adminOperationalQueryDefaults,
  });

  if (catalogQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل كتالوج الصلاحيات…" />;
  }

  if (catalogQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل الصلاحيات"
        message={mapError(catalogQuery.error)}
        actions={
          <button
            type="button"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
            onClick={() => void catalogQuery.refetch()}
          >
            إعادة المحاولة
          </button>
        }
      />
    );
  }

  const groups = catalogQuery.data?.groups ?? [];
  const total = groups.reduce((n, g) => n + g.permissions.length, 0);

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="كتالوج الصلاحيات"
        description="رموز الصلاحيات كما يعرضها الخادم. إنشاء/تحديث الصلاحيات ديناميكياً غير مفعّل في واجهة الإدارة."
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
          setAppliedSearch(search.trim());
        }}
      >
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="بحث بالرمز…"
          className="min-w-[12rem] flex-1 rounded-md border border-border bg-background px-3 py-2 text-sm"
          aria-label="بحث عن صلاحية"
        />
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          بحث
        </button>
      </form>

      {total === 0 ? (
        <AdminEmptyState
          title="لا صلاحيات"
          description="لا نتائج في كتالوج الصلاحيات."
        />
      ) : (
        <div className="space-y-4">
          {groups.map((g) => (
            <section
              key={g.group}
              className="rounded-md border border-border"
              aria-labelledby={`perm-group-${g.group}`}
            >
              <h2
                id={`perm-group-${g.group}`}
                className="border-b border-border bg-muted/40 px-3 py-2 text-sm font-semibold"
              >
                {g.group}
              </h2>
              <ul className="divide-y divide-border">
                {g.permissions.map((p) => (
                  <li key={p.id} className="px-3 py-2.5 text-sm">
                    <p className="font-mono text-xs" dir="ltr">
                      {p.code}
                    </p>
                    {p.description ? (
                      <p className="mt-0.5 text-xs text-muted-foreground">
                        {p.description}
                      </p>
                    ) : null}
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
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
