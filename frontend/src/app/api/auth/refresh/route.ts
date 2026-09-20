import { NextResponse } from "next/server";
import { getServerApiBaseUrl, joinApiUrl } from "@/lib/api/config";
import { isProblemDetails } from "@/lib/api/problem-details";
import {
  AUTH_REFRESH_COOKIE_NAME,
  authRefreshCookieOptions,
  clearAuthRefreshCookieOptions,
} from "@/lib/auth/refresh-cookie";
import {
  isAuthenticationResult,
  toBrowserAuthTokens,
} from "@/lib/auth/types";
import {
  isAllowedBrowserOrigin,
  PRIVATE_NO_STORE_HEADERS,
} from "@/lib/security/same-origin";

/**
 * POST /api/auth/refresh — rotate refresh cookie; return access token only.
 * Same-origin required (bootstrap + 401 recovery are same-origin browser calls).
 */
export async function POST(request: Request) {
  if (!isAllowedBrowserOrigin(request.headers.get("origin"))) {
    return NextResponse.json(
      {
        status: 403,
        title: "Forbidden",
        code: "Forbidden",
        detail: "Cross-site refresh is not allowed.",
      },
      { status: 403, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const cookieHeader = request.headers.get("cookie") ?? "";
  const refreshToken = readCookie(cookieHeader, AUTH_REFRESH_COOKIE_NAME);

  if (!refreshToken) {
    const response = NextResponse.json(
      {
        status: 401,
        title: "Unauthorized",
        code: "Auth.NoRefreshSession",
        detail: "No refresh session.",
      },
      { status: 401, headers: PRIVATE_NO_STORE_HEADERS },
    );
    response.cookies.set(
      AUTH_REFRESH_COOKIE_NAME,
      "",
      clearAuthRefreshCookieOptions(),
    );
    return response;
  }

  const upstream = await fetch(
    joinApiUrl("/auth/refresh", getServerApiBaseUrl()),
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    },
  );

  const raw = await upstream.text();
  let parsed: unknown = undefined;
  if (raw) {
    try {
      parsed = JSON.parse(raw) as unknown;
    } catch {
      const response = NextResponse.json(
        {
          status: upstream.status,
          title: "UpstreamError",
          code: "UpstreamError",
          detail: "تعذر قراءة استجابة التحديث.",
        },
        { status: upstream.status || 502, headers: PRIVATE_NO_STORE_HEADERS },
      );
      clearRefreshCookie(response);
      return response;
    }
  }

  if (!upstream.ok || !isAuthenticationResult(parsed)) {
    const problem = isProblemDetails(parsed) ? parsed : undefined;
    const response = NextResponse.json(
      problem ?? {
        status: upstream.status || 401,
        title: "Unauthorized",
        code: "Auth.RefreshFailed",
        detail: "تعذر تحديث الجلسة.",
      },
      { status: upstream.status || 401, headers: PRIVATE_NO_STORE_HEADERS },
    );
    clearRefreshCookie(response);
    return response;
  }

  const response = NextResponse.json(
    { kind: "authenticated" as const, ...toBrowserAuthTokens(parsed) },
    { status: 200, headers: PRIVATE_NO_STORE_HEADERS },
  );
  response.cookies.set(
    AUTH_REFRESH_COOKIE_NAME,
    parsed.refreshToken,
    authRefreshCookieOptions({
      expiresAtUtc: parsed.refreshTokenExpiresAtUtc,
    }),
  );
  return response;
}

function clearRefreshCookie(response: NextResponse) {
  response.cookies.set(
    AUTH_REFRESH_COOKIE_NAME,
    "",
    clearAuthRefreshCookieOptions(),
  );
}

function readCookie(cookieHeader: string, name: string): string | null {
  const parts = cookieHeader.split(";");
  for (const part of parts) {
    const idx = part.indexOf("=");
    if (idx < 0) continue;
    const key = part.slice(0, idx).trim();
    if (key !== name) continue;
    return decodeURIComponent(part.slice(idx + 1).trim());
  }
  return null;
}
