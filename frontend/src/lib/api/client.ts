import { joinApiUrl } from "./config";
import { ApiError, isProblemDetails, type ProblemDetails } from "./problem-details";

export type ApiFetchOptions = {
  method?: string;
  headers?: HeadersInit;
  /** JSON-serializable body (objects/arrays) or raw BodyInit. */
  body?: unknown;
  signal?: AbortSignal;
  /** Optional Bearer token for later auth phases. Unused by public F2 APIs. */
  accessToken?: string | null;
  cache?: RequestCache;
  next?: NextFetchRequestConfig;
  /** Override base URL (tests / explicit server|browser callers). */
  baseUrl?: string;
};

/**
 * Minimal native-fetch wrapper for ASP.NET Core.
 * No SDK, no Axios, no auth refresh.
 */
export async function apiFetch<T = unknown>(
  path: string,
  options: ApiFetchOptions = {},
): Promise<T> {
  const {
    method = "GET",
    headers: initHeaders,
    body,
    signal,
    accessToken,
    cache,
    next,
    baseUrl,
  } = options;

  const headers = new Headers(initHeaders);

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let requestBody: BodyInit | undefined;
  if (body !== undefined && body !== null) {
    if (
      typeof body === "string" ||
      body instanceof FormData ||
      body instanceof Blob ||
      body instanceof ArrayBuffer ||
      ArrayBuffer.isView(body) ||
      body instanceof URLSearchParams
    ) {
      requestBody = body as BodyInit;
    } else {
      if (!headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
      }
      requestBody = JSON.stringify(body);
    }
  }

  const response = await fetch(joinApiUrl(path, baseUrl), {
    method,
    headers,
    body: requestBody,
    signal,
    cache,
    next,
  });

  if (response.status === 204 || response.status === 205) {
    return undefined as T;
  }

  const raw = await response.text();
  if (!raw) {
    if (!response.ok) {
      throw new ApiError(response.status, undefined, `طلب فشل بحالة ${response.status}`);
    }
    return undefined as T;
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw) as unknown;
  } catch {
    if (!response.ok) {
      throw new ApiError(response.status, undefined, raw.slice(0, 200));
    }
    throw new ApiError(response.status, undefined, "تعذر قراءة استجابة JSON.");
  }

  if (!response.ok) {
    const problem = isProblemDetails(parsed) ? (parsed as ProblemDetails) : undefined;
    throw new ApiError(response.status, problem);
  }

  return parsed as T;
}
