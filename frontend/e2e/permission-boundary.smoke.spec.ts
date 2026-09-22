import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  e2eEnv,
  fetchAccessTokenViaLogin,
  hasLimitedCreds,
  isAuthenticatedSession,
  localStackReady,
  loginAs,
} from "./helpers";

test.describe("permission boundary smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasLimitedCreds(), "Set E2E_LIMITED_EMAIL / E2E_LIMITED_PASSWORD");
  });

  test("limited user can read orders but confirm returns 403 and session remains", async ({
    page,
    request,
  }) => {
    // Place a pending order as guest via API preview/place would be heavier;
    // use any existing order id from public confirmation path is optional.
    // API boundary is the source of truth for Manage denial.
    const token = await fetchAccessTokenViaLogin(
      request,
      e2eEnv.limitedEmail,
      e2eEnv.limitedPassword,
    );
    expect(token).toBeTruthy();

    const listRes = await request.get(`${e2eEnv.apiUrl}/admin/orders`, {
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: "application/json",
      },
    });
    expect(listRes.status()).toBe(200);

    const confirmRes = await request.post(
      `${e2eEnv.apiUrl}/admin/orders/00000000-0000-0000-0000-000000000001/confirm`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: "application/json",
        },
      },
    );
    // Forbidden (no Orders.Manage) — not Unauthorized.
    expect(confirmRes.status()).toBe(403);

    await loginAs(page, e2eEnv.limitedEmail, e2eEnv.limitedPassword);
    await page.goto("/admin/orders");
    await expect(page.getByRole("heading", { name: "الطلبات" })).toBeVisible({
      timeout: 20_000,
    });
    await page.goto("/admin/categories");
    await expect(page.getByText("رفض الوصول")).toBeVisible({
      timeout: 15_000,
    });
    expect(await isAuthenticatedSession(page)).toBeTruthy();
    void E2E_FIXTURE;
  });
});
