/**
 * Shared E2E helpers. Never hardcode production credentials.
 * Supply via env (see e2e/README.md).
 */

import { expect, type APIRequestContext, type Page } from "@playwright/test";

export const e2eEnv = {
  baseUrl: process.env.E2E_BASE_URL ?? "http://localhost:3100",
  apiUrl: process.env.E2E_API_URL ?? "http://localhost:5180",
  customerEmail: process.env.E2E_CUSTOMER_EMAIL ?? "",
  customerPassword: process.env.E2E_CUSTOMER_PASSWORD ?? "",
  adminEmail: process.env.E2E_ADMIN_EMAIL ?? "",
  adminPassword: process.env.E2E_ADMIN_PASSWORD ?? "",
  /** Account with some Manage but without shell/read as needed for 403 cases */
  limitedEmail: process.env.E2E_LIMITED_EMAIL ?? "",
  limitedPassword: process.env.E2E_LIMITED_PASSWORD ?? "",
};

/** Local dev catalog fixtures (LocalDevCatalogFixtureSeeder). */
export const E2E_FIXTURE = {
  productName: "E2E Product",
  categoryName: "E2E Category",
  shippingZoneName: "E2E Shipping Zone",
  variantSku: "E2E-STD-001",
} as const;

export function hasCustomerCreds(): boolean {
  return Boolean(e2eEnv.customerEmail && e2eEnv.customerPassword);
}

export function hasAdminCreds(): boolean {
  return Boolean(e2eEnv.adminEmail && e2eEnv.adminPassword);
}

export function hasLimitedCreds(): boolean {
  return Boolean(e2eEnv.limitedEmail && e2eEnv.limitedPassword);
}

export async function apiHealthy(): Promise<boolean> {
  try {
    const res = await fetch(`${e2eEnv.apiUrl}/health`, {
      signal: AbortSignal.timeout(5000),
    });
    return res.ok;
  } catch {
    return false;
  }
}

export type CapturedDevEmail = {
  id?: string;
  capturedAtUtc?: string;
  to: string;
  from: string;
  subject: string;
  textBody: string;
  htmlBody: string;
  previewUrl?: string;
  textUrl?: string;
};

/** Development CapturingEmailSender outbox (requires Email:UseCapturingSender=true). */
export async function fetchDevEmailOutbox(): Promise<CapturedDevEmail[]> {
  const res = await fetch(`${e2eEnv.apiUrl}/dev/email-outbox`, {
    signal: AbortSignal.timeout(5000),
  });
  if (!res.ok) {
    throw new Error(`Dev email outbox unavailable: HTTP ${res.status}`);
  }
  return (await res.json()) as CapturedDevEmail[];
}

export async function clearDevEmailOutbox(): Promise<void> {
  await fetch(`${e2eEnv.apiUrl}/dev/email-outbox`, {
    method: "DELETE",
    signal: AbortSignal.timeout(5000),
  });
}

export function extractVerificationPath(textBody: string): string {
  const match = textBody.match(
    /https?:\/\/[^\s]+(\/verify-email\?challengeId=[0-9a-fA-F-]{36}&token=[^\s]+)/,
  );
  if (!match) {
    throw new Error("Verification link not found in email text body");
  }
  return match[1];
}

export async function confirmLatestVerificationEmail(
  page: Page,
  email: string,
): Promise<void> {
  const mailbox = await fetchDevEmailOutbox();
  const message = [...mailbox]
    .reverse()
    .find((m) => m.to.toLowerCase() === email.toLowerCase());
  if (!message) {
    throw new Error(`No captured email for ${email}`);
  }
  const path = extractVerificationPath(message.textBody);
  await page.goto(path);
  await expect(
    page.getByRole("heading", { name: /تم تأكيد بريدك الإلكتروني/ }),
  ).toBeVisible({
    timeout: 20_000,
  });
}

export async function frontendHealthy(): Promise<boolean> {
  try {
    const res = await fetch(e2eEnv.baseUrl, {
      signal: AbortSignal.timeout(5000),
    });
    return res.ok || res.status === 404 || res.status === 307 || res.status === 308;
  } catch {
    return false;
  }
}

/** Local stack ready for browser smoke (API + frontend). */
export async function localStackReady(): Promise<boolean> {
  return (await apiHealthy()) && (await frontendHealthy());
}

export async function loginAs(
  page: Page,
  email: string,
  password: string,
): Promise<void> {
  await page.goto("/login");
  await page.locator("#emailOrUserName").fill(email);
  await page.locator('input[type="password"]').first().fill(password);
  await page.getByRole("button", { name: "تسجيل الدخول" }).click();
  // Wait until session is established (header logout or leave login URL).
  await page
    .getByRole("button", { name: /تسجيل الخروج/i })
    .first()
    .waitFor({ state: "visible", timeout: 30_000 });
}

