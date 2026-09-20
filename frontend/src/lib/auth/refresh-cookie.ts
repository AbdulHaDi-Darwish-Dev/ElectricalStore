/**
 * Refresh-token HttpOnly cookie (Next security boundary only).
 * Never readable by browser JavaScript.
 */

export const AUTH_REFRESH_COOKIE_NAME = "electricalstore.auth.refresh";

/** Narrow path: cookie only sent to Next auth route handlers. */
export const AUTH_REFRESH_COOKIE_PATH = "/api/auth";

export type AuthRefreshCookieOptionsInput = {
  /** Absolute expiry from backend `refreshTokenExpiresAtUtc`. */
  expiresAtUtc: string;
  nodeEnv?: string;
};

/**
 * Cookie options. Max-Age is derived from refreshTokenExpiresAtUtc (not a hardcoded 7d).
 */
export function authRefreshCookieOptions(input: AuthRefreshCookieOptionsInput) {
  const nodeEnv = input.nodeEnv ?? process.env.NODE_ENV;
  const secure = nodeEnv === "production";
  const maxAge = maxAgeSecondsFromExpiresAtUtc(input.expiresAtUtc);

  return {
    httpOnly: true as const,
    secure,
    sameSite: "lax" as const,
    path: AUTH_REFRESH_COOKIE_PATH,
    maxAge,
  };
}

export function clearAuthRefreshCookieOptions(options?: { nodeEnv?: string }) {
  const nodeEnv = options?.nodeEnv ?? process.env.NODE_ENV;
  return {
    httpOnly: true as const,
    secure: nodeEnv === "production",
    sameSite: "lax" as const,
    path: AUTH_REFRESH_COOKIE_PATH,
    maxAge: 0,
  };
}

/** Seconds until expiry; minimum 1 when still in the future; 0 if already expired/invalid. */
export function maxAgeSecondsFromExpiresAtUtc(expiresAtUtc: string, nowMs: number = Date.now()): number {
  const expiresMs = Date.parse(expiresAtUtc);
  if (Number.isNaN(expiresMs)) {
    return 0;
  }
  const seconds = Math.floor((expiresMs - nowMs) / 1000);
  return seconds > 0 ? seconds : 0;
}
