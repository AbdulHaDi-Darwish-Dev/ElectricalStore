import { test, expect } from "@playwright/test";
import {
  e2eEnv,
  hasAdminCreds,
  hasCustomerCreds,
  hasLimitedCreds,
  isAuthenticatedSession,
  localStackReady,
  loginAs,
  logout,
} from "./helpers";

test.describe("auth smoke", () => {
  test.beforeEach(async () => {
    test.skip(
      !(await localStackReady()),
      "Local stack not reachable — start frontend :3100 and API :5180",
    );
  });

  test("login page is public", async ({ page }) => {
    await page.goto("/login");
    await expect(
      page.getByRole("heading", { name: "تسجيل الدخول" }),
    ).toBeVisible();
  });

  test("login failure keeps user on login and does not set account session", async ({
    page,
  }) => {
    await page.goto("/login");
    const user = page.locator("#emailOrUserName");
    const pass = page.locator('input[type="password"]').first();
    await user.fill("nobody-e2e@example.invalid");
    await pass.fill("definitely-wrong-password-!!!");
    await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await expect(page).toHaveURL(/login/);
    await page.goto("/account");
    await expect(page).toHaveURL(/login/);
  });

  test("login success reaches account when creds provided", async ({ page }) => {
    test.skip(!hasCustomerCreds(), "Set E2E_CUSTOMER_EMAIL / E2E_CUSTOMER_PASSWORD");
    await loginAs(page, e2eEnv.customerEmail, e2eEnv.customerPassword);
    await expect(page).toHaveURL(/account|\/$/, { timeout: 30_000 });
    await page.goto("/account");
    await expect(page).not.toHaveURL(/login/);
  });

  test("logout returns to anonymous for protected account", async ({ page }) => {
    test.skip(!hasCustomerCreds(), "Set E2E_CUSTOMER_EMAIL / E2E_CUSTOMER_PASSWORD");
    await loginAs(page, e2eEnv.customerEmail, e2eEnv.customerPassword);
    await expect(page).toHaveURL(/account|\/$/, { timeout: 30_000 });
    await logout(page);
    await page.goto("/account");
    await expect(page).toHaveURL(/login/);
  });

  test("anonymous /admin redirects to login", async ({ page }) => {
    await page.goto("/admin");
    await expect(page).toHaveURL(/login/);
  });

  test("admin creds can open admin shell", async ({ page }) => {
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
    await loginAs(page, e2eEnv.adminEmail, e2eEnv.adminPassword);
    await page.goto("/admin");
    await expect(page).not.toHaveURL(/login/, { timeout: 30_000 });
    await expect(page.locator("body")).toContainText(/إدارة|لوحة|admin/i);
  });

  test("limited account sees access denied on restricted module but stays signed in", async ({
    page,
  }) => {
    test.skip(!hasLimitedCreds(), "Set E2E_LIMITED_EMAIL / E2E_LIMITED_PASSWORD");
    await loginAs(page, e2eEnv.limitedEmail, e2eEnv.limitedPassword);
    await expect(page).toHaveURL(/account|\/$/, { timeout: 30_000 });

    await page.goto("/admin/categories");
    await expect(page.getByText("رفض الوصول")).toBeVisible({ timeout: 15_000 });

    expect(await isAuthenticatedSession(page)).toBeTruthy();
  });
});
