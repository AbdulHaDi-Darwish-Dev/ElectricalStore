import { NextResponse } from "next/server";
import { getServerApiBaseUrl, joinApiUrl } from "@/lib/api/config";
import { isProblemDetails } from "@/lib/api/problem-details";
import { readGuestOrderToken } from "@/lib/security/guest-order-cookie.server";
import { PRIVATE_NO_STORE_HEADERS } from "@/lib/security/same-origin";
import { GUEST_ORDER_TOKEN_HEADER, type OrderDto } from "@/features/orders/types";
import { toClientSafeOrderDto } from "@/features/orders/safe-dto";

type RouteContext = {
  params: Promise<{ id: string }>;
};

/**
 * Security-only guest order retrieval.
 * Reads HttpOnly cookie and forwards X-Order-Token to ASP.NET GET /orders/{id}/track.
 * Response is private PII — never cache.
 */
export async function GET(_request: Request, context: RouteContext) {
  const { id } = await context.params;
  if (!id) {
    return NextResponse.json(
      {
        status: 400,
        title: "InvalidRequest",
        code: "InvalidRequest",
        detail: "Order id is required.",
      },
      { status: 400, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const token = await readGuestOrderToken(id);
  if (!token) {
    return NextResponse.json(
      {
        status: 404,
        title: "Ordering.NotFound",
        code: "Ordering.NotFound",
        detail: "Order credential is missing or expired.",
      },
      { status: 404, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const upstream = await fetch(
    joinApiUrl(`/orders/${id}/track`, getServerApiBaseUrl()),
    {
      method: "GET",
      headers: {
        Accept: "application/json",
        [GUEST_ORDER_TOKEN_HEADER]: token,
      },
      cache: "no-store",
    },
  );

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
        detail: "تعذر تحميل الطلب.",
      },
      { status: upstream.status, headers: PRIVATE_NO_STORE_HEADERS },
    );
  }

  const order = parsed as OrderDto;
  const safeOrder = toClientSafeOrderDto(order);

  return NextResponse.json(safeOrder, {
    status: 200,
    headers: PRIVATE_NO_STORE_HEADERS,
  });
}
