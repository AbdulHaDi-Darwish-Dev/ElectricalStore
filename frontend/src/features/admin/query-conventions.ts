/**
 * Admin TanStack Query conventions (F7 foundation — no fake queries yet).
 *
 * - Feature-owned API modules later (do not create a giant adminApi.ts).
 * - Query keys: ["admin", domain, ...params] e.g. ["admin", "orders", orderId]
 * - Mutations invalidate the matching domain prefix.
 * - Prefer short staleTime for Inventory/Orders when those features arrive.
 * - Do not change global QueryClient defaults only for Admin.
 * - Never persist Admin query cache to localStorage.
 * - Normalize errors via ApiError (status + code); map 403 to Access Denied UX.
 * - 401 uses existing authenticatedFetch refresh; 403 must not refresh-loop.
 */

export const ADMIN_QUERY_ROOT = "admin" as const;

export function adminQueryKey(
  domain: string,
  ...parts: Array<string | number | boolean | null | undefined>
): unknown[] {
  return [ADMIN_QUERY_ROOT, domain, ...parts.filter((p) => p !== undefined)];
}

/** Suggested defaults for future operational Admin queries (override per feature). */
export const adminOperationalQueryDefaults = {
  staleTime: 15_000,
  gcTime: 60_000,
  refetchOnWindowFocus: true,
  retry: 1,
} as const;
