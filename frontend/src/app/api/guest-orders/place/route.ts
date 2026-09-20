import { NextResponse } from "next/server";
import { getServerApiBaseUrl, joinApiUrl } from "@/lib/api/config";
import { isProblemDetails } from "@/lib/api/problem-details";
import {
  guestOrderCookieName,
  guestOrderCookieOptions,
} from "@/lib/security/guest-order-cookie";
import {
  isAllowedBrowserOrigin,
  isJsonContentType,
  PRIVATE_NO_STORE_HEADERS,
} from "@/lib/security/same-origin";
import {
  IDEMPOTENCY_KEY_HEADER,
  type OrderDto,
  type PlaceOrderRequest,
} from "@/features/orders/types";
import { toClientSafeOrderDto } from "@/features/orders/safe-dto";

/**
 * Security-only Place Order boundary (guest or authenticated).
 * Same-origin Origin check + Json content-type; optional Bearer forward;
 * captures guestAccessToken into HttpOnly cookie only when present.
 * No pricing / stock / minimum-order business logic.
 */
export async function POST(request: Request) {
  if (!isAllowedBrowserOrigin(request.headers.get("origin"))) {
    return NextResponse.json(
      {
        status: 403,
        title: "Forbidden",
        code: "Forbidden",
        detail: "Cross-site Place Order is not allowed.",
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

  const idempotencyKey = request.headers.get(IDEMPOTENCY_KEY_HEADER);
  if (!idempotencyKey || idempotencyKey.trim().length < 16) {
    return NextResponse.json(
      {
        status: 400,
        title: "Ordering.IdempotencyKeyRequired",
        code: "Ordering.IdempotencyKeyRequired",
        detail: "Idempotency-Key header is required.",
      },
      { status: 400, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  let body: PlaceOrderRequest;
  try {
    body = (await request.json()) as PlaceOrderRequest;
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

  const upstreamHeaders: Record<string, string> = {
    "Content-Type": "application/json",
    Accept: "application/json",
    [IDEMPOTENCY_KEY_HEADER]: idempotencyKey.trim(),
  };

  // Forward browser Bearer for authenticated placement. Never log or persist.
  const authorization = request.headers.get("authorization");
  if (authorization?.toLowerCase().startsWith("bearer ")) {
    upstreamHeaders.Authorization = authorization;
  }

  const upstream = await fetch(joinApiUrl("/orders", getServerApiBaseUrl()), {
    method: "POST",
    headers: upstreamHeaders,
    body: JSON.stringify(body),
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

  if (!upstream.ok) {
    const problem = isProblemDetails(parsed) ? parsed : undefined;
    return NextResponse.json(
      problem ?? {
        status: upstream.status,
        title: "UpstreamError",
        code: "UpstreamError",
        detail: "تعذر إتمام الطلب.",
      },
      { status: upstream.status, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const order = parsed as OrderDto;
  const token = typeof order.guestAccessToken === "string" ? order.guestAccessToken : null;
  const safeOrder = toClientSafeOrderDto(order);

  const response = NextResponse.json(safeOrder, {
    status: 201,
    headers: PRIVATE_NO_STORE_HEADERS,
  });

  if (token && order.id) {
    response.cookies.set(
      guestOrderCookieName(order.id),
      token,
      guestOrderCookieOptions(order.id),
    );
  }

  return response;
}
