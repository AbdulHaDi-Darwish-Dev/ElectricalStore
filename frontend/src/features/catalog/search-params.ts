import type { CatalogProductListParams } from "@/features/catalog";

/**
 * Parse storefront product listing URL search params.
 * Only backend-supported filters: categoryId, search.
 */
export function parseProductListSearchParams(
  searchParams: Record<string, string | string[] | undefined>,
): CatalogProductListParams {
  const params: CatalogProductListParams = {};

  const categoryId = firstParam(searchParams.categoryId)?.trim();
  const search = firstParam(searchParams.search)?.trim();

  if (categoryId) {
    params.categoryId = categoryId;
  }
  if (search) {
    params.search = search;
  }

  return params;
}

function firstParam(value: string | string[] | undefined): string | undefined {
  if (Array.isArray(value)) {
    return value[0];
  }
  return value;
}

/** Build a relative products listing href from filter state. */
export function buildProductsHref(params?: CatalogProductListParams): string {
  const query = new URLSearchParams();
  if (params?.categoryId) {
    query.set("categoryId", params.categoryId);
  }
  if (params?.search) {
    query.set("search", params.search);
  }
  const qs = query.toString();
  return qs ? `/products?${qs}` : "/products";
}
