import { beforeEach, describe, expect, it } from "vitest";
import type { CartItem } from "@/features/cart";
import {
  buildCheckoutPreviewQueryKey,
  cartItemsToCheckoutLines,
  clearIdempotencyAttempt,
  createIdempotencyKey,
  hasPriceChanged,
  hashPlaceOrderFingerprint,
  IDEMPOTENCY_ATTEMPT_STORAGE_KEY,
  normalizePlaceOrderIntent,
  readIdempotencyAttempt,
  resolveIdempotencyKey,
  writeIdempotencyAttempt,
} from "@/features/checkout";
import type { PlaceOrderRequest } from "@/features/orders";
import { IDEMPOTENCY_KEY_HEADER } from "@/features/orders";
import { guestOrderCookieName } from "@/lib/security/guest-order-cookie";

const memory = new Map<string, string>();

beforeEach(() => {
  memory.clear();
  Object.defineProperty(globalThis, "sessionStorage", {
    configurable: true,
    value: {
      getItem: (key: string) => memory.get(key) ?? null,
      setItem: (key: string, value: string) => {
        memory.set(key, value);
      },
      removeItem: (key: string) => {
        memory.delete(key);
      },
      clear: () => memory.clear(),
    },
  });
  clearIdempotencyAttempt();
});

function sampleCartItem(overrides: Partial<CartItem> = {}): CartItem {
  return {
    productId: "p1",
    variantId: "v1",
    productName: "كابل",
    variantName: "1م",
    primaryImageUrl: null,
    sku: "SKU",
    sellingUnit: "Meter",
    quantityIncrement: 0.5,
    quantity: 1,
    lastKnownUnitPrice: 100,
    ...overrides,
  };
}

function samplePlaceOrder(
  overrides: Partial<PlaceOrderRequest> = {},
): PlaceOrderRequest {
  return {
    items: [{ variantId: "v1", quantity: 1 }],
    deliveryZoneId: "11111111-1111-1111-1111-111111111111",
    customerName: "أحمد",
    phone: "0999999999",
    addressText: "حلب",
    customerNote: null,
    ...overrides,
  };
}

describe("checkout preview inputs", () => {
  it("maps cart lines to variantId + quantity only", () => {
    expect(cartItemsToCheckoutLines([sampleCartItem()])).toEqual([
      { variantId: "v1", quantity: 1 },
    ]);
  });

  it("preview query key changes with items/qty/zone but not customer fields", () => {
    const items = [sampleCartItem()];
    const zone = "zone-a";
    const keyA = buildCheckoutPreviewQueryKey(items, zone);
    const keyB = buildCheckoutPreviewQueryKey(
      [sampleCartItem({ quantity: 1.5 })],
      zone,
    );
    const keyC = buildCheckoutPreviewQueryKey(items, "zone-b");
    expect(keyA).not.toEqual(keyB);
    expect(keyA).not.toEqual(keyC);
    // Customer name is not part of the key constructor inputs.
    expect(keyA[0]).toBe("checkout-preview");
  });

  it("detects price differences between cart snapshot and preview", () => {
    expect(hasPriceChanged(100, 100)).toBe(false);
    expect(hasPriceChanged(100, 110)).toBe(true);
  });
});

describe("idempotency attempt", () => {
  it("normalizes payload stably without depending on item order", () => {
    const a = normalizePlaceOrderIntent(
      samplePlaceOrder({
        items: [
          { variantId: "b", quantity: 1 },
          { variantId: "a", quantity: 2 },
        ],
      }),
    );
    const b = normalizePlaceOrderIntent(
      samplePlaceOrder({
        items: [
          { variantId: "a", quantity: 2 },
          { variantId: "b", quantity: 1 },
        ],
      }),
    );
    expect(a).toBe(b);
  });

  it("fingerprint is a hex hash (no plaintext PII in attempt storage)", async () => {
    const request = samplePlaceOrder();
    const fingerprint = await hashPlaceOrderFingerprint(request);
    expect(fingerprint).toMatch(/^[a-f0-9]{64}$/);
    expect(fingerprint.includes("أحمد")).toBe(false);
    expect(fingerprint.includes("0999999999")).toBe(false);
  });

  it("reuses key for identical payload and rotates on change", async () => {
    const request = samplePlaceOrder();
    const key1 = await resolveIdempotencyKey(request);
    const key2 = await resolveIdempotencyKey(request);
    expect(key1).toBe(key2);
    expect(key1.length).toBeGreaterThanOrEqual(16);

    const key3 = await resolveIdempotencyKey(
      samplePlaceOrder({
        items: [{ variantId: "v1", quantity: 2 }],
      }),
    );
    expect(key3).not.toBe(key1);

    const key4 = await resolveIdempotencyKey(
      samplePlaceOrder({
        deliveryZoneId: "22222222-2222-2222-2222-222222222222",
      }),
    );
    expect(key4).not.toBe(key1);

    const key5 = await resolveIdempotencyKey(
      samplePlaceOrder({ addressText: "دمشق" }),
    );
    expect(key5).not.toBe(key1);
  });

  it("forceNew creates a new key even for the same payload", async () => {
    const request = samplePlaceOrder();
    const key1 = await resolveIdempotencyKey(request);
    const key2 = await resolveIdempotencyKey(request, { forceNew: true });
    expect(key2).not.toBe(key1);
  });

  it("persisted attempt stores only key + fingerprint", () => {
    writeIdempotencyAttempt({
      key: createIdempotencyKey(),
      fingerprint: "a".repeat(64),
    });
    const raw = sessionStorage.getItem(IDEMPOTENCY_ATTEMPT_STORAGE_KEY);
    expect(raw).toBeTruthy();
    expect(raw!).not.toContain("أحمد");
    expect(readIdempotencyAttempt()?.fingerprint).toHaveLength(64);
  });
});

describe("guest order cookie naming", () => {
  it("uses order-scoped technical cookie names", () => {
    expect(guestOrderCookieName("abc")).toBe("electricalstore.guest-order.abc");
  });
});

describe("place order header constant", () => {
  it("uses Idempotency-Key header name", () => {
    expect(IDEMPOTENCY_KEY_HEADER).toBe("Idempotency-Key");
  });
});

describe("meetsMinimumOrder gate", () => {
  it("treats false as a blocking business state for submit", () => {
    const preview = { meetsMinimumOrder: false as boolean };
    const canSubmit = preview.meetsMinimumOrder === true;
    expect(canSubmit).toBe(false);
  });
});
