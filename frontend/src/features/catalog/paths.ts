import { buildQueryString } from "@/lib/api";
import type { CatalogProductListParams } from "./types";

/**
 * Pure path builder for GET /catalog/products.
 * Exported for unit tests — does not invent unsupported filters.
 */
export function buildCatalogProductsPath(
  params?: CatalogProductListParams,
): string {
  return `/catalog/products${buildQueryString({
    categoryId: params?.categoryId,
    search: params?.search,
  })}`;
}
