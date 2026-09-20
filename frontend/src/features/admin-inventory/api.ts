import { authenticatedFetch } from "@/lib/auth";
import type {
  AdjustInventoryRequest,
  AdminInventoryItemDto,
  AdminInventoryListParams,
} from "./types";

const BASE = "/admin/inventory";

function buildListQuery(params: AdminInventoryListParams): string {
  const qs = new URLSearchParams();
  if (params.productId) qs.set("productId", params.productId);
  if (params.categoryId) qs.set("categoryId", params.categoryId);
  if (params.search?.trim()) qs.set("search", params.search.trim());
  if (params.inStock !== undefined) qs.set("inStock", String(params.inStock));
  const q = qs.toString();
  return q ? `${BASE}?${q}` : BASE;
}

export async function listAdminInventory(
  params: AdminInventoryListParams = {},
  signal?: AbortSignal,
): Promise<AdminInventoryItemDto[]> {
  return authenticatedFetch<AdminInventoryItemDto[]>(buildListQuery(params), {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function getAdminInventoryByVariant(
  variantId: string,
  signal?: AbortSignal,
): Promise<AdminInventoryItemDto> {
  return authenticatedFetch<AdminInventoryItemDto>(`${BASE}/${variantId}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function adjustAdminInventory(
  variantId: string,
  body: AdjustInventoryRequest,
): Promise<AdminInventoryItemDto> {
  return authenticatedFetch<AdminInventoryItemDto>(
    `${BASE}/${variantId}/adjust`,
    {
      method: "POST",
      body,
    },
  );
}
