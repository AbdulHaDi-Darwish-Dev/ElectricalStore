import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  addE2EProductToCart,
  catalogHasE2EProduct,
  fillGuestCheckoutForm,
  localStackReady,
} from "./helpers";

test.describe("cart and checkout smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
  });

  test("checkout page loads (guest path UI)", async ({ page }) => {
    await page.goto("/checkout");
    await expect(page.locator("body")).toBeVisible();
  });

  test("E2E product: add to cart, quantity persists, guest place order", async ({
    page,
  }) => {
    test.skip(
      !(await catalogHasE2EProduct(page)),
      `Catalog fixture "${E2E_FIXTURE.productName}" missing — enable LocalDevFixtures`,
    );

    expect(await addE2EProductToCart(page)).toBeTruthy();

    await page.goto("/cart");
    await expect(page.getByRole("heading", { level: 2 }).filter({
      hasText: E2E_FIXTURE.productName,
    })).toBeVisible();

    const increase = page.getByRole("button", {
      name: new RegExp(`زيادة كمية.*${E2E_FIXTURE.productName}`),
    });
    await increase.click();

    const qtyInput = page.locator('input[type="number"]').first();
    await expect(qtyInput).toHaveValue("2");

    await page.reload();
    await expect(qtyInput).toHaveValue("2", { timeout: 15_000 });

    await page.getByRole("link", { name: "إتمام الطلب" }).click();
    await expect(page).toHaveURL(/\/checkout/);

    await fillGuestCheckoutForm(page);

    const submit = page.getByRole("button", { name: "تأكيد الطلب" });
    await expect(submit).toBeEnabled({ timeout: 30_000 });
    await submit.click();

    await expect(page).toHaveURL(/\/orders\/[^/]+\/confirmation/, {
      timeout: 45_000,
    });
    await expect(page.getByText("تم استلام طلبك")).toBeVisible();
  });
});
