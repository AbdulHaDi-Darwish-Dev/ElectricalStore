"use client";

import { apiFetch, ApiError, type ApiFetchOptions } from "@/lib/api";
import { coordinatedRefresh } from "./auth-api";
import { useAuthStore } from "./session-store";

export type AuthenticatedFetchOptions = Omit<ApiFetchOptions, "accessToken"> & {
  /** When false, skip 401→refresh→retry (default true). */
  retryOnUnauthorized?: boolean;
};

/**
 * Browser API call with Bearer access token, single coordinated refresh on 401, one retry.
 * Does not refresh on 403/404/409.
 */
export async function authenticatedFetch<T = unknown>(
  path: string,
  options: AuthenticatedFetchOptions = {},
): Promise<T> {
  const { retryOnUnauthorized = true, ...fetchOptions } = options;
  const token = useAuthStore.getState().accessToken;

  try {
    return await apiFetch<T>(path, {
      ...fetchOptions,
      accessToken: token,
    });
  } catch (error) {
    if (
      !(error instanceof ApiError) ||
      error.status !== 401 ||
      !retryOnUnauthorized
    ) {
      throw error;
    }

    const refreshed = await coordinatedRefresh();
    if (!refreshed) {
      useAuthStore.getState().clearSession();
      throw error;
    }

    useAuthStore.getState().applyTokens(refreshed);

    return apiFetch<T>(path, {
      ...fetchOptions,
      accessToken: refreshed.accessToken,
    });
  }
}
