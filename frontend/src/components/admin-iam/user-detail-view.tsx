"use client";

import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import {
  AdminErrorState,
  AdminLoadingState,
  AdminPageHeader,
  AdminPermissionGate,
  AdminSection,
} from "@/components/admin";
import {
  AdminConfirmDialog,
  AdminFeedback,
} from "@/components/admin-categories";
import { IamPermission, adminOperationalQueryDefaults } from "@/features/admin";
import {
  OVERRIDE_PRECEDENCE_HELP,
  ROLE_LEVEL_HELP,
  adminIamKeys,
  canManageOverrides,
  canManageUserRoles,
  formatOverrideEffectLabel,
  formatPermissionSource,
  formatRoleLevel,
  getIamErrorMessage,
  getIamUser,
  listIamPermissionCatalog,
  listIamRoles,
  refreshCurrentUserPermissions,
  removeIamUserPermissionOverride,
  setIamUserPermissionOverride,
  setIamUserRoles,
  type OverrideEffect,
} from "@/features/admin-iam";
import { ApiError } from "@/lib/api";
import { useAuthStore } from "@/lib/auth";

type Props = { userId: string };

export function IamUserDetailView({ userId }: Props) {
  return (
    <AdminPermissionGate anyOf={[IamPermission.users.read]}>
      <IamUserDetailContent userId={userId} />
    </AdminPermissionGate>
  );
}

