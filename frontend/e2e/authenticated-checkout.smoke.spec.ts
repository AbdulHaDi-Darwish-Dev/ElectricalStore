import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  addE2EProductToCart,
  catalogHasE2EProduct,
  e2eEnv,
  fillAuthenticatedCheckoutForm,
  hasCustomerCreds,
  localStackReady,
  loginAs,
  logout,
} from "./helpers";

test.describe("authenticated checkout", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(
      !hasCustomerCreds(),
      "Set E2E_CUSTOMER_EMAIL / E2E_CUSTOMER_PASSWORD",
    );
  });

  test("customer login → buy → account order list + detail", async ({
    page,
  }) => {
    test.skip(
      !(await catalogHasE2EProduct(page)),
      `Catalog fixture "${E2E_FIXTURE.productName}" missing — enable LocalDevFixtures`,
    );

    await loginAs(page, e2eEnv.customerEmail, e2eEnv.customerPassword);
    expect(await addE2EProductToCart(page)).toBeTruthy();

    await page.goto("/checkout");
    await fillAuthenticatedCheckoutForm(page);

    const submit = page.getByRole("button", { name: "تأكيد الطلب" });
    await expect(submit).toBeEnabled({ timeout: 30_000 });
    await submit.click();

    await expect(page).toHaveURL(/\/account\/orders\/[^/]+$/, {
      timeout: 45_000,
    });
    const orderId = page.url().match(/\/account\/orders\/([^/]+)$/)?.[1];
    expect(orderId).toBeTruthy();

    const detailHeading = page.getByRole("heading", { level: 1 });
    await expect(detailHeading).toBeVisible({ timeout: 20_000 });
    const orderNumberText = (await detailHeading.innerText()).trim();
    expect(orderNumberText.length).toBeGreaterThan(3);

    await page.goto("/account/orders");
    await expect(page.getByRole("heading", { name: "طلباتي" })).toBeVisible({
      timeout: 20_000,
    });
    const orderLink = page.locator(`a[href="/account/orders/${orderId}"]`);
    await expect(orderLink.first()).toBeVisible({ timeout: 20_000 });
    await orderLink.first().click();
    await expect(page).toHaveURL(new RegExp(`/account/orders/${orderId}$`));
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      orderNumberText.replace(/^طلب\s*/, ""),
      { timeout: 15_000 },
    );

    await logout(page);
  });
});
