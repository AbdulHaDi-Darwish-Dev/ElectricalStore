import { test, expect, type APIRequestContext } from "@playwright/test";
import {
  e2eEnv,
  fetchAccessTokenViaLogin,
  hasAdminCreds,
  localStackReady,
} from "./helpers";

const E2E_ROLE_PREFIX = "E2E-IAM-Role";
const PERMISSION_CODE = "Categories.Manage";

test.describe("IAM deep E2E (API against local stack)", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("role create, grant, assign, allow/deny overrides, cleanup", async ({
    request,
  }) => {
    const token = await fetchAccessTokenViaLogin(
      request,
      e2eEnv.adminEmail,
      e2eEnv.adminPassword,
    );
    expect(token).toBeTruthy();
    const auth = {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
      "Content-Type": "application/json",
    };

    const stamp = Date.now().toString(36).slice(-6);
    const roleName = `${E2E_ROLE_PREFIX}-${stamp}`;

    // Target: limited E2E user (never Owner).
    const usersRes = await request.get(
      `${e2eEnv.apiUrl}/admin/access/users?page=1&pageSize=50&search=${encodeURIComponent(e2eEnv.limitedEmail)}`,
      { headers: auth },
    );
    expect(usersRes.ok()).toBeTruthy();
    const usersBody = (await usersRes.json()) as {
      items: { id: string; email: string }[];
    };
    const limited = usersBody.items.find(
      (u) => u.email.toLowerCase() === e2eEnv.limitedEmail.toLowerCase(),
    );
    expect(limited, "E2E limited user must exist").toBeTruthy();
    const userId = limited!.id;

    // Capture existing roles for restore.
    const detailBefore = await request.get(
      `${e2eEnv.apiUrl}/admin/access/users/${userId}`,
      { headers: auth },
    );
    expect(detailBefore.ok()).toBeTruthy();
    const beforeBody = (await detailBefore.json()) as {
      roles: { id: string }[];
    };
    const originalRoleIds = beforeBody.roles.map((r) => r.id);

    const limitedRoleId = await findRoleId(request, auth, "E2E-Limited");
    expect(limitedRoleId).toBeTruthy();

    // Place new role relative to E2E-Limited (actor E2E-Admin is higher authority).
    // Referencing the actor's own role (SameLevel) triggers HierarchyViolation.
    const createRole = await request.post(`${e2eEnv.apiUrl}/admin/access/roles`, {
      headers: auth,
      data: {
        name: roleName,
        referenceRoleId: limitedRoleId,
        placement: "Above",
      },
    });
    expect(
      createRole.status(),
      await createRole.text(),
    ).toBe(201);
    const created = (await createRole.json()) as {
      id: string;
      roleLevel: number;
    };
    const roleId = created.id;
    expect(created.roleLevel).toBeGreaterThan(0);

    let permissionId: string | null = null;
    try {
      permissionId = await findPermissionId(request, auth, PERMISSION_CODE);
      expect(permissionId).toBeTruthy();

      const grant = await request.put(
        `${e2eEnv.apiUrl}/admin/access/roles/${roleId}/permissions`,
        {
          headers: auth,
          data: { permissionIds: [permissionId] },
        },
      );
      expect(grant.ok()).toBeTruthy();

      const assign = await request.put(
        `${e2eEnv.apiUrl}/admin/access/users/${userId}/roles`,
        {
          headers: auth,
          data: { roleIds: [...originalRoleIds, roleId] },
        },
      );
      expect(assign.ok(), await assign.text()).toBeTruthy();

      const effectiveAfterRole = await getEffectivePermission(
        request,
        auth,
        userId,
        PERMISSION_CODE,
      );
      expect(effectiveAfterRole?.effective).toBe(true);
      expect(effectiveAfterRole?.source).toMatch(/Role|UserAllow/);

      const allow = await request.put(
        `${e2eEnv.apiUrl}/admin/access/users/${userId}/permissions/${permissionId}`,
        { headers: auth, data: { effect: "Allow" } },
      );
      expect(allow.ok()).toBeTruthy();

      const deny = await request.put(
        `${e2eEnv.apiUrl}/admin/access/users/${userId}/permissions/${permissionId}`,
        { headers: auth, data: { effect: "Deny" } },
      );
      expect(deny.ok()).toBeTruthy();

      const effectiveDeny = await getEffectivePermission(
        request,
        auth,
        userId,
        PERMISSION_CODE,
      );
      expect(effectiveDeny?.effective).toBe(false);
      expect(effectiveDeny?.source).toBe("UserDeny");

      const removeOverride = await request.delete(
        `${e2eEnv.apiUrl}/admin/access/users/${userId}/permissions/${permissionId}`,
        { headers: auth },
      );
      expect([200, 204]).toContain(removeOverride.status());
    } finally {
      // Restore user roles (drop E2E role assignment).
      await request.put(`${e2eEnv.apiUrl}/admin/access/users/${userId}/roles`, {
        headers: auth,
        data: { roleIds: originalRoleIds },
      });

      if (permissionId) {
        await request.delete(
          `${e2eEnv.apiUrl}/admin/access/users/${userId}/permissions/${permissionId}`,
          { headers: auth },
        );
      }

      // Empty role permissions then delete role.
      await request.put(
        `${e2eEnv.apiUrl}/admin/access/roles/${roleId}/permissions`,
        { headers: auth, data: { permissionIds: [] } },
      );
      const del = await request.delete(
        `${e2eEnv.apiUrl}/admin/access/roles/${roleId}`,
        { headers: auth },
      );
      expect([204, 200, 409]).toContain(del.status());
    }
  });
});

async function findRoleId(
  request: APIRequestContext,
  headers: Record<string, string>,
  name: string,
): Promise<string | null> {
  const res = await request.get(`${e2eEnv.apiUrl}/admin/access/roles`, {
    headers,
  });
  if (!res.ok()) return null;
  const roles = (await res.json()) as { id: string; name: string }[];
  return roles.find((r) => r.name === name)?.id ?? null;
}

async function findPermissionId(
  request: APIRequestContext,
  headers: Record<string, string>,
  code: string,
): Promise<string | null> {
  const res = await request.get(
    `${e2eEnv.apiUrl}/admin/access/permissions?search=${encodeURIComponent(code)}`,
    { headers },
  );
  if (!res.ok()) return null;
  const body = (await res.json()) as {
    groups: { permissions: { id: string; name: string; code?: string }[] }[];
  };
  for (const g of body.groups ?? []) {
    for (const p of g.permissions ?? []) {
      if (p.name === code || p.code === code) return p.id;
    }
  }
  return null;
}

async function getEffectivePermission(
  request: APIRequestContext,
  headers: Record<string, string>,
  userId: string,
  code: string,
): Promise<{ effective: boolean; source: string } | null> {
  const res = await request.get(
    `${e2eEnv.apiUrl}/admin/access/users/${userId}/permissions`,
    { headers },
  );
  if (!res.ok()) return null;
  const body = (await res.json()) as {
    permissions: { code: string; effective: boolean; source: string }[];
  };
  const match = body.permissions.find((p) => p.code === code);
  return match
    ? { effective: match.effective, source: match.source }
    : null;
}
