import { test, expect } from "@playwright/test";
import {
  clearDevEmailOutbox,
  confirmLatestVerificationEmail,
  fetchDevEmailOutbox,
  localStackReady,
  loginAs,
  openLatestChangeEmailConfirm,
} from "./helpers";

test.describe("change email", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
  });

  test("full journey: request → outbox → confirm → re-login with new email", async ({
    page,
  }) => {
    const stamp = Date.now().toString(36);
    const oldEmail = `e2e.ec.old.${stamp}@electricalstore.local`;
    const newEmail = `e2e.ec.new.${stamp}@electricalstore.local`;
    const password = "E2e-EmailChange-1!";
    const fullName = "عبدالهادي درويش";

    await clearDevEmailOutbox().catch(() => undefined);

    await page.goto("/register");
    await page.locator("#fullName").fill(fullName);
    await page.locator("#email").fill(oldEmail);
    await page.locator("#password").fill(password);
    await page.locator("#confirmPassword").fill(password);
    await page.getByRole("button", { name: "إنشاء حساب" }).click();
    await expect(page).toHaveURL(/\/verify-email\/pending/, { timeout: 30_000 });

    await confirmLatestVerificationEmail(page, oldEmail);
    await loginAs(page, oldEmail, password);

    await clearDevEmailOutbox().catch(() => undefined);

    await page.goto("/account");
    await expect(page.getByText(oldEmail).first()).toBeVisible({
      timeout: 20_000,
    });
    await page.getByRole("button", { name: "تغيير البريد الإلكتروني" }).click();
    await page.locator("#newEmail").fill(newEmail);
    await page.locator("#emailChangeCurrentPassword").fill(password);
    await page.getByRole("button", { name: "إرسال رابط التأكيد" }).click();
    await expect(page.getByRole("status")).toContainText(
      "أرسلنا رسالة تأكيد",
      { timeout: 15_000 },
    );

    const mailbox = await fetchDevEmailOutbox();
    const changeMail = [...mailbox]
      .reverse()
      .find(
        (m) =>
          m.to.toLowerCase() === newEmail.toLowerCase() &&
          m.textBody.includes("/change-email/confirm"),
      );
    expect(changeMail).toBeTruthy();
    expect(changeMail!.subject).toContain("تأكيد تغيير البريد");
    if (changeMail!.previewUrl) {
      await page.goto(
        changeMail!.previewUrl.startsWith("http")
          ? changeMail!.previewUrl
          : `${process.env.E2E_API_URL ?? "http://localhost:5180"}${changeMail!.previewUrl}`,
      );
      await expect(page.locator("body")).toContainText("تأكيد تغيير البريد");
    }

    const confirmPath = await openLatestChangeEmailConfirm(page, newEmail);
    await expect(
      page.getByRole("heading", { name: "تم تغيير بريدك الإلكتروني بنجاح" }),
    ).toBeVisible({ timeout: 20_000 });
    await expect(
      page.getByRole("main").getByRole("link", { name: "تسجيل الدخول" }),
    ).toBeVisible();

    // Old email login fails.
    await page.goto("/login");
    await page.locator("#emailOrUserName").fill(oldEmail);
    await page.locator('input[type="password"]').first().fill(password);
    await page.getByRole("button", { name: "تسجيل الدخول" }).click();
    await expect(
      page.locator("p[role='alert'], div[role='alert']").first(),
    ).toBeVisible({ timeout: 15_000 });
    await expect(page).toHaveURL(/\/login/);

    await loginAs(page, newEmail, password);
    await page.goto("/account");
    await expect(page.getByText(fullName).first()).toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByText(newEmail).first()).toBeVisible();

    // Reused confirm link.
    await page.goto(confirmPath);
    await expect(
      page.getByRole("heading", { name: "تعذر تأكيد تغيير البريد" }),
    ).toBeVisible({ timeout: 15_000 });
  });

  test("wrong current password shows Arabic error and sends no mail", async ({
    page,
  }) => {
    const stamp = Date.now().toString(36);
    const email = `e2e.ec.wrongpw.${stamp}@electricalstore.local`;
    const password = "E2e-EmailChange-Wrong-1!";
    const newEmail = `e2e.ec.wrongpw.new.${stamp}@electricalstore.local`;

    await clearDevEmailOutbox().catch(() => undefined);

    await page.goto("/register");
    await page.locator("#fullName").fill("Wrong Password EC");
    await page.locator("#email").fill(email);
    await page.locator("#password").fill(password);
    await page.locator("#confirmPassword").fill(password);
    await page.getByRole("button", { name: "إنشاء حساب" }).click();
    await expect(page).toHaveURL(/\/verify-email\/pending/, { timeout: 30_000 });
    await confirmLatestVerificationEmail(page, email);
    await loginAs(page, email, password);

    await clearDevEmailOutbox().catch(() => undefined);
    await page.goto("/account");
    await page.getByRole("button", { name: "تغيير البريد الإلكتروني" }).click();
    await page.locator("#newEmail").fill(newEmail);
    await page.locator("#emailChangeCurrentPassword").fill("Definitely-Wrong-9!");
    await page.getByRole("button", { name: "إرسال رابط التأكيد" }).click();
    await expect(
      page.locator("p[role='alert'], div[role='alert']").filter({ hasText: "كلمة المرور الحالية" }).first(),
    ).toBeVisible({ timeout: 15_000 });

    const mailbox = await fetchDevEmailOutbox();
    expect(
      mailbox.some(
        (m) =>
          m.to.toLowerCase() === newEmail.toLowerCase() &&
          m.textBody.includes("/change-email/confirm"),
      ),
    ).toBe(false);

    await page.goto("/account");
    await expect(page.getByText(email).first()).toBeVisible({ timeout: 15_000 });
  });

  test("duplicate target email shows conflict and keeps current account", async ({
    page,
  }) => {
    const stamp = Date.now().toString(36);
    const occupied = `e2e.ec.taken.${stamp}@electricalstore.local`;
    const actor = `e2e.ec.actor.${stamp}@electricalstore.local`;
    const password = "E2e-EmailChange-Dup-1!";

    await clearDevEmailOutbox().catch(() => undefined);

    for (const email of [occupied, actor]) {
      await page.goto("/register");
      await page.locator("#fullName").fill(email === occupied ? "Taken" : "Actor");
      await page.locator("#email").fill(email);
      await page.locator("#password").fill(password);
      await page.locator("#confirmPassword").fill(password);
      await page.getByRole("button", { name: "إنشاء حساب" }).click();
      await expect(page).toHaveURL(/\/verify-email\/pending/, { timeout: 30_000 });
      await confirmLatestVerificationEmail(page, email);
      await page.goto("/login");
    }

    await loginAs(page, actor, password);
    await clearDevEmailOutbox().catch(() => undefined);

    await page.goto("/account");
    await page.getByRole("button", { name: "تغيير البريد الإلكتروني" }).click();
    await page.locator("#newEmail").fill(occupied);
    await page.locator("#emailChangeCurrentPassword").fill(password);
    await page.getByRole("button", { name: "إرسال رابط التأكيد" }).click();
    await expect(
      page.locator("p[role='alert'], div[role='alert']").filter({ hasText: "مستخدم بالفعل" }).first(),
    ).toBeVisible({ timeout: 15_000 });

    await page.goto("/account");
    await expect(page.getByText(actor).first()).toBeVisible({ timeout: 15_000 });
  });
});
