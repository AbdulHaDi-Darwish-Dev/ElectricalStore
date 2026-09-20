import { adminQueryKey } from "@/features/admin";
import type { AdminInventoryListParams } from "./types";

export const adminInventoryDomain = "inventory" as const;

export const adminInventoryKeys = {
  all: () => adminQueryKey(adminInventoryDomain),
  lists: () => adminQueryKey(adminInventoryDomain, "list"),
  list: (params: AdminInventoryListParams = {}) =>
    adminQueryKey(
      adminInventoryDomain,
      "list",
      params.productId ?? null,
      params.categoryId ?? null,
      params.search ?? null,
      params.inStock ?? null,
    ),
  details: () => adminQueryKey(adminInventoryDomain, "detail"),
  detail: (variantId: string) =>
    adminQueryKey(adminInventoryDomain, "detail", variantId),
} as const;
