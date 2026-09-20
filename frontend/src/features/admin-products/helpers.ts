import type { SellingUnit } from "@/features/catalog";
import type {
  AdminProductDto,
  AdminProductListItemDto,
  CreateAdminProductRequest,
  CreateAdminProductVariantRequest,
  UpdateAdminProductRequest,
  UpdateAdminProductVariantRequest,
} from "./types";
import {
  PRODUCT_IMAGE_ALLOWED_TYPES,
  PRODUCT_IMAGE_MAX_SIZE_BYTES,
  PRODUCT_IMAGE_MAX_SIZE_MB,
  PRODUCT_MAX_IMAGES,
} from "./types";
import type {
  ProductBasicsFormValues,
  ProductCreateFormValues,
  ProductVariantEditFormValues,
  ProductVariantFormValues,
} from "./schema";

export function normalizeOptionalDescription(
  value: string | null | undefined,
): string | null {
  if (value == null) return null;
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

export function toCreateVariantRequest(
  values: ProductVariantFormValues,
): CreateAdminProductVariantRequest {
  return {
    name: values.name.trim(),
    sku: values.sku.trim(),
    price: values.price,
    sellingUnit: values.sellingUnit,
    quantityIncrement: values.quantityIncrement,
    isActive: values.isActive,
  };
}

export function toCreateProductRequest(
  values: ProductCreateFormValues,
): CreateAdminProductRequest {
  return {
    name: values.name.trim(),
    description: normalizeOptionalDescription(values.description),
    categoryId: values.categoryId,
    isActive: values.isActive,
    variants: values.variants.map(toCreateVariantRequest),
  };
}

export function toUpdateProductRequest(
  values: ProductBasicsFormValues,
): UpdateAdminProductRequest {
  return {
    name: values.name.trim(),
    description: normalizeOptionalDescription(values.description),
    categoryId: values.categoryId,
  };
}

export function toUpdateVariantRequest(
  values: ProductVariantEditFormValues,
): UpdateAdminProductVariantRequest {
  return {
    name: values.name.trim(),
    sku: values.sku.trim(),
    price: values.price,
    sellingUnit: values.sellingUnit,
    quantityIncrement: values.quantityIncrement,
  };
}

export type ImageValidationResult =
  | { ok: true }
  | { ok: false; message: string };

export function validateProductImageFile(
  file: File,
  currentImageCount: number,
): ImageValidationResult {
  if (currentImageCount >= PRODUCT_MAX_IMAGES) {
    return {
      ok: false,
      message: `الحد الأقصى لصور المنتج هو ${PRODUCT_MAX_IMAGES} صور.`,
    };
  }
  if (!file || file.size <= 0) {
    return { ok: false, message: "اختر ملف صورة صالحاً." };
  }
  if (file.size > PRODUCT_IMAGE_MAX_SIZE_BYTES) {
    return {
      ok: false,
      message: `حجم الصورة أكبر من الحد المسموح (${PRODUCT_IMAGE_MAX_SIZE_MB} ميغابايت).`,
    };
  }
  const type = file.type.trim().toLowerCase();
  if (!(PRODUCT_IMAGE_ALLOWED_TYPES as readonly string[]).includes(type)) {
    return {
      ok: false,
      message: "نوع الملف غير مسموح. استخدم JPEG أو PNG أو WebP.",
    };
  }
  return { ok: true };
}

export function productImageAcceptAttribute(): string {
  return PRODUCT_IMAGE_ALLOWED_TYPES.join(",");
}

export type ProductReadinessCheck = {
  id: string;
  label: string;
  met: boolean;
};

/**
 * Informational checklist from admin DTO fields only.
 * Not an authorization authority — mirrors known public visibility conditions.
 */
export function getProductReadiness(product: AdminProductDto): {
  checks: ProductReadinessCheck[];
  likelyPublic: boolean;
} {
  const hasActiveVariant = product.variants.some((v) => v.isActive);
  const hasImage = product.images.length > 0;
  const checks: ProductReadinessCheck[] = [
    { id: "active", label: "المنتج فعال", met: product.isActive },
    {
      id: "category",
      label: "التصنيف فعال",
      met: product.categoryIsActive,
    },
    { id: "image", label: "توجد صورة واحدة على الأقل", met: hasImage },
    {
      id: "variant",
      label: "يوجد خيار فعّال واحد على الأقل",
      met: hasActiveVariant,
    },
  ];
  return {
    checks,
    likelyPublic: checks.every((c) => c.met),
  };
}

/** Partial list-level signal — cannot know active-variant or category-active from list DTO alone for certainty. */
export function getListProductSignals(item: AdminProductListItemDto): {
  hasImage: boolean;
  hasVariants: boolean;
} {
  return {
    hasImage: item.imageCount > 0,
    hasVariants: item.variantCount > 0,
  };
}

export function defaultQuantityIncrement(unit: SellingUnit): number {
  return unit === "Piece" ? 1 : 0.5;
}
