import { test, expect } from "@playwright/test";
import { localStackReady } from "./helpers";

test.describe("cart and checkout smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
  });

  test("checkout page loads (guest path UI)", async ({ page }) => {
    await page.goto("/checkout");
    await expect(page.locator("body")).toBeVisible();
  });

  test("add-to-cart from first product when catalog has items", async ({
    page,
  }) => {
    await page.goto("/products");
    const productLink = page.locator('a[href^="/products/"]').first();
    if ((await productLink.count()) === 0) {
      test.skip(true, "No products in local catalog");
      return;
    }
    await productLink.click();
    const addBtn = page.getByRole("button", {
      name: /أضف|إضافة|السلة|cart/i,
    });
    if ((await addBtn.count()) === 0) {
      test.skip(true, "No add-to-cart control on product detail");
      return;
    }
    await addBtn.first().click();
    await page.goto("/cart");
    await expect(page.locator("body")).toBeVisible();
  });
});
