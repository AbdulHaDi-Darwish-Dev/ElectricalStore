/** Admin CategoryDto — mirrors ElectricalStore.Application.Catalog.Categories.CategoryDto */
export type AdminCategoryDto = {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  imageUrl: string | null;
  hasImage: boolean;
};

/** CreateCategoryRequest */
export type CreateAdminCategoryRequest = {
  name: string;
  description?: string | null;
  isActive: boolean;
};

/** UpdateCategoryRequest — isActive is NOT part of update; use activate/deactivate. */
export type UpdateAdminCategoryRequest = {
  name: string;
  description?: string | null;
};

export const CATEGORY_NAME_MAX_LENGTH = 200;
export const CATEGORY_DESCRIPTION_MAX_LENGTH = 2000;

/** Backend MediaOptions defaults (Application.Media.MediaOptions). */
export const CATEGORY_IMAGE_MAX_SIZE_MB = 5;
export const CATEGORY_IMAGE_MAX_SIZE_BYTES = CATEGORY_IMAGE_MAX_SIZE_MB * 1024 * 1024;
export const CATEGORY_IMAGE_ALLOWED_TYPES = [
  "image/jpeg",
  "image/png",
  "image/webp",
] as const;
