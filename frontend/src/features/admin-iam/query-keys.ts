import { adminQueryKey } from "@/features/admin";
import type { IamAuditListParams, IamUserListParams } from "./types";

export const adminIamDomain = "iam" as const;

export const adminIamKeys = {
  all: () => adminQueryKey(adminIamDomain),
  users: () => adminQueryKey(adminIamDomain, "users"),
  userList: (params: IamUserListParams = {}) =>
    adminQueryKey(
      adminIamDomain,
      "users",
      "list",
      params.page ?? 1,
      params.pageSize ?? 20,
      params.search ?? null,
      params.isDisabled ?? null,
      params.isLocked ?? null,
    ),
  userDetail: (id: string) =>
    adminQueryKey(adminIamDomain, "users", "detail", id),
  roles: () => adminQueryKey(adminIamDomain, "roles"),
  roleList: (search?: string) =>
    adminQueryKey(adminIamDomain, "roles", "list", search ?? null),
  roleDetail: (id: string) =>
    adminQueryKey(adminIamDomain, "roles", "detail", id),
  permissions: () => adminQueryKey(adminIamDomain, "permissions"),
  permissionCatalog: (search?: string) =>
    adminQueryKey(adminIamDomain, "permissions", "catalog", search ?? null),
  audit: () => adminQueryKey(adminIamDomain, "audit"),
  auditList: (params: IamAuditListParams = {}) =>
    adminQueryKey(
      adminIamDomain,
      "audit",
      "list",
      params.page ?? 1,
      params.pageSize ?? 20,
      params.actorUserId ?? null,
      params.targetUserId ?? null,
      params.eventType ?? null,
    ),
} as const;
