import type { SellingUnit } from "@/features/catalog";

/** Admin ProductListItemDto */
export type AdminProductListItemDto = {
  id: string;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  isActive: boolean;
  variantCount: number;
  primaryImageUrl: string | null;
  imageCount: number;
};

/** Admin ProductVariantDto */
export type AdminProductVariantDto = {
  id: string;
  name: string;
  sku: string;
  price: number;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  isActive: boolean;
};

/** Admin ProductImageDto */
export type AdminProductImageDto = {
  id: string;
  url: string;
  isPrimary: boolean;
  sortOrder: number;
};

/** Admin ProductDto */
export type AdminProductDto = {
  id: string;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  categoryIsActive: boolean;
  isActive: boolean;
  variants: AdminProductVariantDto[];
  images: AdminProductImageDto[];
};

export type CreateAdminProductVariantRequest = {
  name: string;
  sku: string;
  price: number;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  isActive: boolean;
};

export type CreateAdminProductRequest = {
  name: string;
  description?: string | null;
  categoryId: string;
  isActive: boolean;
  variants: CreateAdminProductVariantRequest[];
};

export type UpdateAdminProductRequest = {
  name: string;
  description?: string | null;
  categoryId: string;
};

export type UpdateAdminProductVariantRequest = {
  name: string;
  sku: string;
  price: number;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
};

export type AdminProductListParams = {
  categoryId?: string;
  isActive?: boolean;
  search?: string;
};

export const PRODUCT_NAME_MAX_LENGTH = 200;
export const PRODUCT_DESCRIPTION_MAX_LENGTH = 2000;
export const PRODUCT_VARIANT_NAME_MAX_LENGTH = 200;
export const PRODUCT_SKU_MAX_LENGTH = 64;
export const PRODUCT_MAX_IMAGES = 4;

/** Backend MediaOptions defaults. */
export const PRODUCT_IMAGE_MAX_SIZE_MB = 5;
export const PRODUCT_IMAGE_MAX_SIZE_BYTES = PRODUCT_IMAGE_MAX_SIZE_MB * 1024 * 1024;
export const PRODUCT_IMAGE_ALLOWED_TYPES = [
  "image/jpeg",
  "image/png",
  "image/webp",
] as const;

export const SELLING_UNITS: readonly SellingUnit[] = ["Piece", "Meter"] as const;
