/**
 * Login BFF → ASP.NET client-IP identity for rate limiting.
 *
 * LOCAL (AUTH_TRUST_PROXY unset/false):
 *   No trusted ingress. Never read browser XFF / X-Real-IP / Forwarded.
 *   Forward loopback 127.0.0.1 (Development KnownProxies).
 *
 * PRODUCTION (AUTH_TRUST_PROXY=true) — means “behind expected Nginx topology”,
 * NOT “trust whatever the browser sent”:
 *   1. Nginx OVERWRITES X-ElectricalStore-Client-Ip = $remote_addr
 *   2. Next reads ONLY that custom header (ignores browser XFF)
 *   3. Next sends a FRESH X-Forwarded-For to ASP.NET with that IP
 *   4. ASP.NET ForwardedHeaders consumes X-Forwarded-For only (no custom-header middleware)
 *   5. RemoteIp = client when TCP peer is KnownProxy (Next 10.80.0.20 or Nginx 10.80.0.10)
 */

/** Loopback identity accepted by Development ForwardedHeaders KnownProxies. */
export const LOCAL_DEV_LOGIN_CLIENT_IP = "127.0.0.1";

/** Header Nginx sets to the TCP client IP (overwritten; not client-appendable). */
export const TRUSTED_CLIENT_IP_HEADER = "x-electricalstore-client-ip";

const IPV4 =
  /^(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)$/;
const IPV6 =
  /^(?:[0-9a-fA-F]{0,4}:){2,7}[0-9a-fA-F]{0,4}$|^::1$|^::$/;

export function isAuthTrustProxyEnabled(
  env: NodeJS.ProcessEnv = process.env,
): boolean {
  const raw = (env.AUTH_TRUST_PROXY ?? "").trim().toLowerCase();
  return raw === "1" || raw === "true" || raw === "yes";
}

/** Single IP only — reject comma lists / unknown junk. */
export function parseSingleIp(raw: string | null | undefined): string | null {
  if (!raw) return null;
  const value = raw.trim();
  if (!value || value.includes(",") || value.includes(" ")) return null;
  if (IPV4.test(value) || IPV6.test(value)) return value;
  return null;
}

/**
 * Identity the Login BFF may forward as X-Forwarded-For to ASP.NET.
 */
export function resolveLoginClientIp(
  request: Request,
  env: NodeJS.ProcessEnv = process.env,
): string {
  if (!isAuthTrustProxyEnabled(env)) {
    return LOCAL_DEV_LOGIN_CLIENT_IP;
  }

  const trusted = parseSingleIp(
    request.headers.get(TRUSTED_CLIENT_IP_HEADER),
  );
  if (trusted) return trusted;

  // Fail closed: do not fall back to browser XFF or a shared bucket IP.
  throw new Error(
    "AUTH_TRUST_PROXY is enabled but X-ElectricalStore-Client-Ip is missing or invalid. " +
      "Nginx must overwrite this header to $remote_addr.",
  );
}

/** @deprecated Prefer resolveLoginClientIp. Local-only helper. */
export function resolveLocalDevLoginClientIp(): string {
  return LOCAL_DEV_LOGIN_CLIENT_IP;
}

/**
 * @deprecated Prefer resolveLoginClientIp.
 * Local mode ignores Request headers; production trust uses resolveLoginClientIp.
 */
export function resolveForwardableClientIp(
  request?: Request,
  env: NodeJS.ProcessEnv = process.env,
): string {
  if (!request) {
    return LOCAL_DEV_LOGIN_CLIENT_IP;
  }
  if (!isAuthTrustProxyEnabled(env)) {
    return LOCAL_DEV_LOGIN_CLIENT_IP;
  }
  return resolveLoginClientIp(request, env);
}
