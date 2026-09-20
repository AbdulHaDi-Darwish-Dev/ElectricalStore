import { IamPermission } from "@/features/admin";
import { hasAnyPermission, hasPermission } from "@/lib/auth";
import type {
  EffectivePermissionSource,
  OverrideEffect,
  RolePlacement,
} from "./types";

export function formatRoleLevel(level: number): string {
  return `المستوى ${level}`;
}

export const ROLE_LEVEL_HELP =
  "كلما كان رقم المستوى أصغر كانت صلاحية الدور أعلى في التسلسل الإداري.";

export const OVERRIDE_PRECEDENCE_HELP =
  "أولوية الصلاحية: رفض المستخدم > سماح المستخدم > صلاحية الدور > الرفض الافتراضي.";

export function formatOverrideEffect(
  effect: OverrideEffect | number | string,
): OverrideEffect | null {
  if (effect === "Allow" || effect === 1) return "Allow";
  if (effect === "Deny" || effect === 2) return "Deny";
  if (typeof effect === "string") {
    const t = effect.trim().toLowerCase();
    if (t === "allow") return "Allow";
    if (t === "deny") return "Deny";
  }
  return null;
}

export function formatOverrideEffectLabel(
  effect: OverrideEffect | number | string,
): string {
  const n = formatOverrideEffect(effect);
  if (n === "Allow") return "سماح صريح";
  if (n === "Deny") return "رفض صريح";
  return String(effect);
}

export function formatPermissionSource(source: string): string {
  switch (source as EffectivePermissionSource) {
    case "UserDeny":
      return "رفض صريح للمستخدم";
    case "UserAllow":
      return "سماح صريح للمستخدم";
    case "Role":
      return "موروث من الدور";
    case "DefaultDeny":
      return "غير ممنوح (افتراضي)";
    default:
      return source;
  }
}

export function formatPlacement(placement: RolePlacement): string {
  switch (placement) {
    case "Above":
      return "أعلى من المرجع (سلطة أعلى / رقم أصغر)";
    case "Below":
      return "أدنى من المرجع (سلطة أقل / رقم أكبر)";
    case "SameLevel":
      return "نفس مستوى المرجع";
  }
}

export function formatAuditOutcome(outcome: number | string): string {
  if (outcome === 0 || outcome === "Success") return "نجاح";
  if (outcome === 1 || outcome === "Failure") return "فشل";
  return String(outcome);
}

export type IamModuleId =
  | "users"
  | "roles"
  | "permissions"
  | "audit";

export type IamModuleCard = {
  id: IamModuleId;
  href: string;
  title: string;
  description: string;
  anyOf: readonly string[];
};

export const iamModules: IamModuleCard[] = [
  {
    id: "users",
    href: "/admin/access/users",
    title: "المستخدمون",
    description: "عرض المستخدمين والأدوار واستثناءات الصلاحيات.",
    // Screen requires Users.Read — mutation perms do not imply read.
    anyOf: [IamPermission.users.read],
  },
  {
    id: "roles",
    href: "/admin/access/roles",
    title: "الأدوار",
    description: "إدارة الأدوار ومستوياتها وصلاحياتها.",
    // Screen requires Roles.Read — Create/Update/Delete/RolePermissions do not imply read.
    anyOf: [IamPermission.roles.read],
  },
  {
    id: "permissions",
    href: "/admin/access/permissions",
    title: "كتالوج الصلاحيات",
    description: "عرض رموز الصلاحيات المسجّلة في النظام.",
    anyOf: [IamPermission.permissions.read],
  },
  {
    id: "audit",
    href: "/admin/access/audit",
    title: "سجل التدقيق",
    description: "أحداث إدارة الهوية والصلاحيات.",
    anyOf: [IamPermission.audit.read],
  },
];

export function getVisibleIamModules(
  permissions: readonly string[] | null | undefined,
): IamModuleCard[] {
  return iamModules.filter((m) => hasAnyPermission(permissions, m.anyOf));
}

export function canReadIamUsers(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.users.read);
}

export function canManageUserRoles(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.userRoles.manage);
}

export function canManageOverrides(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(
    permissions,
    IamPermission.userPermissionOverrides.manage,
  );
}

export function canReadIamRoles(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.roles.read);
}

export function canCreateIamRoles(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.roles.create);
}

export function canUpdateIamRoles(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.roles.update);
}

export function canDeleteIamRoles(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.roles.delete);
}

export function canManageRolePermissions(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.rolePermissions.manage);
}

export function canReadPermissionCatalog(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.permissions.read);
}

export function canReadAudit(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasPermission(permissions, IamPermission.audit.read);
}
