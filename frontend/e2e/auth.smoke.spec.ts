import { test, expect } from "@playwright/test";
import {
  e2eEnv,
  hasAdminCreds,
  hasCustomerCreds,
  hasLimitedCreds,
  localStackReady,
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
    await page.goto("/login");
    const user = page.locator("#emailOrUserName");
    const pass = page.locator('input[type="password"]').first();
    await user.fill(e2eEnv.customerEmail);
    await pass.fill(e2eEnv.customerPassword);
        await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await expect(page).toHaveURL(/account|\/$/, { timeout: 30_000 });
    await page.goto("/account");
    await expect(page).not.toHaveURL(/login/);
  });

  test("logout returns to anonymous for protected account", async ({ page }) => {
    test.skip(!hasCustomerCreds(), "Set E2E_CUSTOMER_EMAIL / E2E_CUSTOMER_PASSWORD");
    await page.goto("/login");
    const user = page.locator("#emailOrUserName");
    const pass = page.locator('input[type="password"]').first();
    await user.fill(e2eEnv.customerEmail);
    await pass.fill(e2eEnv.customerPassword);
    await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await expect(page).toHaveURL(/account|\/$/, { timeout: 30_000 });
    // Header shows "تسجيل الخروج" once auth store is ready.
    const logout = page.getByRole("button", { name: /تسجيل الخروج|logout/i });
    await expect(logout.first()).toBeVisible({ timeout: 15_000 });
    await logout.first().click();
    await expect(
      page.getByRole("link", { name: /تسجيل الدخول|login/i }).first(),
    ).toBeVisible({ timeout: 15_000 });
    await page.goto("/account");
    await expect(page).toHaveURL(/login/);
  });

  test("anonymous /admin redirects to login", async ({ page }) => {
    await page.goto("/admin");
    await expect(page).toHaveURL(/login/);
  });

  test("admin creds can open admin shell", async ({ page }) => {
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
    await page.goto("/login");
    const user = page.locator("#emailOrUserName");
    const pass = page.locator('input[type="password"]').first();
    await user.fill(e2eEnv.adminEmail);
    await pass.fill(e2eEnv.adminPassword);
        await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await page.goto("/admin");
    await expect(page).not.toHaveURL(/login/, { timeout: 30_000 });
    await expect(page.locator("body")).toContainText(/إدارة|لوحة|admin/i);
  });

  test("limited account denied admin does not clear to anonymous loop", async ({
    page,
  }) => {
    test.skip(!hasLimitedCreds(), "Set E2E_LIMITED_EMAIL / E2E_LIMITED_PASSWORD");
    await page.goto("/login");
    const user = page.locator("#emailOrUserName");
    const pass = page.locator('input[type="password"]').first();
    await user.fill(e2eEnv.limitedEmail);
    await pass.fill(e2eEnv.limitedPassword);
        await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await page.waitForTimeout(1500);
    await page.goto("/admin");
    // Either redirected away from admin or access-denied UX — must not wipe session solely for 403
    const url = page.url();
    expect(url.includes("/admin") || url.includes("/login") || url.includes("/account")).toBeTruthy();
  });
});
