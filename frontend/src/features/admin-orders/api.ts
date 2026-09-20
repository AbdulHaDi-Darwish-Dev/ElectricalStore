import { authenticatedFetch } from "@/lib/auth";
import type {
  AdminOrderDto,
  AdminOrderListItemDto,
  AdminOrderListParams,
  CancelAdminOrderRequest,
} from "./types";

const BASE = "/admin/orders";

function buildListQuery(params: AdminOrderListParams): string {
  const qs = new URLSearchParams();
  if (params.status) qs.set("status", params.status);
  if (params.paymentStatus) qs.set("paymentStatus", params.paymentStatus);
  if (params.search?.trim()) qs.set("search", params.search.trim());
  if (params.createdFromUtc) qs.set("createdFromUtc", params.createdFromUtc);
  if (params.createdToUtc) qs.set("createdToUtc", params.createdToUtc);
  const q = qs.toString();
  return q ? `${BASE}?${q}` : BASE;
}

export async function listAdminOrders(
  params: AdminOrderListParams = {},
  signal?: AbortSignal,
): Promise<AdminOrderListItemDto[]> {
  return authenticatedFetch<AdminOrderListItemDto[]>(buildListQuery(params), {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function getAdminOrder(
  id: string,
  signal?: AbortSignal,
): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function confirmAdminOrder(id: string): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}/confirm`, {
    method: "POST",
  });
}

export async function prepareAdminOrder(id: string): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}/prepare`, {
    method: "POST",
  });
}

export async function outForDeliveryAdminOrder(
  id: string,
): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}/out-for-delivery`, {
    method: "POST",
  });
}

export async function deliverAdminOrder(id: string): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}/deliver`, {
    method: "POST",
  });
}

export async function markPaidAdminOrder(id: string): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}/mark-paid`, {
    method: "POST",
  });
}

export async function cancelAdminOrder(
  id: string,
  body: CancelAdminOrderRequest,
): Promise<AdminOrderDto> {
  return authenticatedFetch<AdminOrderDto>(`${BASE}/${id}/cancel`, {
    method: "POST",
    body,
  });
}
