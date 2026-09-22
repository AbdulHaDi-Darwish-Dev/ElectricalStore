import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  addE2EProductToCart,
  catalogHasE2EProduct,
  e2eEnv,
  fetchAccessTokenViaLogin,
  getOrderingMinimum,
  hasAdminCreds,
  localStackReady,
  loginAs,
  putOrderingMinimum,
  selectE2EShippingZone,
} from "./helpers";

/** Deterministic E2E minimum above a single E2E Product line (price 25). */
const E2E_MINIMUM = 9999;

test.describe("ordering settings smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("update MinimumMerchandiseSubtotal, verify checkout, restore", async ({
    page,
    request,
  }) => {
    test.skip(
      !(await catalogHasE2EProduct(page)),
      `Catalog fixture "${E2E_FIXTURE.productName}" missing — enable LocalDevFixtures`,
    );

    const token = await fetchAccessTokenViaLogin(
      request,
      e2eEnv.adminEmail,
      e2eEnv.adminPassword,
    );
    expect(token).toBeTruthy();

    const original = await getOrderingMinimum(request, token!);

    try {
      await putOrderingMinimum(request, token!, E2E_MINIMUM);

      await loginAs(page, e2eEnv.adminEmail, e2eEnv.adminPassword);
      await page.goto("/admin/settings");
      const input = page.locator("#minimumMerchandiseSubtotal");
      await expect(input).toBeVisible({ timeout: 20_000 });
      await expect(input).toHaveValue(String(E2E_MINIMUM), { timeout: 15_000 });

      // Fresh guest cart — preview must follow backend minimum.
      await page.context().clearCookies();
      await page.goto("/");
      expect(await addE2EProductToCart(page)).toBeTruthy();
      await page.goto("/checkout");
      await selectE2EShippingZone(page);

      await expect(
        page.getByRole("status").filter({ hasText: /الحد الأدنى لمجموع البضاعة/ }),
      ).toBeVisible({ timeout: 30_000 });
      await expect(page.getByRole("button", { name: "تأكيد الطلب" })).toBeDisabled();
    } finally {
      await putOrderingMinimum(request, token!, original);
      const restored = await getOrderingMinimum(request, token!);
      expect(restored).toBe(original);
    }
  });
});
