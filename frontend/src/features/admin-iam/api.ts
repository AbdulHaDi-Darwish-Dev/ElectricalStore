import { authenticatedFetch } from "@/lib/auth";
import type {
  CreateIamRoleRequest,
  IamAuditListParams,
  IamAuditLogDto,
  IamRoleDetailDto,
  IamRoleDto,
  IamUserDetailDto,
  IamUserDto,
  IamUserListParams,
  OverrideEffect,
  PagedResult,
  PermissionCatalogResponse,
  SetRolePermissionsRequest,
  SetUserRolesRequest,
  UpdateIamRoleRequest,
  UserPermissionOverrideListItem,
} from "./types";

const BASE = "/admin/access";

function qs(
  params: Record<string, string | number | boolean | undefined | null>,
): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === "") continue;
    sp.set(k, String(v));
  }
  const q = sp.toString();
  return q ? `?${q}` : "";
}

export async function listIamUsers(
  params: IamUserListParams = {},
  signal?: AbortSignal,
): Promise<PagedResult<IamUserDto>> {
  return authenticatedFetch<PagedResult<IamUserDto>>(
    `${BASE}/users${qs({
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      search: params.search,
      isDisabled: params.isDisabled,
      isLocked: params.isLocked,
    })}`,
    { method: "GET", signal, cache: "no-store" },
  );
}

export async function getIamUser(
  id: string,
  signal?: AbortSignal,
): Promise<IamUserDetailDto> {
  return authenticatedFetch<IamUserDetailDto>(`${BASE}/users/${id}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function setIamUserRoles(
  id: string,
  body: SetUserRolesRequest,
): Promise<IamRoleDto[]> {
  return authenticatedFetch<IamRoleDto[]>(`${BASE}/users/${id}/roles`, {
    method: "PUT",
    body,
  });
}

export async function setIamUserPermissionOverride(
  userId: string,
  permissionId: string,
  effect: OverrideEffect,
): Promise<UserPermissionOverrideListItem> {
  return authenticatedFetch(`${BASE}/users/${userId}/permissions/${permissionId}`, {
    method: "PUT",
    body: { effect },
  });
}

export async function removeIamUserPermissionOverride(
  userId: string,
  permissionId: string,
): Promise<void> {
  return authenticatedFetch(
    `${BASE}/users/${userId}/permissions/${permissionId}`,
    { method: "DELETE" },
  );
}

export async function listIamRoles(
  search?: string,
  signal?: AbortSignal,
): Promise<IamRoleDto[]> {
  return authenticatedFetch<IamRoleDto[]>(
    `${BASE}/roles${qs({ search })}`,
    { method: "GET", signal, cache: "no-store" },
  );
}

export async function getIamRole(
  id: string,
  signal?: AbortSignal,
): Promise<IamRoleDetailDto> {
  return authenticatedFetch<IamRoleDetailDto>(`${BASE}/roles/${id}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function createIamRole(
  body: CreateIamRoleRequest,
): Promise<IamRoleDto> {
  return authenticatedFetch<IamRoleDto>(`${BASE}/roles`, {
    method: "POST",
    body,
  });
}

export async function updateIamRole(
  id: string,
  body: UpdateIamRoleRequest,
): Promise<IamRoleDto> {
  return authenticatedFetch<IamRoleDto>(`${BASE}/roles/${id}`, {
    method: "PUT",
    body,
  });
}

export async function deleteIamRole(id: string): Promise<void> {
  return authenticatedFetch(`${BASE}/roles/${id}`, { method: "DELETE" });
}

export async function setIamRolePermissions(
  id: string,
  body: SetRolePermissionsRequest,
): Promise<unknown> {
  return authenticatedFetch(`${BASE}/roles/${id}/permissions`, {
    method: "PUT",
    body,
  });
}

export async function listIamPermissionCatalog(
  search?: string,
  signal?: AbortSignal,
): Promise<PermissionCatalogResponse> {
  return authenticatedFetch<PermissionCatalogResponse>(
    `${BASE}/permissions${qs({ search })}`,
    { method: "GET", signal, cache: "no-store" },
  );
}

export async function listIamAudit(
  params: IamAuditListParams = {},
  signal?: AbortSignal,
): Promise<PagedResult<IamAuditLogDto>> {
  return authenticatedFetch<PagedResult<IamAuditLogDto>>(
    `${BASE}/audit${qs({
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      actorUserId: params.actorUserId,
      targetUserId: params.targetUserId,
      eventType: params.eventType,
    })}`,
    { method: "GET", signal, cache: "no-store" },
  );
}
