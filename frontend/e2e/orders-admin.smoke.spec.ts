import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  addE2EProductToCart,
  assertDecimalClose,
  catalogHasE2EProduct,
  e2eEnv,
  fetchAccessTokenViaLogin,
  fetchE2EInventory,
  fillGuestCheckoutForm,
  hasAdminCreds,
  localStackReady,
  loginAs,
  runAdminOrderAction,
} from "./helpers";

const ADMIN_ORDER_ACTIONS = [
  "تأكيد الطلب",
  "بدء التجهيز",
  "إرسال للتوصيل",
  "تأكيد التسليم",
] as const;

test.describe("admin order fulfillment smoke", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("guest places order then admin advances fulfillment with inventory checks", async ({
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
    const before = await fetchE2EInventory(request, token!);
    const orderQty = 1;

    expect(await addE2EProductToCart(page)).toBeTruthy();
    await page.goto("/checkout");
    await fillGuestCheckoutForm(page);

    const submit = page.getByRole("button", { name: "تأكيد الطلب" });
    await expect(submit).toBeEnabled({ timeout: 30_000 });
    await submit.click();

    await expect(page).toHaveURL(/\/orders\/([^/]+)\/confirmation/, {
      timeout: 45_000,
    });
    const orderId = page.url().match(/\/orders\/([^/]+)\/confirmation/)?.[1];
    expect(orderId).toBeTruthy();

    await loginAs(page, e2eEnv.adminEmail, e2eEnv.adminPassword);
    await page.goto(`/admin/orders/${orderId}`);
    await expect(page.locator("body")).not.toContainText("Application error");
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible({
      timeout: 20_000,
    });

    await runAdminOrderAction(page, ADMIN_ORDER_ACTIONS[0]);
    const afterConfirm = await fetchE2EInventory(request, token!);
    assertDecimalClose(afterConfirm.onHand, before.onHand, "onHand after confirm");
    assertDecimalClose(
      afterConfirm.reserved,
      before.reserved + orderQty,
      "reserved after confirm",
    );

    await runAdminOrderAction(page, ADMIN_ORDER_ACTIONS[1]);
    const afterPrepare = await fetchE2EInventory(request, token!);
    assertDecimalClose(
      afterPrepare.onHand,
      afterConfirm.onHand,
      "onHand after prepare",
    );
    assertDecimalClose(
      afterPrepare.reserved,
      afterConfirm.reserved,
      "reserved after prepare",
    );

    await runAdminOrderAction(page, ADMIN_ORDER_ACTIONS[2]);
    const afterOut = await fetchE2EInventory(request, token!);
    assertDecimalClose(
      afterOut.onHand,
      before.onHand - orderQty,
      "onHand after OutForDelivery",
    );
    assertDecimalClose(
      afterOut.reserved,
      before.reserved,
      "reserved after OutForDelivery",
    );
    assertDecimalClose(
      afterOut.available,
      before.available - orderQty,
      "available after OutForDelivery",
    );

    await runAdminOrderAction(page, ADMIN_ORDER_ACTIONS[3]);
    const afterDeliver = await fetchE2EInventory(request, token!);
    assertDecimalClose(
      afterDeliver.onHand,
      afterOut.onHand,
      "onHand after deliver (unchanged)",
    );
    assertDecimalClose(
      afterDeliver.reserved,
      afterOut.reserved,
      "reserved after deliver (unchanged)",
    );

    await expect(page.getByText("تم التسليم").first()).toBeVisible({
      timeout: 20_000,
    });
  });
});
