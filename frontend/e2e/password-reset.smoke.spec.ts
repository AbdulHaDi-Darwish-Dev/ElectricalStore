import { test, expect } from "@playwright/test";
import {
  clearDevEmailOutbox,
  confirmLatestVerificationEmail,
  fetchDevEmailOutbox,
  localStackReady,
  loginAs,
  logout,
  openLatestPasswordResetEmail,
} from "./helpers";

test.describe("forgot / reset password", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
  });

  test("full recovery: forgot → outbox → reset → old fails → new succeeds", async ({
    page,
  }) => {
    const stamp = Date.now().toString(36);
    const email = `e2e.reset.${stamp}@electricalstore.local`;
    const oldPassword = "E2e-Reset-Old-1!";
    const newPassword = "E2e-Reset-New-2!";
    const fullName = "عبدالهادي درويش";

    await clearDevEmailOutbox().catch(() => undefined);

    await page.goto("/register");
    await page.locator("#fullName").fill(fullName);
    await page.locator("#email").fill(email);
    await page.locator("#password").fill(oldPassword);
    await page.locator("#confirmPassword").fill(oldPassword);
    await page.getByRole("button", { name: "إنشاء حساب" }).click();
    await expect(page).toHaveURL(/\/verify-email\/pending/, { timeout: 30_000 });

    await confirmLatestVerificationEmail(page, email);
    await loginAs(page, email, oldPassword);
    await logout(page);

    await clearDevEmailOutbox().catch(() => undefined);

    await page.goto("/login");
    await page.getByRole("link", { name: "نسيت كلمة المرور؟" }).click();
    await expect(page).toHaveURL(/\/forgot-password/);

    await page.locator("#email").fill(email);
    await page.getByRole("button", { name: "إرسال رابط إعادة التعيين" }).click();
    await expect(page.getByRole("status")).toContainText("حساب مؤهل", {
      timeout: 15_000,
    });

    const resetPath = await openLatestPasswordResetEmail(page, email);
    await expect(page.locator("#newPassword")).toBeVisible({
      timeout: 15_000,
    });
    await page.locator("#newPassword").fill(newPassword);
    await page.locator("#confirmNewPassword").fill(newPassword);
    await page.getByRole("button", { name: "تعيين كلمة المرور" }).click();
    await expect(
      page.getByRole("heading", { name: "تم تغيير كلمة المرور بنجاح" }),
    ).toBeVisible({ timeout: 20_000 });
    await expect(
      page.getByRole("main").getByRole("link", { name: "تسجيل الدخول" }),
    ).toBeVisible();

    await page.goto("/login");
    await page.locator("#emailOrUserName").fill(email);
    await page.locator('input[type="password"]').first().fill(oldPassword);
    await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await expect(page.locator("p[role='alert'], div[role='alert']").first()).toBeVisible({
      timeout: 15_000,
    });
    // Still on login after failed attempt.
    await expect(page).toHaveURL(/\/login/);

    await loginAs(page, email, newPassword);
    await page.goto("/account");
    await expect(page.getByText(fullName).first()).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByText(email).first()).toBeVisible();

    // Reused link must not reset again.
    await page.goto(resetPath);
    await page.locator("#newPassword").fill("E2e-Reset-New-3!");
    await page.locator("#confirmNewPassword").fill("E2e-Reset-New-3!");
    await page.getByRole("button", { name: "تعيين كلمة المرور" }).click();
    await expect(page.getByRole("heading", { name: "تعذر إعادة التعيين" })).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByRole("link", { name: "طلب رابط جديد" })).toBeVisible();
  });

  test("unknown email shows same generic success and no outbox mail", async ({
    page,
  }) => {
    await clearDevEmailOutbox().catch(() => undefined);
    const unknown = `e2e.unknown.${Date.now().toString(36)}@electricalstore.local`;

    await page.goto("/forgot-password");
    await page.locator("#email").fill(unknown);
    await page.getByRole("button", { name: "إرسال رابط إعادة التعيين" }).click();
    await expect(page.getByRole("status")).toContainText("حساب مؤهل", {
      timeout: 15_000,
    });

    const mailbox = await fetchDevEmailOutbox();
    expect(
      mailbox.filter((m) => m.to.toLowerCase() === unknown.toLowerCase()),
    ).toHaveLength(0);
  });

  test("invalid reset link shows safe Arabic error and request-new CTA", async ({
    page,
  }) => {
    await page.goto(
      `/reset-password?challengeId=${crypto.randomUUID()}&token=not-valid`,
    );
    await page.locator("#newPassword").fill("E2e-Reset-Bad-1!");
    await page.locator("#confirmNewPassword").fill("E2e-Reset-Bad-1!");
    await page.getByRole("button", { name: "تعيين كلمة المرور" }).click();
    await expect(page.getByRole("heading", { name: "تعذر إعادة التعيين" })).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.locator("p[role='alert']")).toContainText(/غير صالح|لم يعد صالح/);
    await expect(page.getByRole("link", { name: "طلب رابط جديد" })).toBeVisible();
  });
});
