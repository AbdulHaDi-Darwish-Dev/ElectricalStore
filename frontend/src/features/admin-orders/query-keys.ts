import { adminQueryKey } from "@/features/admin";
import type { AdminOrderListParams } from "./types";

export const adminOrdersDomain = "orders" as const;

export const adminOrderKeys = {
  all: () => adminQueryKey(adminOrdersDomain),
  lists: () => adminQueryKey(adminOrdersDomain, "list"),
  list: (params: AdminOrderListParams = {}) =>
    adminQueryKey(
      adminOrdersDomain,
      "list",
      params.status ?? null,
      params.paymentStatus ?? null,
      params.search ?? null,
      params.createdFromUtc ?? null,
      params.createdToUtc ?? null,
    ),
  details: () => adminQueryKey(adminOrdersDomain, "detail"),
  detail: (id: string) => adminQueryKey(adminOrdersDomain, "detail", id),
} as const;
