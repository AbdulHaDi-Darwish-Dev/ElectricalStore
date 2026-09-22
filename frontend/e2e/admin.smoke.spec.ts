import { test, expect } from "@playwright/test";
import {
  e2eEnv,
  hasAdminCreds,
  localStackReady,
  loginAs,
} from "./helpers";

test.describe("admin module smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("critical admin routes load without crash", async ({ page }) => {
    await loginAs(page, e2eEnv.adminEmail, e2eEnv.adminPassword);
    await page.goto("/admin");
    await expect(page).not.toHaveURL(/login/, { timeout: 30_000 });
    await expect(page.getByRole("heading", { name: /لوحة التحكم/ })).toBeVisible({
      timeout: 20_000,
    });

    const routes = [
      "/admin",
      "/admin/categories",
      "/admin/products",
      "/admin/inventory",
      "/admin/shipping",
      "/admin/orders",
      "/admin/settings",
      "/admin/access",
      "/admin/access/users",
      "/admin/access/roles",
      "/admin/access/permissions",
      "/admin/access/audit",
    ];
    for (const route of routes) {
      await page.goto(route);
      await expect(page).not.toHaveURL(/login/);
      await expect(page.locator("body")).toBeVisible();
      await expect(page.locator("body")).not.toContainText("Application error");
    }
  });
});
