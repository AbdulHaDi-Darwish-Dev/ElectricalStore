import { authenticatedFetch } from "@/lib/auth";
import type {
  AdminCategoryDto,
  CreateAdminCategoryRequest,
  UpdateAdminCategoryRequest,
} from "./types";

const BASE = "/admin/categories";

export async function listAdminCategories(
  signal?: AbortSignal,
): Promise<AdminCategoryDto[]> {
  return authenticatedFetch<AdminCategoryDto[]>(BASE, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function getAdminCategory(
  id: string,
  signal?: AbortSignal,
): Promise<AdminCategoryDto> {
  return authenticatedFetch<AdminCategoryDto>(`${BASE}/${id}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function createAdminCategory(
  body: CreateAdminCategoryRequest,
): Promise<AdminCategoryDto> {
  return authenticatedFetch<AdminCategoryDto>(BASE, {
    method: "POST",
    body,
  });
}

export async function updateAdminCategory(
  id: string,
  body: UpdateAdminCategoryRequest,
): Promise<AdminCategoryDto> {
  return authenticatedFetch<AdminCategoryDto>(`${BASE}/${id}`, {
    method: "PUT",
    body,
  });
}

export async function activateAdminCategory(
  id: string,
): Promise<AdminCategoryDto> {
  return authenticatedFetch<AdminCategoryDto>(`${BASE}/${id}/activate`, {
    method: "POST",
  });
}

export async function deactivateAdminCategory(
  id: string,
): Promise<AdminCategoryDto> {
  return authenticatedFetch<AdminCategoryDto>(`${BASE}/${id}/deactivate`, {
    method: "POST",
  });
}

/**
 * Multipart upload. Field name must be `file` (ASP.NET IFormFile file).
 * Do not set Content-Type manually — browser sets multipart boundary.
 */
export async function upsertAdminCategoryImage(
  id: string,
  file: File,
): Promise<AdminCategoryDto> {
  const formData = new FormData();
  formData.append("file", file, file.name);
  return authenticatedFetch<AdminCategoryDto>(`${BASE}/${id}/image`, {
    method: "PUT",
    body: formData,
  });
}

export async function deleteAdminCategoryImage(
  id: string,
): Promise<AdminCategoryDto> {
  return authenticatedFetch<AdminCategoryDto>(`${BASE}/${id}/image`, {
    method: "DELETE",
  });
}