function IamUserDetailContent({ userId }: Props) {
  const queryClient = useQueryClient();
  const permissions = useAuthStore((s) => s.permissions);
  const currentUserId = useAuthStore((s) => s.userId);
  const canRoles = canManageUserRoles(permissions);
  const canOverrides = canManageOverrides(permissions);

  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[] | null>(null);
  const [pendingDeny, setPendingDeny] = useState<{
    permissionId: string;
    code: string;
  } | null>(null);

  const detailQuery = useQuery({
    queryKey: adminIamKeys.userDetail(userId),
    queryFn: ({ signal }) => getIamUser(userId, signal),
    ...adminOperationalQueryDefaults,
  });

  const rolesQuery = useQuery({
    queryKey: adminIamKeys.roleList(),
    queryFn: ({ signal }) => listIamRoles(undefined, signal),
    enabled: canRoles,
    ...adminOperationalQueryDefaults,
  });

  const catalogQuery = useQuery({
    queryKey: adminIamKeys.permissionCatalog(),
    queryFn: ({ signal }) => listIamPermissionCatalog(undefined, signal),
    enabled: canOverrides,
    ...adminOperationalQueryDefaults,
  });

  const roleIds = useMemo(() => {
    if (selectedRoleIds) return selectedRoleIds;
    return detailQuery.data?.roles.map((r) => r.id) ?? [];
  }, [selectedRoleIds, detailQuery.data]);

  async function invalidateUser() {
    await queryClient.invalidateQueries({ queryKey: adminIamKeys.users() });
    await queryClient.invalidateQueries({
      queryKey: adminIamKeys.userDetail(userId),
    });
    if (currentUserId === userId) {
      try {
        await refreshCurrentUserPermissions();
      } catch {
        /* ignore — session may still be valid */
      }
    }
  }

  const saveRoles = useMutation({
    mutationFn: () => setIamUserRoles(userId, { roleIds }),
    onSuccess: async () => {
      setError(null);
      setFeedback("تم تحديث أدوار المستخدم.");
      setSelectedRoleIds(null);
      await invalidateUser();
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
    },
  });

  const setOverride = useMutation({
    mutationFn: ({
      permissionId,
      effect,
    }: {
      permissionId: string;
      effect: OverrideEffect;
    }) => setIamUserPermissionOverride(userId, permissionId, effect),
    onSuccess: async () => {
      setPendingDeny(null);
      setError(null);
      setFeedback("تم حفظ استثناء الصلاحية.");
      await invalidateUser();
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
    },
  });

  const clearOverride = useMutation({
    mutationFn: (permissionId: string) =>
      removeIamUserPermissionOverride(userId, permissionId),
    onSuccess: async () => {
      setError(null);
      setFeedback("تمت إزالة الاستثناء (العودة للوراثة).");
      await invalidateUser();
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
    },
  });

  if (detailQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل المستخدم…" />;
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <AdminErrorState
        title="تعذر تحميل المستخدم"
        message={mapError(detailQuery.error)}
        actions={
          <Link
            href="/admin/access/users"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            العودة
          </Link>
        }
      />
    );
  }

  const { user, roles, overrides, permissions: effective } = detailQuery.data;
  const overrideByName = new Map(
    overrides.map((o) => [o.permissionName, o] as const),
  );
  const catalogFlat =
    catalogQuery.data?.groups.flatMap((g) => g.permissions) ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title={user.userName}
        description={`${user.email} · ${formatRoleLevel(user.effectiveRoleLevel)}`}
        actions={
          <Link
            href="/admin/access/users"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع
          </Link>
        }
      />

      <p className="text-xs leading-5 text-muted-foreground">{ROLE_LEVEL_HELP}</p>
      <p className="text-xs leading-5 text-muted-foreground">
        {OVERRIDE_PRECEDENCE_HELP}
      </p>

      {feedback ? <AdminFeedback tone="success">{feedback}</AdminFeedback> : null}
      {error ? <AdminFeedback tone="error">{error}</AdminFeedback> : null}

      <AdminSection title="الهوية">
        <dl className="grid gap-2 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">اسم المستخدم</dt>
            <dd className="font-medium">{user.userName}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">البريد</dt>
            <dd className="font-medium" dir="ltr">
              {user.email}
              {user.emailConfirmed ? "" : " (غير مؤكد)"}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">الحالة</dt>
            <dd>
              {user.isLocked
                ? "مقفل"
                : user.isDisabled
                  ? "معطّل"
                  : "نشط"}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">المستوى الفعّال</dt>
            <dd>{formatRoleLevel(user.effectiveRoleLevel)}</dd>
          </div>
        </dl>
        <p className="mt-3 text-xs text-muted-foreground">
          قفل الحساب وتغيير البريد غير متاحين عبر واجهة الإدارة حالياً.
        </p>
      </AdminSection>

      <AdminSection title="الأدوار">
        <ul className="mb-3 space-y-1 text-sm">
          {roles.map((r) => (
            <li key={r.id}>
              {r.name}{" "}
              <span className="text-muted-foreground">
                ({formatRoleLevel(r.roleLevel)})
              </span>
            </li>
          ))}
          {roles.length === 0 ? (
            <li className="text-muted-foreground">لا أدوار معيّنة.</li>
          ) : null}
        </ul>

        {canRoles ? (
          <div className="space-y-3">
            <p className="text-xs text-muted-foreground">
              استبدال مجموعة الأدوار بالكامل. الخادم يفرض قواعد التسلسل.
            </p>
            <div className="max-h-56 space-y-2 overflow-y-auto rounded-md border border-border p-3">
              {(rolesQuery.data ?? []).map((r) => {
                const checked = roleIds.includes(r.id);
                return (
                  <label
                    key={r.id}
                    className="flex items-center gap-2 text-sm"
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => {
                        setSelectedRoleIds((prev) => {
                          const base = prev ?? roles.map((x) => x.id);
                          return checked
                            ? base.filter((id) => id !== r.id)
                            : [...base, r.id];
                        });
                      }}
                    />
                    <span>
                      {r.name}{" "}
                      <span className="text-muted-foreground">
                        ({formatRoleLevel(r.roleLevel)})
                      </span>
                    </span>
                  </label>
                );
              })}
            </div>
            <button
              type="button"
              disabled={saveRoles.isPending || selectedRoleIds === null}
              onClick={() => saveRoles.mutate()}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
            >
              {saveRoles.isPending ? "جاري الحفظ…" : "حفظ الأدوار"}
            </button>
          </div>
        ) : null}
      </AdminSection>

      <AdminSection title="الصلاحيات الفعّالة (من الخادم)">
        <div className="max-h-72 overflow-y-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="sticky top-0 border-b border-border bg-muted/50 text-start">
              <tr>
                <th className="px-3 py-2 font-medium">الرمز</th>
                <th className="px-3 py-2 font-medium">فعّالة؟</th>
                <th className="px-3 py-2 font-medium">المصدر</th>
              </tr>
            </thead>
            <tbody>
              {effective.map((p) => (
                <tr
                  key={p.code}
                  className="border-b border-border last:border-b-0"
                >
                  <td className="px-3 py-2 font-mono text-xs" dir="ltr">
                    {p.code}
                  </td>
                  <td className="px-3 py-2">
                    {p.effective ? "نعم" : "لا"}
                  </td>
                  <td className="px-3 py-2">
                    {formatPermissionSource(p.source)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </AdminSection>

      {canOverrides ? (
        <AdminSection title="استثناءات الصلاحيات">
          <p className="mb-3 text-xs leading-5 text-muted-foreground">
            الرفض الصريح أقوى من السماح الموروث من الدور. لا تخلط بين «رفض
            صريح» و«غير ممنوح».
          </p>
          <div className="max-h-96 space-y-2 overflow-y-auto">
            {catalogFlat.map((p) => {
              const ov = overrideByName.get(p.code);
              const effectLabel = ov
                ? formatOverrideEffectLabel(ov.effect)
                : "لا استثناء (وراثة)";
              return (
                <div
                  key={p.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border px-3 py-2 text-sm"
                >
                  <div>
                    <p className="font-mono text-xs" dir="ltr">
                      {p.code}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {effectLabel}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-1">
                    <button
                      type="button"
                      className="rounded-md border border-border px-2 py-1 text-xs hover:bg-muted"
                      disabled={setOverride.isPending}
                      onClick={() =>
                        setOverride.mutate({
                          permissionId: p.id,
                          effect: "Allow",
                        })
                      }
                    >
                      سماح
                    </button>
                    <button
                      type="button"
                      className="rounded-md border border-destructive/40 px-2 py-1 text-xs text-destructive hover:bg-destructive/5"
                      disabled={setOverride.isPending}
                      onClick={() =>
                        setPendingDeny({ permissionId: p.id, code: p.code })
                      }
                    >
                      رفض
                    </button>
                    {ov ? (
                      <button
                        type="button"
                        className="rounded-md border border-border px-2 py-1 text-xs hover:bg-muted"
                        disabled={clearOverride.isPending}
                        onClick={() => clearOverride.mutate(p.id)}
                      >
                        إزالة
                      </button>
                    ) : null}
                  </div>
                </div>
              );
            })}
          </div>
        </AdminSection>
      ) : null}

      <AdminConfirmDialog
        open={pendingDeny !== null}
        title="تعيين رفض صريح؟"
        description={
          pendingDeny
            ? `الرفض الصريح لـ ${pendingDeny.code} يتجاوز أي منح من الأدوار. هذه أولوية أعلى من السماح.`
            : ""
        }
        confirmLabel="تعيين الرفض"
        tone="danger"
        busy={setOverride.isPending}
        onCancel={() => setPendingDeny(null)}
        onConfirm={() => {
          if (pendingDeny) {
            setOverride.mutate({
              permissionId: pendingDeny.permissionId,
              effect: "Deny",
            });
          }
        }}
      />
    </div>
  );
}

function mapError(error: unknown): string {
  if (error instanceof ApiError) {
    return getIamErrorMessage(error.code, error.status);
  }
  return getIamErrorMessage(undefined);
}
