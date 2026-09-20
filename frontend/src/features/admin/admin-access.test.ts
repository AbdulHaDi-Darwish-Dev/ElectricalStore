import { describe, expect, it } from "vitest";
import {
  ADMIN_SHELL_PERMISSION_CODES,
  APP_PERMISSION_CODES,
  AppPermission,
  IAM_PERMISSION_CODES,
  IamPermission,
  canAccessAdminShell,
  canAccessCategories,
  canAccessInventory,
  canAccessOrders,
  canAccessProducts,
  getVisibleAdminNavigation,
  getAdminDashboardModules,
} from "@/features/admin";
import { hasAnyPermission, isSafeReturnTo } from "@/lib/auth";

describe("admin permission catalog", () => {
  it("uses exact backend application permission codes", () => {
    expect(AppPermission.categories.manage).toBe("Categories.Manage");
    expect(AppPermission.products.manage).toBe("Products.Manage");
    expect(AppPermission.inventory.read).toBe("Inventory.Read");
    expect(AppPermission.inventory.adjust).toBe("Inventory.Adjust");
    expect(AppPermission.shipping.manage).toBe("Shipping.Manage");
    expect(AppPermission.orders.read).toBe("Orders.Read");
    expect(AppPermission.orders.manage).toBe("Orders.Manage");
    expect(AppPermission.settings.manage).toBe("Settings.Manage");
    expect(APP_PERMISSION_CODES).toHaveLength(8);
  });

  it("includes audited IAM permission codes (no invented Admin.Access)", () => {
    expect(IamPermission.users.read).toBe("Iam.Users.Read");
    expect(IamPermission.roles.read).toBe("Iam.Roles.Read");
    expect(IamPermission.rolePermissions.manage).toBe("Iam.RolePermissions.Manage");
    expect(IamPermission.audit.read).toBe("Iam.Audit.Read");
    expect(ADMIN_SHELL_PERMISSION_CODES).not.toContain("Admin.Access");
    expect(ADMIN_SHELL_PERMISSION_CODES).toEqual(
      expect.arrayContaining([...APP_PERMISSION_CODES, ...IAM_PERMISSION_CODES]),
    );
  });

  it("does not authorize by role name", () => {
    expect(canAccessAdminShell(["Admin"])).toBe(false);
    expect(canAccessAdminShell(["Owner"])).toBe(false);
    expect(canAccessAdminShell(["role:Admin"])).toBe(false);
  });
});

describe("admin shell access", () => {
  it("denies empty and customer-only permission sets", () => {
    expect(canAccessAdminShell([])).toBe(false);
    expect(canAccessAdminShell(null)).toBe(false);
    expect(canAccessAdminShell(["Some.Other"])).toBe(false);
  });

  it("allows shell when any one admin permission is present", () => {
    expect(canAccessAdminShell([AppPermission.categories.manage])).toBe(true);
    expect(canAccessAdminShell([IamPermission.users.read])).toBe(true);
    expect(canAccessAdminShell([AppPermission.inventory.read])).toBe(true);
  });
});

describe("admin navigation visibility", () => {
  it("always includes dashboard for an admin-capable user", () => {
    const nav = getVisibleAdminNavigation([AppPermission.categories.manage]);
    expect(nav.some((s) => s.items.some((i) => i.id === "dashboard"))).toBe(
      true,
    );
  });

  it("shows Categories only with Categories.Manage", () => {
    const withCat = getVisibleAdminNavigation([AppPermission.categories.manage]);
    expect(withCat.flatMap((s) => s.items).map((i) => i.id)).toContain(
      "categories",
    );
    expect(withCat.flatMap((s) => s.items).map((i) => i.id)).not.toContain(
      "products",
    );

    const without = getVisibleAdminNavigation([AppPermission.products.manage]);
    expect(without.flatMap((s) => s.items).map((i) => i.id)).not.toContain(
      "categories",
    );
  });

  it("shows Products only with Products.Manage", () => {
    expect(canAccessProducts([AppPermission.products.manage])).toBe(true);
    expect(canAccessProducts([AppPermission.categories.manage])).toBe(false);
  });

  it("shows Inventory with Read or Adjust", () => {
    expect(canAccessInventory([AppPermission.inventory.read])).toBe(true);
    expect(canAccessInventory([AppPermission.inventory.adjust])).toBe(true);
    expect(canAccessInventory([AppPermission.orders.read])).toBe(false);
  });

  it("shows Orders with Read or Manage", () => {
    expect(canAccessOrders([AppPermission.orders.read])).toBe(true);
    expect(canAccessOrders([AppPermission.orders.manage])).toBe(true);
    expect(canAccessOrders([AppPermission.shipping.manage])).toBe(false);
  });

  it("omits unauthorized modules from dashboard cards", () => {
    const cards = getAdminDashboardModules([AppPermission.shipping.manage]);
    expect(cards.map((c) => c.href)).toEqual(["/admin/shipping"]);
  });
});

describe("admin permission gate helper semantics", () => {
  it("allows when any required code matches", () => {
    expect(
      hasAnyPermission(
        [AppPermission.inventory.read],
        [AppPermission.inventory.read, AppPermission.inventory.adjust],
      ),
    ).toBe(true);
  });

  it("denies when none match", () => {
    expect(
      hasAnyPermission(
        [AppPermission.categories.manage],
        [AppPermission.inventory.read, AppPermission.inventory.adjust],
      ),
    ).toBe(false);
  });
});

describe("admin returnTo safety", () => {
  it("accepts /admin as a safe relative returnTo", () => {
    expect(isSafeReturnTo("/admin")).toBe(true);
    expect(isSafeReturnTo("/admin/categories")).toBe(true);
  });
});

describe("categories access alias", () => {
  it("requires Categories.Manage", () => {
    expect(canAccessCategories([AppPermission.categories.manage])).toBe(true);
    expect(canAccessCategories([])).toBe(false);
  });
});
