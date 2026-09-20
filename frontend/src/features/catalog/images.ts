import type { ProductImageDto } from "./types";

/**
 * Sort product images using server fields: primary first, then sortOrder.
 */
export function sortProductImages(
  images: readonly ProductImageDto[],
): ProductImageDto[] {
  return [...images].sort((a, b) => {
    if (a.isPrimary !== b.isPrimary) {
      return a.isPrimary ? -1 : 1;
    }
    return a.sortOrder - b.sortOrder;
  });
}

export function getPrimaryProductImage(
  images: readonly ProductImageDto[],
): ProductImageDto | undefined {
  return sortProductImages(images)[0];
}
