import { storeConfig } from "@/config/store";
import type { CatalogProductDto } from "./types";
import { sortProductImages } from "./images";

export type ProductJsonLd = {
  "@context": "https://schema.org";
  "@type": "Product";
  name: string;
  description?: string;
  image?: string[];
  sku?: string;
  offers:
    | ProductOfferJsonLd
    | ProductOfferJsonLd[];
};

export type ProductOfferJsonLd = {
  "@type": "Offer";
  name?: string;
  sku: string;
  price: number;
  priceCurrency: string;
  availability: "https://schema.org/InStock" | "https://schema.org/OutOfStock";
  url: string;
};

/**
 * Build Product JSON-LD from actual catalog fields only.
 * Omits ratings, brand, GTIN, manufacturer — not in the API.
 */
export function buildProductJsonLd(
  product: CatalogProductDto,
  productUrl: string,
): ProductJsonLd | null {
  const activeVariants = product.variants.filter((variant) => variant.isActive);
  if (activeVariants.length === 0) {
    return null;
  }

  const images = sortProductImages(product.images)
    .map((image) => image.url)
    .filter(Boolean);

  const offers: ProductOfferJsonLd[] = activeVariants.map((variant) => ({
    "@type": "Offer",
    name: variant.name,
    sku: variant.sku,
    price: variant.price,
    priceCurrency: storeConfig.currencyCode,
    availability: variant.isInStock
      ? "https://schema.org/InStock"
      : "https://schema.org/OutOfStock",
    url: productUrl,
  }));

  const jsonLd: ProductJsonLd = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.name,
    offers: offers.length === 1 ? offers[0]! : offers,
  };

  if (product.description) {
    jsonLd.description = product.description;
  }
  if (images.length > 0) {
    jsonLd.image = images;
  }
  if (offers.length === 1) {
    jsonLd.sku = offers[0]!.sku;
  }

  return jsonLd;
}

/**
 * Serialize JSON-LD safely for inline `<script type="application/ld+json">`.
 * Escapes `<` to reduce XSS risk if a field ever contains markup-like text.
 */
export function serializeJsonLd(data: unknown): string {
  return JSON.stringify(data).replace(/</g, "\\u003c");
}
