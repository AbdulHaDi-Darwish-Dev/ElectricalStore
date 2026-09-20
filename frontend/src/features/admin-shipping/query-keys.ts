import { adminQueryKey } from "@/features/admin";

export const adminShippingDomain = "shipping" as const;

export const adminShippingKeys = {
  all: () => adminQueryKey(adminShippingDomain),
  zones: () => adminQueryKey(adminShippingDomain, "zones"),
  lists: () => adminQueryKey(adminShippingDomain, "zones", "list"),
  list: () => adminQueryKey(adminShippingDomain, "zones", "list"),
  details: () => adminQueryKey(adminShippingDomain, "zones", "detail"),
  detail: (id: string) =>
    adminQueryKey(adminShippingDomain, "zones", "detail", id),
} as const;
