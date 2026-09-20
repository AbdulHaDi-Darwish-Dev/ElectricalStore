/**
 * API base URL resolution.
 * Server Components / Route Handlers prefer API_BASE_URL;
 * the browser uses NEXT_PUBLIC_API_BASE_URL.
 */
export function getApiBaseUrl(): string {
  if (typeof window === "undefined") {
    return (
      process.env.API_BASE_URL ??
      process.env.NEXT_PUBLIC_API_BASE_URL ??
      "http://localhost:5080"
    );
  }

  return process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";
}

export function joinApiUrl(path: string): string {
  const base = getApiBaseUrl().replace(/\/+$/, "");
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${base}${normalized}`;
}
