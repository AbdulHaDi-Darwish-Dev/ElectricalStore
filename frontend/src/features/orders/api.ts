import { apiFetch } from "@/lib/api";
import {
  IDEMPOTENCY_KEY_HEADER,
  type OrderDto,
  type PlaceOrderRequest,
} from "./types";

/**
 * Place Order via Next security boundary (not direct ASP.NET).
 * Optional Bearer for authenticated customers — Next forwards Authorization.
 * Browser never receives/stores guestAccessToken.
 */
export function placeOrder(
  request: PlaceOrderRequest,
  idempotencyKey: string,
  accessToken?: string | null,
): Promise<OrderDto> {
  const headers: Record<string, string> = {
    [IDEMPOTENCY_KEY_HEADER]: idempotencyKey,
  };
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  return apiFetch<OrderDto>("/api/guest-orders/place", {
    method: "POST",
    body: request,
    headers,
    cache: "no-store",
    baseUrl: "",
  });
}

/** Guest-only alias. */
export function placeGuestOrder(
  request: PlaceOrderRequest,
  idempotencyKey: string,
): Promise<OrderDto> {
  return placeOrder(request, idempotencyKey, null);
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
