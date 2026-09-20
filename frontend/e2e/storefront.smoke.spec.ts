import { test, expect } from "@playwright/test";
import { localStackReady } from "./helpers";

test.describe("anonymous storefront smoke", () => {
  test.beforeEach(async () => {
    test.skip(
      !(await localStackReady()),
      "Local stack not reachable — start frontend :3100 and API :5180",
    );
  });

  test("home loads without auth", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator("body")).toBeVisible();
  });

  test("categories and products routes respond", async ({ page }) => {
    await page.goto("/categories");
    await expect(page.locator("body")).toBeVisible();
    await page.goto("/products");
    await expect(page.locator("body")).toBeVisible();
  });

  test("cart page is reachable", async ({ page }) => {
    await page.goto("/cart");
    await expect(page.locator("body")).toBeVisible();
  });

  test("robots and sitemap are public", async ({ request }) => {
    const robots = await request.get("/robots.txt");
    expect(robots.ok()).toBeTruthy();
    const sitemap = await request.get("/sitemap.xml");
    expect(sitemap.ok()).toBeTruthy();
  });
});
