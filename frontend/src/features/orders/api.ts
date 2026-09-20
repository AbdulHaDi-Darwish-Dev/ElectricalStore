import { apiFetch } from "@/lib/api";
import {
  IDEMPOTENCY_KEY_HEADER,
  type OrderDto,
  type PlaceOrderRequest,
} from "./types";

/**
 * Guest Place Order via Next security boundary (not direct ASP.NET).
 * Browser never receives/stores guestAccessToken.
 */
export function placeGuestOrder(
  request: PlaceOrderRequest,
  idempotencyKey: string,
): Promise<OrderDto> {
  return apiFetch<OrderDto>("/api/guest-orders/place", {
    method: "POST",
    body: request,
    headers: {
      [IDEMPOTENCY_KEY_HEADER]: idempotencyKey,
    },
    cache: "no-store",
    // Same-origin Next route — empty base uses relative URL via joinApiUrl.
    // Override: call browser origin relative path.
    baseUrl: "",
  });
}

/**
 * Reload guest order confirmation via Next security boundary.
 */
export function getGuestOrder(orderId: string): Promise<OrderDto> {
  return apiFetch<OrderDto>(`/api/guest-orders/${orderId}`, {
    method: "GET",
    cache: "no-store",
    baseUrl: "",
  });
}
