import { apiFetch } from "@/lib/api";
import { buildCatalogProductsPath } from "./paths";
import type {
  CatalogProductDto,
  CatalogProductListItemDto,
  CatalogProductListParams,
  CategoryDto,
} from "./types";

/** Default revalidation for public catalog GETs (seconds). */
const CATALOG_REVALIDATE_SECONDS = 60;

/**
 * GET /catalog/categories — active categories with images (public catalog).
 * Intended for Server Components / server rendering.
 */
export function getCategories(): Promise<CategoryDto[]> {
  return apiFetch<CategoryDto[]>("/catalog/categories", {
    next: { revalidate: CATALOG_REVALIDATE_SECONDS },
  });
}

/**
 * GET /catalog/categories/{id}
 * Throws ApiError on failure (including 404). Callers decide UI handling.
 */
export function getCategory(id: string): Promise<CategoryDto> {
  return apiFetch<CategoryDto>(`/catalog/categories/${id}`, {
    next: { revalidate: CATALOG_REVALIDATE_SECONDS },
  });
}

/**
 * GET /catalog/products?categoryId=&search=
 * Supports only backend query parameters.
 */
export function getProducts(
  params?: CatalogProductListParams,
): Promise<CatalogProductListItemDto[]> {
  return apiFetch<CatalogProductListItemDto[]>(buildCatalogProductsPath(params), {
    next: { revalidate: CATALOG_REVALIDATE_SECONDS },
  });
}

/**
 * GET /catalog/products/{id}
 * Throws ApiError on failure (including 404).
 */
export function getProduct(id: string): Promise<CatalogProductDto> {
  return apiFetch<CatalogProductDto>(`/catalog/products/${id}`, {
    next: { revalidate: CATALOG_REVALIDATE_SECONDS },
  });
}
