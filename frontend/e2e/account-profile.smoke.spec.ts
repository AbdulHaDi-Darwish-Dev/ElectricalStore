import { test, expect } from "@playwright/test";
import {
  e2eEnv,
  hasCustomerCreds,
  localStackReady,
  loginAs,
  logout,
} from "./helpers";

test.describe("customer profile", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
  });

  test("register Arabic fullName → login → profile → edit → change password", async ({
    page,
  }) => {
    const stamp = Date.now().toString(36);
    const email = `e2e.profile.${stamp}@electricalstore.local`;
    const password = "E2e-Profile-Pass-1!";
    const fullName = "عبدالهادي درويش";
    const updatedName = "عبدالهادي م. درويش";
    const nextPassword = "E2e-Profile-Pass-2!";

    await page.goto("/register");
    await page.locator("#fullName").fill(fullName);
    await page.locator("#email").fill(email);
    await page.locator("#password").fill(password);
    await page.locator("#confirmPassword").fill(password);
    await page.getByRole("button", { name: "إنشاء حساب" }).click();

    await expect(page).toHaveURL(/\/login/, { timeout: 30_000 });

    await loginAs(page, email, password);
    await page.goto("/account");
    await expect(page.getByText(fullName).first()).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByText(email).first()).toBeVisible();

    await page.locator("#fullName").fill(updatedName);
    await page.getByRole("button", { name: "حفظ الاسم" }).click();
    await expect(page.getByText("تم تحديث الاسم بنجاح.")).toBeVisible({
      timeout: 15_000,
    });
    await page.reload();
    await expect(page.getByText(updatedName).first()).toBeVisible({
      timeout: 20_000,
    });

    await page.locator("#currentPassword").fill(password);
    await page.locator("#newPassword").fill(nextPassword);
    await page.locator("#confirmNewPassword").fill(nextPassword);
    await page.getByRole("button", { name: "تغيير كلمة المرور" }).click();

    // Either stays authenticated with success, or re-auth required → login.
    await page.waitForTimeout(1500);
    if (page.url().includes("/login")) {
      await loginAs(page, email, nextPassword);
      await page.goto("/account");
      await expect(page.getByText(updatedName).first()).toBeVisible({
        timeout: 20_000,
      });
    } else {
      await expect(
        page.getByText(/تم تغيير كلمة المرور/),
      ).toBeVisible({ timeout: 15_000 });
      await logout(page);
      await loginAs(page, email, nextPassword);
    }
  });

  test("fixture customer sees profile on account", async ({ page }) => {
    test.skip(!hasCustomerCreds(), "Set E2E_CUSTOMER_EMAIL / E2E_CUSTOMER_PASSWORD");
    await loginAs(page, e2eEnv.customerEmail, e2eEnv.customerPassword);
    await page.goto("/account");
    await expect(page.getByRole("heading", { name: "حسابي" })).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByText(e2eEnv.customerEmail).first()).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByRole("link", { name: "عرض طلباتي" })).toBeVisible();
  });
});
