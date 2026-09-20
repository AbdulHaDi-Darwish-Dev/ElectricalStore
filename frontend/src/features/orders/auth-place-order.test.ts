import { describe, expect, it } from "vitest";
import type { PlaceOrderRequest } from "@/features/orders";

/**
 * Documents authenticated Place Order header behavior expected by the BFF.
 * Route handler forwards Authorization when present; omits guest cookie when
 * guestAccessToken is null/absent.
 */
describe("authenticated place-order contract", () => {
  it("builds Authorization bearer header only when access token present", () => {
    const token = "access-xyz";
    const headers: Record<string, string> = {
      "Idempotency-Key": "k".repeat(16),
    };
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    expect(headers.Authorization).toBe("Bearer access-xyz");
  });

  it("does not create guest cookie when guestAccessToken is null", () => {
    const order = {
      id: "o1",
      guestAccessToken: null as string | null,
    };
    const shouldSetCookie =
      typeof order.guestAccessToken === "string" && Boolean(order.guestAccessToken);
    expect(shouldSetCookie).toBe(false);
  });

  it("keeps place payload free of invented auth fields", () => {
    const body: PlaceOrderRequest = {
      items: [{ variantId: "v1", quantity: 1 }],
      deliveryZoneId: "00000000-0000-0000-0000-000000000001",
      customerName: "A",
      phone: "1",
      addressText: "x",
    };
    expect(body).not.toHaveProperty("userId");
    expect(body).not.toHaveProperty("accessToken");
  });
});
