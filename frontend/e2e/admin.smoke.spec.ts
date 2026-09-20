import { test, expect } from "@playwright/test";
import { e2eEnv, hasAdminCreds, localStackReady } from "./helpers";

async function loginAdmin(page: import("@playwright/test").Page) {
  await page.goto("/login");
  const user = page.locator("#emailOrUserName");
  const pass = page.locator('input[type="password"]').first();
  await user.fill(e2eEnv.adminEmail);
  await pass.fill(e2eEnv.adminPassword);
  await page.getByRole("button", { name: "تسجيل الدخول" }).click();
  await page.waitForTimeout(1500);
}

test.describe("admin module smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("critical admin routes load without crash", async ({ page }) => {
    await loginAdmin(page);
    const routes = [
      "/admin",
      "/admin/categories",
      "/admin/products",
      "/admin/inventory",
      "/admin/shipping",
      "/admin/orders",
      "/admin/settings",
      "/admin/access",
    ];
    for (const route of routes) {
      await page.goto(route);
      await expect(page.locator("body")).toBeVisible();
      // Permission gate may deny some modules; page must still render shell/denied UX
      await expect(page.locator("body")).not.toContainText("Application error");
    }
  });
});
