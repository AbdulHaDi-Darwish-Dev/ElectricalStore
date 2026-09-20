import {
  CATEGORY_IMAGE_ALLOWED_TYPES,
  CATEGORY_IMAGE_MAX_SIZE_BYTES,
  CATEGORY_IMAGE_MAX_SIZE_MB,
  type CreateAdminCategoryRequest,
  type UpdateAdminCategoryRequest,
} from "./types";
import type { CategoryFormValues } from "./schema";

/** Trim; blank description becomes null for the API. */
export function normalizeOptionalDescription(
  value: string | null | undefined,
): string | null {
  if (value == null) return null;
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

export function toCreateCategoryRequest(
  values: CategoryFormValues,
): CreateAdminCategoryRequest {
  return {
    name: values.name.trim(),
    description: normalizeOptionalDescription(values.description),
    isActive: values.isActive,
  };
}

export function toUpdateCategoryRequest(
  values: Pick<CategoryFormValues, "name" | "description">,
): UpdateAdminCategoryRequest {
  return {
    name: values.name.trim(),
    description: normalizeOptionalDescription(values.description),
  };
}

export type ImageValidationResult =
  | { ok: true }
  | { ok: false; message: string };

export function validateCategoryImageFile(file: File): ImageValidationResult {
  if (!file || file.size <= 0) {
    return { ok: false, message: "اختر ملف صورة صالحاً." };
  }

  if (file.size > CATEGORY_IMAGE_MAX_SIZE_BYTES) {
    return {
      ok: false,
      message: `حجم الصورة أكبر من الحد المسموح (${CATEGORY_IMAGE_MAX_SIZE_MB} ميغابايت).`,
    };
  }

  const type = file.type.trim().toLowerCase();
  if (
    !(CATEGORY_IMAGE_ALLOWED_TYPES as readonly string[]).includes(type)
  ) {
    return {
      ok: false,
      message: "نوع الملف غير مسموح. استخدم JPEG أو PNG أو WebP.",
    };
  }

  return { ok: true };
}

export function categoryImageAcceptAttribute(): string {
  return CATEGORY_IMAGE_ALLOWED_TYPES.join(",");
}
