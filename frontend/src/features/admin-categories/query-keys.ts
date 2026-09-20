import { adminQueryKey } from "@/features/admin";

export const adminCategoriesDomain = "categories" as const;

export const adminCategoryKeys = {
  all: () => adminQueryKey(adminCategoriesDomain),
  lists: () => adminQueryKey(adminCategoriesDomain, "list"),
  list: () => adminQueryKey(adminCategoriesDomain, "list"),
  details: () => adminQueryKey(adminCategoriesDomain, "detail"),
  detail: (id: string) => adminQueryKey(adminCategoriesDomain, "detail", id),
} as const;
