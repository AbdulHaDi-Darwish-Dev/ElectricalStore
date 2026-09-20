import { apiFetch } from "@/lib/api";
import type { RegisterRequest, RegisterResponseDto } from "@/lib/auth";

/** POST /auth/register — public; does not create a session. */
export function registerAccount(
  request: RegisterRequest,
): Promise<RegisterResponseDto> {
  return apiFetch<RegisterResponseDto>("/auth/register", {
    method: "POST",
    body: request,
    cache: "no-store",
  });
}
