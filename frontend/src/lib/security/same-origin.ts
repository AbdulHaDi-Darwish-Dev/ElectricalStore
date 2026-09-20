/**
 * Same-origin helpers for Next guest-order mutation routes.
 * Prefer server SITE_URL; fall back to NEXT_PUBLIC_SITE_URL / localhost.
 */

export function getExpectedSiteOrigin(): string {
  const raw =
    process.env.SITE_URL ??
    process.env.NEXT_PUBLIC_SITE_URL ??
    "http://localhost:3100";
  try {
    return new URL(raw).origin;
  } catch {
    return "http://localhost:3100";
  }
}

/**
 * Browser fetch/XHR always send Origin on POST.
 * Missing Origin is rejected (non-browser / forged) — tests must supply Origin.
 */
export function isAllowedBrowserOrigin(
  originHeader: string | null,
  expectedOrigin: string = getExpectedSiteOrigin(),
): boolean {
  if (!originHeader) {
    return false;
  }
  try {
    return new URL(originHeader).origin === new URL(expectedOrigin).origin;
  } catch {
    return false;
  }
}

export function isJsonContentType(contentType: string | null): boolean {
  if (!contentType) {
    return false;
  }
  return contentType.toLowerCase().includes("application/json");
}

export const PRIVATE_NO_STORE_HEADERS = {
  "Cache-Control": "private, no-store, no-cache, must-revalidate",
  Pragma: "no-cache",
} as const;
