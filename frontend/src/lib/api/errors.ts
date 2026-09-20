import { ApiError } from "@/lib/api";

/** True when an API failure should map to Next.js `notFound()`. */
export function isNotFoundError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404;
}
