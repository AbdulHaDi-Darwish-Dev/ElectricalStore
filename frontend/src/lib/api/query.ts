/**
 * Builds a query string from optional string parameters.
 * Omits undefined, null, and empty values. Does not invent unsupported filters.
 */
export function buildQueryString(
  params: Record<string, string | undefined | null>,
): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") {
      continue;
    }
    search.set(key, value);
  }

  const query = search.toString();
  return query.length > 0 ? `?${query}` : "";
}