export async function logout(page: Page): Promise<void> {
  const logoutBtn = page.getByRole("button", { name: /تسجيل الخروج|logout/i });
  await logoutBtn.first().waitFor({ state: "visible", timeout: 15_000 });
  await logoutBtn.first().click();
  await page
    .getByRole("link", { name: /تسجيل الدخول|login/i })
    .first()
    .waitFor({ state: "visible", timeout: 15_000 });
}

/** True when header logout is visible or /account is not redirected to login. */
export async function isAuthenticatedSession(page: Page): Promise<boolean> {
  const logoutBtn = page.getByRole("button", { name: /تسجيل الخروج/i });
  if (await logoutBtn.first().isVisible().catch(() => false)) {
    return true;
  }
  await page.goto("/account");
  await page.waitForLoadState("domcontentloaded");
  return !page.url().includes("/login");
}

export async function catalogHasE2EProduct(page: Page): Promise<boolean> {
  await page.goto("/products");
  const link = page.getByRole("link", { name: E2E_FIXTURE.productName });
  return (await link.count()) > 0;
}

/** Opens the E2E Product detail page. Returns false if the fixture is missing. */
export async function gotoProductE2E(page: Page): Promise<boolean> {
  // Prefer direct API id to avoid list/card flakes.
  try {
    const res = await fetch(`${e2eEnv.apiUrl}/catalog/products`, {
      signal: AbortSignal.timeout(5000),
    });
    if (res.ok) {
      const body = (await res.json()) as
        | { id: string; name: string }[]
        | { items?: { id: string; name: string }[] };
      const list = Array.isArray(body) ? body : (body.items ?? []);
      const match = list.find((p) => p.name === E2E_FIXTURE.productName);
      if (match?.id) {
        await page.goto(`/products/${match.id}`);
        await page.waitForURL(new RegExp(`/products/${match.id}`), {
          timeout: 15_000,
        });
        return true;
      }
    }
  } catch {
    // fall through to link navigation
  }

  await page.goto("/products");
  const link = page.getByRole("link", { name: E2E_FIXTURE.productName });
  if ((await link.count()) === 0) {
    return false;
  }
  await link.first().click();
  await page.waitForURL(/\/products\//, { timeout: 15_000 });
  return true;
}

export async function addE2EProductToCart(page: Page): Promise<boolean> {
  if (!(await gotoProductE2E(page))) {
    return false;
  }
  const addBtn = page.getByRole("button", { name: "أضف إلى السلة" });
  try {
    await addBtn.waitFor({ state: "visible", timeout: 15_000 });
  } catch {
    return false;
  }
  if (await addBtn.isDisabled()) {
    return false;
  }
  await addBtn.click();
  await page
    .getByRole("status", { name: "تمت الإضافة إلى السلة." })
    .waitFor({ state: "visible", timeout: 10_000 })
    .catch(() => undefined);
  return true;
}

export async function selectE2EShippingZone(page: Page): Promise<void> {
  const select = page.locator("#deliveryZoneId");
  await select.waitFor({ state: "visible", timeout: 15_000 });
  // Options render as "{name} — {fee}"; match by containing the fixture name.
  const option = select.locator("option").filter({
    hasText: E2E_FIXTURE.shippingZoneName,
  });
  await expect.poll(async () => option.count()).toBeGreaterThan(0);
  const value = await option.first().getAttribute("value");
  if (!value) {
    throw new Error(
      `No delivery zone option found for "${E2E_FIXTURE.shippingZoneName}"`,
    );
  }
  await select.selectOption(value);
}

export type GuestCheckoutDetails = {
  customerName: string;
  phone: string;
  addressText: string;
};

export function uniqueGuestCheckoutDetails(): GuestCheckoutDetails {
  const stamp = Date.now().toString().slice(-8);
  return {
    customerName: "E2E Guest",
    phone: `09${stamp.padStart(8, "0").slice(0, 8)}`,
    addressText: "دمشق — عنوان اختبار E2E",
  };
}

export async function fillGuestCheckoutForm(
  page: Page,
  details: GuestCheckoutDetails = uniqueGuestCheckoutDetails(),
): Promise<GuestCheckoutDetails> {
  await page.locator("#customerName").fill(details.customerName);
  await page.locator("#phone").fill(details.phone);
  await page.locator("#addressText").fill(details.addressText);
  await selectE2EShippingZone(page);
  return details;
}

/** Clicks an admin order action and confirms the native dialog. */
export async function runAdminOrderAction(
  page: Page,
  actionButtonLabel: string,
): Promise<void> {
  await page.getByRole("button", { name: actionButtonLabel }).click();
  const dialog = page.locator("dialog[open]");
  await dialog.waitFor({ state: "visible", timeout: 15_000 });
  await dialog.getByRole("button", { name: actionButtonLabel }).click();
  await dialog.waitFor({ state: "hidden", timeout: 30_000 });
}

export async function fetchAccessTokenViaLogin(
  request: APIRequestContext,
  email: string,
  password: string,
): Promise<string | null> {
  const origin = e2eEnv.baseUrl.replace(/\/+$/, "");
  const res = await request.post(`${origin}/api/auth/login`, {
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Origin: origin,
    },
    data: {
      emailOrUserName: email,
      password,
    },
  });
  if (!res.ok()) {
    return null;
  }
  const body = (await res.json()) as {
    kind?: string;
    accessToken?: string;
  };
  if (body.kind === "authenticated" && typeof body.accessToken === "string") {
    return body.accessToken;
  }
  return null;
}

