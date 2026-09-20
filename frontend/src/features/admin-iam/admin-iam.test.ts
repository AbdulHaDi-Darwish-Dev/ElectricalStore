import { describe, expect, it, vi, beforeEach } from "vitest";
import { IamPermission } from "@/features/admin";
import {
  adminIamKeys,
  canManageOverrides,
  canManageRolePermissions,
  canManageUserRoles,
  canReadAudit,
  canReadIamRoles,
  canReadIamUsers,
  canReadPermissionCatalog,
  createIamRole,
  createRoleFormSchema,
  deleteIamRole,
  formatOverrideEffectLabel,
  formatPermissionSource,
  formatPlacement,
  getIamErrorMessage,
  getIamRole,
  getIamUser,
  getVisibleIamModules,
  listIamAudit,
  listIamPermissionCatalog,
  listIamRoles,
  listIamUsers,
  removeIamUserPermissionOverride,
  setIamRolePermissions,
  setIamUserPermissionOverride,
  setIamUserRoles,
  updateIamRole,
} from "@/features/admin-iam";

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    authenticatedFetch: vi.fn(),
  };
});

import { authenticatedFetch } from "@/lib/auth";

const mockedFetch = vi.mocked(authenticatedFetch);

describe("admin IAM permission gates", () => {
  it("uses exact IAM codes — never role names", () => {
    expect(IamPermission.users.read).toBe("Iam.Users.Read");
    expect(IamPermission.userRoles.manage).toBe("Iam.UserRoles.Manage");
    expect(IamPermission.userPermissionOverrides.manage).toBe(
      "Iam.UserPermissionOverrides.Manage",
    );
    expect(IamPermission.roles.read).toBe("Iam.Roles.Read");
    expect(IamPermission.rolePermissions.manage).toBe(
      "Iam.RolePermissions.Manage",
    );
    expect(IamPermission.permissions.read).toBe("Iam.Permissions.Read");
    expect(IamPermission.audit.read).toBe("Iam.Audit.Read");
  });

  it("gates helpers by permission codes", () => {
    expect(canReadIamUsers(["Iam.Users.Read"])).toBe(true);
    expect(canReadIamUsers(["Admin"])).toBe(false);
    expect(canManageUserRoles(["Iam.UserRoles.Manage"])).toBe(true);
    expect(canManageOverrides(["Iam.UserPermissionOverrides.Manage"])).toBe(
      true,
    );
    expect(canReadIamRoles(["Iam.Roles.Read"])).toBe(true);
    expect(canManageRolePermissions(["Iam.RolePermissions.Manage"])).toBe(
      true,
    );
    expect(canReadPermissionCatalog(["Iam.Permissions.Read"])).toBe(true);
    expect(canReadAudit(["Iam.Audit.Read"])).toBe(true);
  });

  it("shows landing modules only for matching Read permissions", () => {
    const modules = getVisibleIamModules([
      "Iam.Users.Read",
      "Iam.Audit.Read",
      "Iam.Roles.Create",
      "Iam.UserRoles.Manage",
    ]);
    // Mutation-only codes must not unlock Users/Roles screens.
    expect(modules.map((m) => m.id)).toEqual(["users", "audit"]);
  });

  it("does not treat Roles mutation perms as Roles.Read", () => {
    expect(
      getVisibleIamModules([
        "Iam.Roles.Create",
        "Iam.Roles.Update",
        "Iam.Roles.Delete",
        "Iam.RolePermissions.Manage",
      ]).map((m) => m.id),
    ).toEqual([]);
    expect(canReadIamRoles(["Iam.Roles.Create"])).toBe(false);
    expect(canReadIamRoles(["Iam.Roles.Read"])).toBe(true);
  });
});

describe("admin IAM query keys", () => {
  it("uses stable nested keys", () => {
    expect(adminIamKeys.all()).toEqual(["admin", "iam"]);
    expect(adminIamKeys.userList({ page: 2, search: "a" })).toEqual([
      "admin",
      "iam",
      "users",
      "list",
      2,
      20,
      "a",
      null,
      null,
    ]);
    expect(adminIamKeys.userDetail("u1")).toEqual([
      "admin",
      "iam",
      "users",
      "detail",
      "u1",
    ]);
    expect(adminIamKeys.roleDetail("r1")).toEqual([
      "admin",
      "iam",
      "roles",
      "detail",
      "r1",
    ]);
    expect(adminIamKeys.permissionCatalog()).toEqual([
      "admin",
      "iam",
      "permissions",
      "catalog",
      null,
    ]);
    expect(adminIamKeys.auditList({ page: 1, eventType: "RoleCreated" })).toEqual([
      "admin",
      "iam",
      "audit",
      "list",
      1,
      20,
      null,
      null,
      "RoleCreated",
    ]);
  });
});

