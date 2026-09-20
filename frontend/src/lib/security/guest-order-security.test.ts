import { describe, expect, it } from "vitest";
import {
  GUEST_ORDER_COOKIE_MAX_AGE_SECONDS,
  guestOrderCookieName,
  guestOrderCookieOptions,
  guestOrderCookiePath,
} from "@/lib/security/guest-order-cookie";
import {
  getExpectedSiteOrigin,
  isAllowedBrowserOrigin,
  isJsonContentType,
  PRIVATE_NO_STORE_HEADERS,
} from "@/lib/security/same-origin";
import type { OrderDto } from "@/features/orders";
import { toClientSafeOrderDto } from "@/features/orders/safe-dto";

describe("guest order cookie strategy", () => {
  it("uses a 30-day Max-Age (not idempotency 24h)", () => {
    expect(GUEST_ORDER_COOKIE_MAX_AGE_SECONDS).toBe(60 * 60 * 24 * 30);
    expect(GUEST_ORDER_COOKIE_MAX_AGE_SECONDS).not.toBe(60 * 60 * 24);
  });

  it("is HttpOnly, SameSite=Lax, and Secure only in production", () => {
    const dev = guestOrderCookieOptions("order-a", { nodeEnv: "development" });
    expect(dev.httpOnly).toBe(true);
    expect(dev.sameSite).toBe("lax");
    expect(dev.secure).toBe(false);
    expect(dev.maxAge).toBe(GUEST_ORDER_COOKIE_MAX_AGE_SECONDS);

    const prod = guestOrderCookieOptions("order-a", { nodeEnv: "production" });
    expect(prod.secure).toBe(true);
  });

  it("scopes Path to the order-specific guest-order API route", () => {
    expect(guestOrderCookiePath("abc")).toBe("/api/guest-orders/abc");
    expect(guestOrderCookieOptions("abc").path).toBe("/api/guest-orders/abc");
  });

  it("keeps credentials independent per order id", () => {
    expect(guestOrderCookieName("id-1")).not.toBe(guestOrderCookieName("id-2"));
    expect(guestOrderCookiePath("id-1")).not.toBe(guestOrderCookiePath("id-2"));
  });
});

describe("same-origin Place Order protection", () => {
  it("rejects mismatched and missing Origin", () => {
    const expected = "http://localhost:3000";
    expect(isAllowedBrowserOrigin("http://evil.example", expected)).toBe(false);
    expect(isAllowedBrowserOrigin(null, expected)).toBe(false);
    expect(isAllowedBrowserOrigin("", expected)).toBe(false);
  });

  it("accepts matching same-origin Origin", () => {
    expect(
      isAllowedBrowserOrigin("http://localhost:3000", "http://localhost:3000"),
    ).toBe(true);
    expect(
      isAllowedBrowserOrigin("http://localhost:3000/", "http://localhost:3000"),
    ).toBe(true);
  });

  it("requires application/json content type", () => {
    expect(isJsonContentType("application/json")).toBe(true);
    expect(isJsonContentType("application/json; charset=utf-8")).toBe(true);
    expect(isJsonContentType("text/plain")).toBe(false);
    expect(isJsonContentType(null)).toBe(false);
  });

  it("resolves an expected site origin", () => {
    expect(getExpectedSiteOrigin()).toMatch(/^https?:\/\//);
  });
});

describe("private guest-order caching headers", () => {
  it("declares private no-store semantics", () => {
    expect(PRIVATE_NO_STORE_HEADERS["Cache-Control"]).toContain("no-store");
    expect(PRIVATE_NO_STORE_HEADERS["Cache-Control"]).toContain("private");
  });
});

describe("Place Order browser-visible DTO", () => {
  it("never retains raw guestAccessToken after stripping", () => {
    const raw = {
      id: "o1",
      orderNumber: "ES-1",
      status: "PendingConfirmation",
      paymentMethod: "CashOnDelivery",
      paymentStatus: "Unpaid",
      customerName: "A",
      phone: "1",
      addressText: "x",
      customerNote: null,
      deliveryZoneId: "z",
      deliveryZoneName: "Aleppo",
      shippingFee: 1,
      merchandiseSubtotal: 1,
      appliedMinimumOrderAmount: 0,
      total: 2,
      createdAtUtc: new Date().toISOString(),
      confirmedAtUtc: null,
      preparingAtUtc: null,
      outForDeliveryAtUtc: null,
      deliveredAtUtc: null,
      cancelledAtUtc: null,
      cancellationReason: null,
      items: [],
      guestAccessToken: "super-secret-token",
      trackingHint: "/orders/o1/track",
    } satisfies OrderDto;

    const safe = toClientSafeOrderDto(raw);
    expect(safe.guestAccessToken).toBeNull();
    expect(JSON.stringify(safe)).not.toContain("super-secret-token");
    expect(safe.id).toBe("o1");
    expect(safe.orderNumber).toBe("ES-1");
  });
});
