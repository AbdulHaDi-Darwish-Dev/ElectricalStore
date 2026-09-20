import type { OrderDto } from "@/features/orders/types";

/** Strip raw guest credential before any browser-visible JSON. */
export function toClientSafeOrderDto(order: OrderDto): OrderDto {
  return {
    ...order,
    guestAccessToken: null,
  };
}
