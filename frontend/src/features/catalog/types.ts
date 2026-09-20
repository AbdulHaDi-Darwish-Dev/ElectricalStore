/**
 * Public catalog DTOs — camelCase JSON from ASP.NET Core.
 * No slugs. No invented fields.
 */

/** Exact SellingUnit enum names as serialized by the backend (.ToString()). */
export type SellingUnit = "Piece" | "Meter";

export type CategoryDto = {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  imageUrl: string | null;
  hasImage: boolean;
};

export type CatalogProductListItemDto = {
  id: string;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  fromPrice: number;
  primaryImageUrl: string;
  hasInStock: boolean;
};

export type ProductImageDto = {
  id: string;
  url: string;
  isPrimary: boolean;
  sortOrder: number;
};

export type CatalogProductVariantDto = {
  id: string;
  name: string;
  sku: string;
  price: number;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  isActive: boolean;
  availableQuantity: number;
  isInStock: boolean;
};

export type CatalogProductDto = {
  id: string;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  variants: CatalogProductVariantDto[];
  images: ProductImageDto[];
};

/** Only query params supported by GET /catalog/products. */
export type CatalogProductListParams = {
  categoryId?: string;
  search?: string;
};
