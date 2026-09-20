/** PagedResult from Permixa. */
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type IamUserDto = {
  id: string;
  userName: string;
  email: string;
  emailConfirmed: boolean;
  isDisabled: boolean;
  isLocked: boolean;
  lockoutEndUtc: string | null;
  effectiveRoleLevel: number;
  twoFactorEnabled: boolean;
  pendingEmail: string | null;
};

export type IamRoleDto = {
  id: string;
  name: string;
  roleLevel: number;
};

/** Permission from role/catalog (Permixa PermissionDto). name == code. */
export type IamPermissionDto = {
  id: string;
  name: string;
  description: string | null;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type PermissionCatalogItem = {
  id: string;
  code: string;
  description: string | null;
};

export type PermissionCatalogGroup = {
  group: string;
  permissions: PermissionCatalogItem[];
};

export type PermissionCatalogResponse = {
  groups: PermissionCatalogGroup[];
};

export type OverrideEffect = "Allow" | "Deny";

export type EffectivePermissionSource =
  | "UserDeny"
  | "UserAllow"
  | "Role"
  | "DefaultDeny";

export type EffectivePermissionItem = {
  code: string;
  effective: boolean;
  source: EffectivePermissionSource | string;
};

export type UserPermissionOverrideListItem = {
  permissionId: string;
  permissionName: string;
  /** May serialize as string or enum int from Permixa. */
  effect: OverrideEffect | number | string;
};

export type IamUserDetailDto = {
  user: IamUserDto;
  roles: IamRoleDto[];
  overrides: UserPermissionOverrideListItem[];
  permissions: EffectivePermissionItem[];
};

export type IamRoleDetailDto = {
  role: IamRoleDto;
  permissions: IamPermissionDto[];
};

export type RolePlacement = "Above" | "Below" | "SameLevel";

export type CreateIamRoleRequest = {
  name: string;
  referenceRoleId: string;
  placement: RolePlacement;
};

export type UpdateIamRoleRequest = {
  name?: string | null;
  referenceRoleId?: string | null;
  placement?: RolePlacement | null;
};

export type SetUserRolesRequest = {
  roleIds: string[];
};

export type SetRolePermissionsRequest = {
  permissionIds: string[];
};

export type SetOverrideRequest = {
  effect: OverrideEffect;
};

export type IamAuditLogDto = {
  id: string;
  occurredAtUtc: string;
  eventType: string;
  outcome: number | string;
  actorUserId: string | null;
  targetUserId: string | null;
  targetRoleId: string | null;
  targetPermissionId: string | null;
  correlationId: string | null;
  metadata: string | null;
};

export type IamUserListParams = {
  page?: number;
  pageSize?: number;
  search?: string;
  isDisabled?: boolean;
  isLocked?: boolean;
};

export type IamAuditListParams = {
  page?: number;
  pageSize?: number;
  actorUserId?: string;
  targetUserId?: string;
  eventType?: string;
};
