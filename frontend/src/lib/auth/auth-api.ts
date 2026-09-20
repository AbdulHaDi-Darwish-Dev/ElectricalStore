"use client";

import { ApiError, isProblemDetails } from "@/lib/api";
import type { BrowserAuthTokensDto, BrowserLoginResponseDto, MeDto } from "./types";

const AUTH_CHANNEL = "electricalstore.auth";
const REFRESH_LOCK = "electricalstore.auth.refresh";

type AuthChannelMessage =
  | { type: "logout" }
  | { type: "session"; tokens: BrowserAuthTokensDto; me: MeDto };

let refreshInFlight: Promise<BrowserAuthTokensDto | null> | null = null;

function sameOriginHeaders(): HeadersInit {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
  };
}

async function parseJson(response: Response): Promise<unknown> {
  const raw = await response.text();
  if (!raw) return undefined;
  try {
    return JSON.parse(raw) as unknown;
  } catch {
    throw new ApiError(response.status, undefined, "تعذر قراءة استجابة JSON.");
  }
}

function throwIfNotOk(response: Response, parsed: unknown): void {
  if (response.ok) return;
  const problem = isProblemDetails(parsed) ? parsed : undefined;
  throw new ApiError(response.status, problem);
}

/** Low-level refresh against Next BFF (no store mutation). */
async function postRefresh(): Promise<BrowserAuthTokensDto | null> {
  const response = await fetch("/api/auth/refresh", {
    method: "POST",
    headers: sameOriginHeaders(),
    credentials: "same-origin",
    cache: "no-store",
  });

  const parsed = await parseJson(response);

  if (response.status === 401) {
    return null;
  }

  throwIfNotOk(response, parsed);

  if (
    typeof parsed === "object" &&
    parsed !== null &&
    (parsed as { kind?: string }).kind === "authenticated" &&
    typeof (parsed as BrowserAuthTokensDto).accessToken === "string"
  ) {
    const body = parsed as BrowserAuthTokensDto & { kind: string };
    return {
      userId: body.userId,
      accessToken: body.accessToken,
      accessTokenExpiresAtUtc: body.accessTokenExpiresAtUtc,
    };
  }

  return null;
}

async function withRefreshLock<T>(run: () => Promise<T>): Promise<T> {
  const locks = globalThis.navigator?.locks;
  if (locks?.request) {
    return locks.request(REFRESH_LOCK, run);
  }
  return run();
}

/**
 * Single-flight refresh (same tab) + Web Locks (cross-tab when available).
 * Fallback without locks: in-tab Promise only — document multi-tab risk.
 */
export function coordinatedRefresh(): Promise<BrowserAuthTokensDto | null> {
  if (refreshInFlight) {
    return refreshInFlight;
  }

  refreshInFlight = withRefreshLock(() => postRefresh()).finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

/** Test seam: whether a refresh is currently in flight. */
export function hasRefreshInFlight(): boolean {
  return refreshInFlight !== null;
}

export async function postLogin(
  emailOrUserName: string,
  password: string,
): Promise<BrowserLoginResponseDto> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: sameOriginHeaders(),
    credentials: "same-origin",
    cache: "no-store",
    body: JSON.stringify({ emailOrUserName, password }),
  });

  const parsed = await parseJson(response);
  throwIfNotOk(response, parsed);

  if (
    typeof parsed === "object" &&
    parsed !== null &&
    (parsed as { kind?: string }).kind === "mfaRequired"
  ) {
    return {
      kind: "mfaRequired",
      expiresAtUtc:
        typeof (parsed as { expiresAtUtc?: string }).expiresAtUtc === "string"
          ? (parsed as { expiresAtUtc: string }).expiresAtUtc
          : undefined,
    };
  }

  if (
    typeof parsed === "object" &&
    parsed !== null &&
    (parsed as { kind?: string }).kind === "authenticated"
  ) {
    const body = parsed as BrowserAuthTokensDto & { kind: string };
    return {
      kind: "authenticated",
      userId: body.userId,
      accessToken: body.accessToken,
      accessTokenExpiresAtUtc: body.accessTokenExpiresAtUtc,
    };
  }

  throw new ApiError(502, undefined, "استجابة تسجيل دخول غير متوقعة.");
}

export async function postLogout(): Promise<void> {
  try {
    await fetch("/api/auth/logout", {
      method: "POST",
      headers: sameOriginHeaders(),
      credentials: "same-origin",
      cache: "no-store",
      body: "{}",
    });
  } catch {
    // Local clear still proceeds.
  }
}

export async function fetchMe(accessToken: string): Promise<MeDto> {
  const { apiFetch } = await import("@/lib/api/client");
  return apiFetch<MeDto>("/me", {
    method: "GET",
    accessToken,
    cache: "no-store",
  });
}

let channel: BroadcastChannel | null = null;

export function getAuthBroadcastChannel(): BroadcastChannel | null {
  if (typeof BroadcastChannel === "undefined") {
    return null;
  }
  if (!channel) {
    channel = new BroadcastChannel(AUTH_CHANNEL);
  }
  return channel;
}

export function broadcastLogout(): void {
  getAuthBroadcastChannel()?.postMessage({ type: "logout" } satisfies AuthChannelMessage);
}

export function broadcastSession(tokens: BrowserAuthTokensDto, me: MeDto): void {
  getAuthBroadcastChannel()?.postMessage({
    type: "session",
    tokens,
    me,
  } satisfies AuthChannelMessage);
}

export type { AuthChannelMessage };