export type InventorySnapshot = {
  variantId: string;
  sku: string;
  onHand: number;
  reserved: number;
  available: number;
};

export function assertDecimalClose(
  actual: number,
  expected: number,
  label: string,
  epsilon = 0.0001,
): void {
  expect(
    Math.abs(actual - expected) <= epsilon,
    `${label}: expected ${expected}, got ${actual}`,
  ).toBeTruthy();
}

export async function fetchE2EInventory(
  request: APIRequestContext,
  accessToken: string,
): Promise<InventorySnapshot> {
  const res = await request.get(
    `${e2eEnv.apiUrl}/admin/inventory?search=${encodeURIComponent(E2E_FIXTURE.variantSku)}`,
    {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        Accept: "application/json",
      },
    },
  );
  expect(res.ok(), `inventory list HTTP ${res.status()}`).toBeTruthy();
  const rows = (await res.json()) as InventorySnapshot[];
  const match = rows.find((r) => r.sku === E2E_FIXTURE.variantSku);
  expect(match, `inventory row for ${E2E_FIXTURE.variantSku}`).toBeTruthy();
  return match!;
}

export async function fetchE2EProductId(): Promise<string | null> {
  try {
    const res = await fetch(`${e2eEnv.apiUrl}/catalog/products`, {
      signal: AbortSignal.timeout(5000),
    });
    if (!res.ok) return null;
    const body = (await res.json()) as
      | { id: string; name: string }[]
      | { items?: { id: string; name: string }[] };
    const list = Array.isArray(body) ? body : (body.items ?? []);
    return list.find((p) => p.name === E2E_FIXTURE.productName)?.id ?? null;
  } catch {
    return null;
  }
}

/** Guest/authenticated checkout still requires recipient fields. */
export async function fillAuthenticatedCheckoutForm(
  page: Page,
  details: GuestCheckoutDetails = uniqueGuestCheckoutDetails(),
): Promise<GuestCheckoutDetails> {
  const stamped: GuestCheckoutDetails = {
    ...details,
    customerName: `E2E Customer ${details.phone.slice(-4)}`,
  };
  await page.locator("#customerName").fill(stamped.customerName);
  await page.locator("#phone").fill(stamped.phone);
  await page.locator("#addressText").fill(stamped.addressText);
  await selectE2EShippingZone(page);
  return stamped;
}

export async function cancelAdminOrderWithReason(
  page: Page,
  reason: string,
): Promise<void> {
  // Cancel opens an inline form (not the confirm dialog used by other actions).
  await page.getByRole("button", { name: "إلغاء الطلب" }).click();
  await page.locator("#cancel-reason").waitFor({ state: "visible", timeout: 15_000 });
  await page.locator("#cancel-reason").fill(reason);
  await page.getByRole("button", { name: "تأكيد الإلغاء" }).click();
  await expect(page.getByText("ملغى").first()).toBeVisible({
    timeout: 30_000,
  });
}

export async function getOrderingMinimum(
  request: APIRequestContext,
  accessToken: string,
): Promise<number> {
  const res = await request.get(`${e2eEnv.apiUrl}/admin/settings/ordering`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: "application/json",
    },
  });
  expect(res.ok()).toBeTruthy();
  const body = (await res.json()) as { minimumMerchandiseSubtotal: number };
  return body.minimumMerchandiseSubtotal;
}

export async function putOrderingMinimum(
  request: APIRequestContext,
  accessToken: string,
  value: number,
): Promise<void> {
  const res = await request.put(`${e2eEnv.apiUrl}/admin/settings/ordering`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    data: { minimumMerchandiseSubtotal: value },
  });
  expect(res.ok(), `PUT ordering settings HTTP ${res.status()}`).toBeTruthy();
}
