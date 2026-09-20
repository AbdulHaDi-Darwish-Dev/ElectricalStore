/**
 * Local Login BFF → ASP.NET client-IP identity.
 *
 * CURRENT topology (no reverse proxy in front of Next):
 *   Browser → Next → ASP.NET
 *
 * There is NO trusted ingress. Browser-supplied headers such as
 * X-Real-IP, X-Forwarded-For, Forwarded, or custom headers are
 * attacker-controlled and MUST NOT choose the rate-limit identity.
 *
 * Development KnownProxies trust loopback only (F6.0). Forward the
 * explicit loopback identity so ASP.NET RemoteIp resolves consistently
 * for the single shared local Login bucket.
 *
 * This is a LOCAL DEVELOPMENT identity only.
 * It does NOT simulate production per-client IP partitioning.
 *
 * Production client-IP forwarding is intentionally NOT implemented here
 * until trusted ingress exists and KnownProxies/Networks are configured.
 */

/** Loopback identity accepted by Development ForwardedHeaders KnownProxies. */
export const LOCAL_DEV_LOGIN_CLIENT_IP = "127.0.0.1";

/**
 * Identity the Login BFF may forward as X-Forwarded-For today.
 * Does not read the Request — header presence/syntax is not trust.
 */
export function resolveLocalDevLoginClientIp(): string {
  return LOCAL_DEV_LOGIN_CLIENT_IP;
}

/**
 * @deprecated Prefer resolveLocalDevLoginClientIp.
 * Kept so tests/call sites can pass a Request without implying header trust.
 */
export function resolveForwardableClientIp(_request?: Request): string {
  void _request;
  return resolveLocalDevLoginClientIp();
}
