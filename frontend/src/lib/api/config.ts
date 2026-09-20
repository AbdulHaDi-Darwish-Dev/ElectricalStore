/**
 * API base URL resolution.
 *
 * Server Components / metadata / server rendering → API_BASE_URL
 * Browser Client Components → NEXT_PUBLIC_API_BASE_URL
 *
 * Prefer calling getServerApiBaseUrl / getBrowserApiBaseUrl at intentional
 * boundaries; getApiBaseUrl() remains for shared fetch helpers.
 */

const DEFAULT_API_BASE_URL = "http://localhost:5180";

/** Explicit server-side ASP.NET base URL. */
export function getServerApiBaseUrl(): string {
  return (
    process.env.API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL ??
    DEFAULT_API_BASE_URL
  );
}

/** Explicit browser-side ASP.NET base URL. */
export function getBrowserApiBaseUrl(): string {
  return process.env.NEXT_PUBLIC_API_BASE_URL ?? DEFAULT_API_BASE_URL;
}

/**
 * Runtime-selected base URL for the shared fetch helper.
 * Server: getServerApiBaseUrl(); Browser: getBrowserApiBaseUrl().
 */
export function getApiBaseUrl(): string {
  if (typeof window === "undefined") {
    return getServerApiBaseUrl();
  }
  return getBrowserApiBaseUrl();
}

export function joinApiUrl(path: string, baseUrl: string = getApiBaseUrl()): string {
  const base = baseUrl.replace(/\/+$/, "");
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${base}${normalized}`;
}
