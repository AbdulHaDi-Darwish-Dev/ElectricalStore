import { adminQueryKey } from "@/features/admin";
import type { AdminProductListParams } from "./types";

export const adminProductsDomain = "products" as const;

export const adminProductKeys = {
  all: () => adminQueryKey(adminProductsDomain),
  lists: () => adminQueryKey(adminProductsDomain, "list"),
  list: (params: AdminProductListParams = {}) =>
    adminQueryKey(
      adminProductsDomain,
      "list",
      params.categoryId ?? null,
      params.isActive ?? null,
      params.search ?? null,
    ),
  details: () => adminQueryKey(adminProductsDomain, "detail"),
  detail: (id: string) => adminQueryKey(adminProductsDomain, "detail", id),
} as const;
