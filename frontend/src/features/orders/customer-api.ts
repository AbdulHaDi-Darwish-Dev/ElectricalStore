import { authenticatedFetch } from "@/lib/auth";
import type { OrderDto } from "./types";

export type OrderListItemDto = {
  id: string;
  orderNumber: string;
  status: string;
  paymentStatus: string;
  customerName: string;
  phone: string;
  total: number;
  createdAtUtc: string;
};

/** GET /orders — authenticated customer list. */
export function listMyOrders(): Promise<OrderListItemDto[]> {
  return authenticatedFetch<OrderListItemDto[]>("/orders", {
    method: "GET",
    cache: "no-store",
  });
}

/** GET /orders/{id} — authenticated; cross-customer → 404. */
export function getMyOrder(orderId: string): Promise<OrderDto> {
  return authenticatedFetch<OrderDto>(`/orders/${orderId}`, {
    method: "GET",
    cache: "no-store",
  });
}
