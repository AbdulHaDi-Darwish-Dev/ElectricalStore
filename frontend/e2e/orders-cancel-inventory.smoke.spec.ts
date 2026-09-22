import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  addE2EProductToCart,
  assertDecimalClose,
  cancelAdminOrderWithReason,
  catalogHasE2EProduct,
  e2eEnv,
  fetchAccessTokenViaLogin,
  fetchE2EInventory,
  fillGuestCheckoutForm,
  hasAdminCreds,
  localStackReady,
  loginAs,
  runAdminOrderAction,
  type InventorySnapshot,
} from "./helpers";

test.describe("order inventory side effects", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("confirm then cancel releases reserved stock", async ({
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

    const afterPlace = await fetchE2EInventory(request, token!);
    assertInventoryUnchanged(before, afterPlace, "after place (no reserve)");

    await loginAs(page, e2eEnv.adminEmail, e2eEnv.adminPassword);
    await page.goto(`/admin/orders/${orderId}`);
    await runAdminOrderAction(page, "تأكيد الطلب");

    const afterConfirm = await fetchE2EInventory(request, token!);
    assertDecimalClose(afterConfirm.onHand, before.onHand, "onHand after confirm");
    assertDecimalClose(
      afterConfirm.reserved,
      before.reserved + orderQty,
      "reserved after confirm",
    );
    assertDecimalClose(
      afterConfirm.available,
      before.available - orderQty,
      "available after confirm",
    );

    await cancelAdminOrderWithReason(page, "E2E cancel inventory release");

    const afterCancel = await fetchE2EInventory(request, token!);
    assertDecimalClose(afterCancel.onHand, before.onHand, "onHand after cancel");
    assertDecimalClose(
      afterCancel.reserved,
      before.reserved,
      "reserved after cancel",
    );
    assertDecimalClose(
      afterCancel.available,
      before.available,
      "available after cancel",
    );
  });
});

function assertInventoryUnchanged(
  a: InventorySnapshot,
  b: InventorySnapshot,
  label: string,
): void {
  assertDecimalClose(b.onHand, a.onHand, `${label} onHand`);
  assertDecimalClose(b.reserved, a.reserved, `${label} reserved`);
  assertDecimalClose(b.available, a.available, `${label} available`);
}
