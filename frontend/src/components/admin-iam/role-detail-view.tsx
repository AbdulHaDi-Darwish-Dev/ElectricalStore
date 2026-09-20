"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
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
  ROLE_LEVEL_HELP,
  adminIamKeys,
  canDeleteIamRoles,
  canManageRolePermissions,
  canUpdateIamRoles,
  deleteIamRole,
  formatRoleLevel,
  getIamErrorMessage,
  getIamRole,
  listIamPermissionCatalog,
  listIamRoles,
  refreshCurrentUserPermissions,
  renameRoleFormSchema,
  setIamRolePermissions,
  updateIamRole,
  type RenameRoleFormValues,
  type RolePlacement,
} from "@/features/admin-iam";
import { ApiError } from "@/lib/api";
import { useAuthStore } from "@/lib/auth";

type Props = { roleId: string };

const placements: RolePlacement[] = ["Above", "Below", "SameLevel"];

export function IamRoleDetailView({ roleId }: Props) {
  return (
    <AdminPermissionGate anyOf={[IamPermission.roles.read]}>
      <IamRoleDetailContent key={roleId} roleId={roleId} />
    </AdminPermissionGate>
  );
}

function IamRoleDetailContent({ roleId }: Props) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const permissions = useAuthStore((s) => s.permissions);
  const canUpdate = canUpdateIamRoles(permissions);
  const canDelete = canDeleteIamRoles(permissions);
  const canPerms = canManageRolePermissions(permissions);

  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [selectedPermissionIds, setSelectedPermissionIds] = useState<
    string[] | null
  >(null);
  const [referenceRoleId, setReferenceRoleId] = useState("");
  const [placement, setPlacement] = useState<RolePlacement>("SameLevel");

  const detailQuery = useQuery({
    queryKey: adminIamKeys.roleDetail(roleId),
    queryFn: ({ signal }) => getIamRole(roleId, signal),
    ...adminOperationalQueryDefaults,
  });

  const rolesQuery = useQuery({
    queryKey: adminIamKeys.roleList(),
    queryFn: ({ signal }) => listIamRoles(undefined, signal),
    enabled: canUpdate,
    ...adminOperationalQueryDefaults,
  });

  const catalogQuery = useQuery({
    queryKey: adminIamKeys.permissionCatalog(),
    queryFn: ({ signal }) => listIamPermissionCatalog(undefined, signal),
    enabled: canPerms,
    ...adminOperationalQueryDefaults,
  });

  const renameForm = useForm<RenameRoleFormValues>({
    resolver: zodResolver(renameRoleFormSchema),
    defaultValues: { name: "" },
  });

  useEffect(() => {
    if (detailQuery.data) {
      renameForm.reset({ name: detailQuery.data.role.name });
    }
  }, [detailQuery.data, renameForm]);

  const permissionIds = useMemo(() => {
    if (selectedPermissionIds) return selectedPermissionIds;
    return detailQuery.data?.permissions.map((p) => p.id) ?? [];
  }, [selectedPermissionIds, detailQuery.data]);

  const otherRoles = useMemo(
    () => (rolesQuery.data ?? []).filter((r) => r.id !== roleId),
    [rolesQuery.data, roleId],
  );

  async function invalidateRole() {
    await queryClient.invalidateQueries({ queryKey: adminIamKeys.roles() });
    await queryClient.invalidateQueries({
      queryKey: adminIamKeys.roleDetail(roleId),
    });
    await queryClient.invalidateQueries({ queryKey: adminIamKeys.users() });
    try {
      await refreshCurrentUserPermissions();
    } catch {
      /* ignore */
    }
  }

  const rename = useMutation({
    mutationFn: (values: RenameRoleFormValues) =>
      updateIamRole(roleId, { name: values.name }),
    onSuccess: async () => {
      setError(null);
      setFeedback("تم تحديث اسم الدور.");
      await invalidateRole();
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
    },
  });

  const reposition = useMutation({
    mutationFn: () =>
      updateIamRole(roleId, {
        referenceRoleId,
        placement,
      }),
    onSuccess: async () => {
      setError(null);
      setFeedback("تم تحديث موضع الدور في التسلسل.");
      await invalidateRole();
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
    },
  });

  const savePerms = useMutation({
    mutationFn: () =>
      setIamRolePermissions(roleId, { permissionIds }),
    onSuccess: async () => {
      setError(null);
      setFeedback("تم تحديث صلاحيات الدور.");
      setSelectedPermissionIds(null);
      await invalidateRole();
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
    },
  });

  const remove = useMutation({
    mutationFn: () => deleteIamRole(roleId),
    onSuccess: async () => {
      setConfirmDelete(false);
      await queryClient.invalidateQueries({ queryKey: adminIamKeys.roles() });
      try {
        await refreshCurrentUserPermissions();
      } catch {
        /* ignore */
      }
      router.push("/admin/access/roles");
    },
    onError: (e) => {
      setFeedback(null);
      setError(mapError(e));
      setConfirmDelete(false);
    },
  });

  if (detailQuery.isLoading) {
    return <AdminLoadingState label="جاري تحميل الدور…" />;
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <AdminErrorState
        title="تعذر تحميل الدور"
        message={mapError(detailQuery.error)}
        actions={
          <Link
            href="/admin/access/roles"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            العودة
          </Link>
        }
      />
    );
  }

  const { role, permissions: rolePerms } = detailQuery.data;
  const catalogGroups = catalogQuery.data?.groups ?? [];

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title={role.name}
        description={formatRoleLevel(role.roleLevel)}
        actions={
          <Link
            href="/admin/access/roles"
            className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
          >
            رجوع
          </Link>
        }
      />

      <p className="text-xs leading-5 text-muted-foreground">{ROLE_LEVEL_HELP}</p>

      {feedback ? <AdminFeedback tone="success">{feedback}</AdminFeedback> : null}
      {error ? <AdminFeedback tone="error">{error}</AdminFeedback> : null}

      <AdminSection title="الدور">
        <dl className="grid gap-2 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">الاسم</dt>
            <dd className="font-medium">{role.name}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">المستوى</dt>
            <dd>{formatRoleLevel(role.roleLevel)}</dd>
          </div>
        </dl>

        {canUpdate ? (
          <form
            className="mt-4 space-y-3"
            onSubmit={renameForm.handleSubmit((v) => rename.mutate(v))}
          >
            <div>
              <label htmlFor="rename-role" className="mb-1 block text-sm">
                إعادة تسمية
              </label>
              <input
                id="rename-role"
                className="w-full max-w-md rounded-md border border-border bg-background px-3 py-2 text-sm"
                {...renameForm.register("name")}
              />
              {renameForm.formState.errors.name ? (
                <p className="mt-1 text-xs text-destructive">
                  {renameForm.formState.errors.name.message}
                </p>
              ) : null}
            </div>
            <button
              type="submit"
              disabled={rename.isPending}
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
            >
              {rename.isPending ? "جاري الحفظ…" : "حفظ الاسم"}
            </button>
          </form>
        ) : null}
      </AdminSection>

      {canUpdate ? (
        <AdminSection title="التسلسل الإداري">
          <p className="mb-3 text-xs text-muted-foreground">
            لا تُرسل رقم المستوى يدوياً. اختر دوراً مرجعياً وموضعاً نسبياً؛
            الخادم يحسب المستوى.
          </p>
          <div className="space-y-3">
            <div>
              <label htmlFor="repo-ref" className="mb-1 block text-sm">
                الدور المرجعي
              </label>
              <select
                id="repo-ref"
                className="w-full max-w-md rounded-md border border-border bg-background px-3 py-2 text-sm"
                value={referenceRoleId}
                onChange={(e) => setReferenceRoleId(e.target.value)}
              >
                <option value="">— اختر —</option>
                {otherRoles.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name} (مستوى {r.roleLevel})
                  </option>
                ))}
              </select>
            </div>
            <fieldset>
              <legend className="mb-1 text-sm">الموضع</legend>
              <div className="space-y-2">
                {placements.map((p) => (
                  <label key={p} className="flex items-center gap-2 text-sm">
                    <input
                      type="radio"
                      name="placement"
                      checked={placement === p}
                      onChange={() => setPlacement(p)}
                    />
                    {p === "Above"
                      ? "أعلى (سلطة أعلى)"
                      : p === "Below"
                        ? "أدنى (سلطة أقل)"
                        : "نفس المستوى"}
                  </label>
                ))}
              </div>
            </fieldset>
            <button
              type="button"
              disabled={reposition.isPending || !referenceRoleId}
              onClick={() => reposition.mutate()}
              className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
            >
              {reposition.isPending ? "جاري التحديث…" : "تحديث الموضع"}
            </button>
          </div>
        </AdminSection>
      ) : null}

      <AdminSection title="الصلاحيات المعيّنة">
        <ul className="mb-3 max-h-40 space-y-1 overflow-y-auto text-sm">
          {rolePerms.map((p) => (
            <li key={p.id} className="font-mono text-xs" dir="ltr">
              {p.name}
            </li>
          ))}
          {rolePerms.length === 0 ? (
            <li className="text-muted-foreground">لا صلاحيات معيّنة.</li>
          ) : null}
        </ul>

        {canPerms ? (
          <div className="space-y-3">
            <p className="text-xs text-muted-foreground">
              استبدال مجموعة صلاحيات الدور بالكامل. الرموز تُرسل كما هي.
            </p>
            <div className="max-h-96 space-y-4 overflow-y-auto rounded-md border border-border p-3">
              {catalogGroups.map((g) => (
                <div key={g.group}>
                  <p className="mb-2 text-sm font-medium">{g.group}</p>
                  <div className="space-y-1">
                    {g.permissions.map((p) => {
                      const checked = permissionIds.includes(p.id);
                      return (
                        <label
                          key={p.id}
                          className="flex items-start gap-2 text-sm"
                        >
                          <input
                            type="checkbox"
                            className="mt-1"
                            checked={checked}
                            onChange={() => {
                              setSelectedPermissionIds((prev) => {
                                const base =
                                  prev ?? rolePerms.map((x) => x.id);
                                return checked
                                  ? base.filter((id) => id !== p.id)
                                  : [...base, p.id];
                              });
                            }}
                          />
                          <span>
                            <span className="font-mono text-xs" dir="ltr">
                              {p.code}
                            </span>
                            {p.description ? (
                              <span className="mt-0.5 block text-xs text-muted-foreground">
                                {p.description}
                              </span>
                            ) : null}
                          </span>
                        </label>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
            <button
              type="button"
              disabled={savePerms.isPending || selectedPermissionIds === null}
              onClick={() => savePerms.mutate()}
              className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
            >
              {savePerms.isPending ? "جاري الحفظ…" : "حفظ صلاحيات الدور"}
            </button>
          </div>
        ) : null}
      </AdminSection>

      {canDelete ? (
        <AdminSection title="حذف الدور">
          <p className="mb-3 text-xs text-muted-foreground">
            الحذف نهائي. الأدوار المحمية أو المرتبطة بمستخدمين أو التي خارج
            نطاقك ستُرفض من الخادم.
          </p>
          <button
            type="button"
            onClick={() => setConfirmDelete(true)}
            className="rounded-md border border-destructive/40 px-3 py-2 text-sm text-destructive hover:bg-destructive/5"
          >
            حذف الدور
          </button>
        </AdminSection>
      ) : null}

      <AdminConfirmDialog
        open={confirmDelete}
        title="حذف الدور؟"
        description={`سيتم حذف الدور «${role.name}» نهائياً إن سمح الخادم بذلك.`}
        confirmLabel="حذف"
        tone="danger"
        busy={remove.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => remove.mutate()}
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
