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
  formatAuditOutcome,
  getIamErrorMessage,
  listIamAudit,
} from "@/features/admin-iam";
import { formatOrderDateTime } from "@/features/admin-orders";
import { ApiError } from "@/lib/api";

export function IamAuditListView() {
  return (
    <AdminPermissionGate anyOf={[IamPermission.audit.read]}>
      <IamAuditListContent />
    </AdminPermissionGate>
  );
}

function IamAuditListContent() {
  const [page, setPage] = useState(1);
  const [eventType, setEventType] = useState("");
  const [appliedEventType, setAppliedEventType] = useState("");
  const pageSize = 20;

  const params = useMemo(
    () => ({
      page,
      pageSize,
      eventType: appliedEventType || undefined,
    }),
    [page, pageSize, appliedEventType],
  );

  const listQuery = useQuery({
    queryKey: adminIamKeys.auditList(params),
    queryFn: ({ signal }) => listIamAudit(params, signal),
    ...adminOperationalQueryDefaults,
  });

  if (listQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل سجل التدقيق…" />;
  }

  if (listQuery.isError) {
    return (
      <AdminErrorState
        title="تعذر تحميل سجل التدقيق"
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
        title="سجل التدقيق"
        description="أحداث إدارة الهوية والصلاحيات كما يسجّلها الخادم. قراءة فقط."
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
          setAppliedEventType(eventType.trim());
        }}
      >
        <input
          type="search"
          value={eventType}
          onChange={(e) => setEventType(e.target.value)}
          placeholder="نوع الحدث…"
          className="min-w-[12rem] flex-1 rounded-md border border-border bg-background px-3 py-2 text-sm"
          aria-label="تصفية بنوع الحدث"
        />
        <button
          type="submit"
          className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
        >
          تصفية
        </button>
      </form>

      {data.items.length === 0 ? (
        <AdminEmptyState
          title="لا أحداث"
          description="لا نتائج مطابقة في سجل التدقيق."
        />
      ) : (
        <>
          <div className="hidden overflow-x-auto rounded-md border border-border md:block">
            <table className="w-full min-w-[48rem] text-sm">
              <thead className="border-b border-border bg-muted/40 text-start">
                <tr>
                  <th className="px-3 py-2.5 font-medium">الوقت</th>
                  <th className="px-3 py-2.5 font-medium">الحدث</th>
                  <th className="px-3 py-2.5 font-medium">النتيجة</th>
                  <th className="px-3 py-2.5 font-medium">الفاعل</th>
                  <th className="px-3 py-2.5 font-medium">الهدف</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((row) => (
                  <tr
                    key={row.id}
                    className="border-b border-border last:border-b-0 align-top"
                  >
                    <td className="px-3 py-3 whitespace-nowrap">
                      {formatOrderDateTime(row.occurredAtUtc)}
                    </td>
                    <td className="px-3 py-3 font-mono text-xs" dir="ltr">
                      {row.eventType}
                    </td>
                    <td className="px-3 py-3">
                      {formatAuditOutcome(row.outcome)}
                    </td>
                    <td className="px-3 py-3 font-mono text-xs" dir="ltr">
                      {row.actorUserId ?? "—"}
                    </td>
                    <td className="px-3 py-3 font-mono text-xs" dir="ltr">
                      {row.targetUserId ??
                        row.targetRoleId ??
                        row.targetPermissionId ??
                        "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="space-y-3 md:hidden">
            {data.items.map((row) => (
              <li
                key={row.id}
                className="space-y-1 rounded-md border border-border p-4 text-sm"
              >
                <p className="font-medium">
                  {formatOrderDateTime(row.occurredAtUtc)}
                </p>
                <p className="font-mono text-xs" dir="ltr">
                  {row.eventType}
                </p>
                <p className="text-xs text-muted-foreground">
                  {formatAuditOutcome(row.outcome)}
                </p>
                <p className="font-mono text-[11px] text-muted-foreground" dir="ltr">
                  actor: {row.actorUserId ?? "—"}
                </p>
              </li>
            ))}
          </ul>

          <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
            <p className="text-muted-foreground">
              صفحة {data.page} من {totalPages} · {data.totalCount} حدث
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

      <p className="text-xs text-muted-foreground">
        حقل metadata لا يُعرض كاملاً هنا لتجنب عرض بيانات حساسة عرضاً.
      </p>
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getIamErrorMessage(error.code, error.status);
  }
  return getIamErrorMessage(undefined);
}
