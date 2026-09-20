import { NextResponse } from "next/server";
import { getServerApiBaseUrl, joinApiUrl } from "@/lib/api/config";
import { isProblemDetails } from "@/lib/api/problem-details";
import { resolveLoginClientIp } from "@/lib/auth/client-ip";
import {
  AUTH_REFRESH_COOKIE_NAME,
  authRefreshCookieOptions,
} from "@/lib/auth/refresh-cookie";
import {
  isAuthenticationResult,
  isMfaLoginChallenge,
  toBrowserAuthTokens,
  type LoginRequest,
} from "@/lib/auth/types";
import {
  isAllowedBrowserOrigin,
  isJsonContentType,
  PRIVATE_NO_STORE_HEADERS,
} from "@/lib/security/same-origin";

/**
 * POST /api/auth/login — same-origin BFF.
 * Stores refreshToken in HttpOnly cookie; strips it from browser JSON.
 */
export async function POST(request: Request) {
  if (!isAllowedBrowserOrigin(request.headers.get("origin"))) {
    return NextResponse.json(
      {
        status: 403,
        title: "Forbidden",
        code: "Forbidden",
        detail: "Cross-site login is not allowed.",
      },
      { status: 403, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  if (!isJsonContentType(request.headers.get("content-type"))) {
    return NextResponse.json(
      {
        status: 415,
        title: "UnsupportedMediaType",
        code: "UnsupportedMediaType",
        detail: "Content-Type must be application/json.",
      },
      { status: 415, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  let body: LoginRequest;
  try {
    body = (await request.json()) as LoginRequest;
  } catch {
    return NextResponse.json(
      {
        status: 400,
        title: "InvalidRequest",
        code: "InvalidRequest",
        detail: "Invalid JSON body.",
      },
      { status: 400, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  // Production: Nginx-overwritten X-ElectricalStore-Client-Ip only.
  // Local: loopback identity — never from browser forwarding headers.
  let clientIp: string;
  try {
    clientIp = resolveLoginClientIp(request);
  } catch {
    return NextResponse.json(
      {
        status: 503,
        title: "ServiceUnavailable",
        code: "TrustedClientIpUnavailable",
        detail: "تعذر تحديد عنوان العميل الموثوق لتسجيل الدخول.",
      },
      { status: 503, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const upstream = await fetch(joinApiUrl("/auth/login", getServerApiBaseUrl()), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      "X-Forwarded-For": clientIp,
    },
    body: JSON.stringify({
      emailOrUserName: body.emailOrUserName,
      password: body.password,
    }),
    cache: "no-store",
  });

  const raw = await upstream.text();
  let parsed: unknown = undefined;
  if (raw) {
    try {
      parsed = JSON.parse(raw) as unknown;
    } catch {
      return NextResponse.json(
        {
          status: upstream.status,
          title: "UpstreamError",
          code: "UpstreamError",
          detail: "تعذر قراءة استجابة الخادم.",
        },
        { status: upstream.status || 502, headers: PRIVATE_NO_STORE_HEADERS },
      );
    }
  }

  const responseHeaders = new Headers(PRIVATE_NO_STORE_HEADERS);
  const retryAfter = upstream.headers.get("Retry-After");
  if (retryAfter) {
    responseHeaders.set("Retry-After", retryAfter);
  }

  if (!upstream.ok) {
    const problem = isProblemDetails(parsed) ? parsed : undefined;
    return NextResponse.json(
      problem ?? {
        status: upstream.status,
        title: "UpstreamError",
        code: "UpstreamError",
        detail: "تعذر تسجيل الدخول.",
      },
      { status: upstream.status, headers: responseHeaders },
    );
  }

  if (isAuthenticationResult(parsed)) {
    const browserBody = {
      kind: "authenticated" as const,
      ...toBrowserAuthTokens(parsed),
    };
    const response = NextResponse.json(browserBody, {
      status: 200,
      headers: responseHeaders,
    });
    response.cookies.set(
      AUTH_REFRESH_COOKIE_NAME,
      parsed.refreshToken,
      authRefreshCookieOptions({
        expiresAtUtc: parsed.refreshTokenExpiresAtUtc,
      }),
    );
    return response;
  }

  if (isMfaLoginChallenge(parsed)) {
    // Do not store refresh cookie; do not expose mfaProof to the browser.
    return NextResponse.json(
      {
        kind: "mfaRequired" as const,
        expiresAtUtc: parsed.expiresAtUtc,
      },
      { status: 200, headers: responseHeaders },
    );
  }

  return NextResponse.json(
    {
      status: 502,
      title: "UpstreamError",
      code: "UpstreamError",
      detail: "استجابة تسجيل دخول غير متوقعة.",
    },
    { status: 502, headers: responseHeaders },
  );
}
