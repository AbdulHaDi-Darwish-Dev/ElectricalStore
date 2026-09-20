/**
 * Safe returnTo validation for post-login redirects.
 * Allow only same-origin relative application paths.
 */

export function isSafeReturnTo(value: string | null | undefined): value is string {
  if (value == null) {
    return false;
  }
  const trimmed = value.trim();
  if (!trimmed.startsWith("/")) {
    return false;
  }
  // Reject protocol-relative, backslash tricks, embedded schemes.
  if (trimmed.startsWith("//") || trimmed.startsWith("/\\")) {
    return false;
  }
  if (trimmed.includes("://") || trimmed.includes("\\")) {
    return false;
  }
  const lower = trimmed.toLowerCase();
  if (lower.startsWith("/javascript:") || lower.includes("javascript:")) {
    return false;
  }
  if (lower.startsWith("/data:") || lower.includes("data:")) {
    return false;
  }
  // Disallow control characters / whitespace in path.
  if (/[\u0000-\u001F\u007F\s]/.test(trimmed)) {
    return false;
  }
  return true;
}

/** Returns a safe path or the fallback (default `/account`). */
export function resolveSafeReturnTo(
  value: string | null | undefined,
  fallback: string = "/account",
): string {
  if (isSafeReturnTo(value)) {
    return value;
  }
  return isSafeReturnTo(fallback) ? fallback : "/account";
}
