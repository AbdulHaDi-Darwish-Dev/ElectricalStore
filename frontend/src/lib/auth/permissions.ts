/**
 * Frontend permission helpers — UX only. ASP.NET remains authoritative.
 * Do not authorize by role name; do not invent roles from permissions.
 */

export function hasPermission(
  permissions: readonly string[] | null | undefined,
  code: string,
): boolean {
  if (!permissions || !code) {
    return false;
  }
  return permissions.includes(code);
}

export function hasAnyPermission(
  permissions: readonly string[] | null | undefined,
  codes: readonly string[],
): boolean {
  if (!permissions || codes.length === 0) {
    return false;
  }
  return codes.some((code) => permissions.includes(code));
}
