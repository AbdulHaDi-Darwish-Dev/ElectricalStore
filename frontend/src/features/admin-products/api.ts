import { authenticatedFetch } from "@/lib/auth";
import type {
  AdminProductDto,
  AdminProductListItemDto,
  AdminProductListParams,
  CreateAdminProductRequest,
  CreateAdminProductVariantRequest,
  UpdateAdminProductRequest,
  UpdateAdminProductVariantRequest,
} from "./types";

const BASE = "/admin/products";

function buildListQuery(params: AdminProductListParams): string {
  const qs = new URLSearchParams();
  if (params.categoryId) qs.set("categoryId", params.categoryId);
  if (params.isActive !== undefined) qs.set("isActive", String(params.isActive));
  if (params.search?.trim()) qs.set("search", params.search.trim());
  const q = qs.toString();
  return q ? `${BASE}?${q}` : BASE;
}

export async function listAdminProducts(
  params: AdminProductListParams = {},
  signal?: AbortSignal,
): Promise<AdminProductListItemDto[]> {
  return authenticatedFetch<AdminProductListItemDto[]>(buildListQuery(params), {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function getAdminProduct(
  id: string,
  signal?: AbortSignal,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(`${BASE}/${id}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function createAdminProduct(
  body: CreateAdminProductRequest,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(BASE, {
    method: "POST",
    body,
  });
}

export async function updateAdminProduct(
  id: string,
  body: UpdateAdminProductRequest,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(`${BASE}/${id}`, {
    method: "PUT",
    body,
  });
}

export async function activateAdminProduct(
  id: string,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(`${BASE}/${id}/activate`, {
    method: "POST",
  });
}

export async function deactivateAdminProduct(
  id: string,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(`${BASE}/${id}/deactivate`, {
    method: "POST",
  });
}

export async function addAdminProductVariant(
  productId: string,
  body: CreateAdminProductVariantRequest,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/variants`,
    { method: "POST", body },
  );
}

export async function updateAdminProductVariant(
  productId: string,
  variantId: string,
  body: UpdateAdminProductVariantRequest,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/variants/${variantId}`,
    { method: "PUT", body },
  );
}

export async function activateAdminProductVariant(
  productId: string,
  variantId: string,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/variants/${variantId}/activate`,
    { method: "POST" },
  );
}

export async function deactivateAdminProductVariant(
  productId: string,
  variantId: string,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/variants/${variantId}/deactivate`,
    { method: "POST" },
  );
}

/** Multipart upload — field name `file`. Do not set Content-Type manually. */
export async function addAdminProductImage(
  productId: string,
  file: File,
): Promise<AdminProductDto> {
  const formData = new FormData();
  formData.append("file", file, file.name);
  return authenticatedFetch<AdminProductDto>(`${BASE}/${productId}/images`, {
    method: "POST",
    body: formData,
  });
}

export async function deleteAdminProductImage(
  productId: string,
  imageId: string,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/images/${imageId}`,
    { method: "DELETE" },
  );
}

export async function setPrimaryAdminProductImage(
  productId: string,
  imageId: string,
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/images/${imageId}/primary`,
    { method: "POST" },
  );
}

export async function reorderAdminProductImages(
  productId: string,
  orderedImageIds: string[],
): Promise<AdminProductDto> {
  return authenticatedFetch<AdminProductDto>(
    `${BASE}/${productId}/images/order`,
    {
      method: "PUT",
      body: { orderedImageIds },
    },
  );
}
