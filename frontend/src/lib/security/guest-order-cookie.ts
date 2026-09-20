/**
 * Guest order HttpOnly cookie helpers (security transport only).
 *
 * Backend guestAccessToken has NO expiry: Order stores SHA-256 hash only;
 * TokensMatch is hash equality. The 24h window applies to Place Order
 * IDEMPOTENCY retention, not guest access.
 *
 * Cookie Max-Age is a deliberate frontend access window (not permanent,
 * not tied to idempotency).
 */

/** Cookie prefix — order-scoped. Brand-neutral. */
export const GUEST_ORDER_COOKIE_PREFIX = "electricalstore.guest-order.";

/**
 * Pragmatic browser access window for confirmation/reload tracking.
 * Backend token remains valid for the life of the order hash.
 */
export const GUEST_ORDER_COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 30; // 30 days

export function guestOrderCookieName(orderId: string): string {
  return `${GUEST_ORDER_COOKIE_PREFIX}${orderId}`;
}

/** Narrow Path so credentials are not sent on unrelated storefront requests. */
export function guestOrderCookiePath(orderId: string): string {
  return `/api/guest-orders/${orderId}`;
}

export function guestOrderCookieOptions(
  orderId: string,
  options?: { nodeEnv?: string },
) {
  const nodeEnv = options?.nodeEnv ?? process.env.NODE_ENV;
  const secure = nodeEnv === "production";
  return {
    httpOnly: true as const,
    secure,
    sameSite: "lax" as const,
    path: guestOrderCookiePath(orderId),
    maxAge: GUEST_ORDER_COOKIE_MAX_AGE_SECONDS,
  };
}
