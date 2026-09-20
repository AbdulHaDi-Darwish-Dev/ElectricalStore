import { NextResponse } from "next/server";
import { getServerApiBaseUrl, joinApiUrl } from "@/lib/api/config";
import {
  AUTH_REFRESH_COOKIE_NAME,
  clearAuthRefreshCookieOptions,
} from "@/lib/auth/refresh-cookie";
import {
  isAllowedBrowserOrigin,
  PRIVATE_NO_STORE_HEADERS,
} from "@/lib/security/same-origin";

/**
 * POST /api/auth/logout — revoke refresh family when possible; always clear cookie.
 * Local session cleanup proceeds even if upstream already considers the token invalid.
 */
export async function POST(request: Request) {
  if (!isAllowedBrowserOrigin(request.headers.get("origin"))) {
    return NextResponse.json(
      {
        status: 403,
        title: "Forbidden",
        code: "Forbidden",
        detail: "Cross-site logout is not allowed.",
      },
      { status: 403, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const cookieHeader = request.headers.get("cookie") ?? "";
  const refreshToken = readCookie(cookieHeader, AUTH_REFRESH_COOKIE_NAME);

  if (refreshToken) {
    try {
      await fetch(joinApiUrl("/auth/logout", getServerApiBaseUrl()), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({ refreshToken }),
        cache: "no-store",
      });
    } catch {
      // Cookie still cleared below — local logout is fail-open for UX safety.
    }
  }

  const response = NextResponse.json(
    { ok: true },
    { status: 200, headers: PRIVATE_NO_STORE_HEADERS },
  );
  response.cookies.set(
    AUTH_REFRESH_COOKIE_NAME,
    "",
    clearAuthRefreshCookieOptions(),
  );
  return response;
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