describe("admin IAM API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("lists users with paging", async () => {
    mockedFetch.mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    await listIamUsers({ page: 1, pageSize: 20, search: "x" });
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/users?page=1&pageSize=20&search=x",
      { method: "GET", signal: undefined, cache: "no-store" },
    );
  });

  it("gets user detail", async () => {
    mockedFetch.mockResolvedValueOnce({
      user: { id: "u1" },
      roles: [],
      overrides: [],
      permissions: [],
    });
    await getIamUser("u1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/access/users/u1", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("replaces user roles via PUT", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    const body = { roleIds: ["r1", "r2"] };
    await setIamUserRoles("u1", body);
    expect(mockedFetch).toHaveBeenCalledWith("/admin/access/users/u1/roles", {
      method: "PUT",
      body,
    });
  });

  it("sets and removes overrides", async () => {
    mockedFetch.mockResolvedValueOnce({
      permissionId: "p1",
      permissionName: "Orders.Read",
      effect: "Deny",
    });
    await setIamUserPermissionOverride("u1", "p1", "Deny");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/users/u1/permissions/p1",
      { method: "PUT", body: { effect: "Deny" } },
    );

    mockedFetch.mockResolvedValueOnce(undefined);
    await removeIamUserPermissionOverride("u1", "p1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/users/u1/permissions/p1",
      { method: "DELETE" },
    );
  });

  it("lists/gets roles and shapes create with placement", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    await listIamRoles("mgr");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/roles?search=mgr",
      { method: "GET", signal: undefined, cache: "no-store" },
    );

    mockedFetch.mockResolvedValueOnce({ role: { id: "r1" }, permissions: [] });
    await getIamRole("r1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/access/roles/r1", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });

    const createBody = {
      name: "Staff",
      referenceRoleId: "550e8400-e29b-41d4-a716-446655440000",
      placement: "Below" as const,
    };
    mockedFetch.mockResolvedValueOnce({ id: "r2", name: "Staff", roleLevel: 40 });
    await createIamRole(createBody);
    expect(mockedFetch).toHaveBeenCalledWith("/admin/access/roles", {
      method: "POST",
      body: createBody,
    });
  });

  it("updates/deletes roles and replaces role permissions", async () => {
    mockedFetch.mockResolvedValueOnce({ id: "r1", name: "X", roleLevel: 20 });
    await updateIamRole("r1", { name: "X" });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/access/roles/r1", {
      method: "PUT",
      body: { name: "X" },
    });

    mockedFetch.mockResolvedValueOnce(undefined);
    await deleteIamRole("r1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/access/roles/r1", {
      method: "DELETE",
    });

    const perms = { permissionIds: ["p1"] };
    mockedFetch.mockResolvedValueOnce({});
    await setIamRolePermissions("r1", perms);
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/roles/r1/permissions",
      { method: "PUT", body: perms },
    );
  });

  it("lists permission catalog and paged audit", async () => {
    mockedFetch.mockResolvedValueOnce({ groups: [] });
    await listIamPermissionCatalog("Orders");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/permissions?search=Orders",
      { method: "GET", signal: undefined, cache: "no-store" },
    );

    mockedFetch.mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    await listIamAudit({ page: 1, eventType: "RoleCreated" });
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/access/audit?page=1&pageSize=20&eventType=RoleCreated",
      { method: "GET", signal: undefined, cache: "no-store" },
    );
  });
});

describe("admin IAM hierarchy / override helpers", () => {
  it("validates create role placement without numeric RoleLevel", () => {
    const ok = createRoleFormSchema.safeParse({
      name: "Ops",
      referenceRoleId: "550e8400-e29b-41d4-a716-446655440000",
      placement: "Above",
    });
    expect(ok.success).toBe(true);
    expect(formatPlacement("Above")).toContain("أعلى");
    expect(
      createRoleFormSchema.safeParse({
        name: "Ops",
        referenceRoleId: "not-uuid",
        placement: "Below",
      }).success,
    ).toBe(false);
  });

  it("labels override effects and precedence sources", () => {
    expect(formatOverrideEffectLabel("Deny")).toBe("رفض صريح");
    expect(formatOverrideEffectLabel("Allow")).toBe("سماح صريح");
    expect(formatOverrideEffectLabel(2)).toBe("رفض صريح");
    expect(formatPermissionSource("UserDeny")).toContain("رفض");
    expect(formatPermissionSource("Role")).toContain("موروث");
    expect(formatPermissionSource("DefaultDeny")).toContain("افتراضي");
  });
});

describe("admin IAM error mapping", () => {
  it("maps hierarchy / protected / self codes", () => {
    expect(getIamErrorMessage("Authorization.HierarchyViolation")).toContain(
      "مستوى",
    );
    expect(getIamErrorMessage("Authorization.OwnerProtected")).toContain(
      "محمي",
    );
    expect(getIamErrorMessage("Authorization.CannotManageSelf")).toContain(
      "حسابك",
    );
    expect(getIamErrorMessage("Authorization.RoleHasUsers")).toContain(
      "مستخدمين",
    );
    expect(getIamErrorMessage(undefined, 403)).toContain("صلاحية");
    expect(getIamErrorMessage(undefined, 401)).toContain("الجلسة");
  });
});
