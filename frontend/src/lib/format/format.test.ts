import { describe, expect, it } from "vitest";
import { formatFromPrice, formatPrice } from "@/lib/format/price";
import {
  formatQuantityIncrement,
  formatSellingUnit,
} from "@/lib/format/selling-unit";
import {
  buildProductsHref,
  parseProductListSearchParams,
} from "@/features/catalog/search-params";
import {
  getPrimaryProductImage,
  sortProductImages,
} from "@/features/catalog/images";
import {
  buildProductJsonLd,
  serializeJsonLd,
} from "@/features/catalog/product-json-ld";
import type { CatalogProductDto, ProductImageDto } from "@/features/catalog";
import { storeConfig } from "@/config/store";

describe("formatPrice", () => {
  it("formats finite amounts with centralized SYP display", () => {
    const result = formatPrice(1500);
    expect(result).toContain(storeConfig.currencyDisplay);
    expect(result).toMatch(/[0-9٠-٩]/);
  });

  it("returns an em dash for non-finite values", () => {
    expect(formatPrice(Number.NaN)).toBe("—");
    expect(formatPrice(Number.POSITIVE_INFINITY)).toBe("—");
  });

  it("prefixes list prices with من", () => {
    expect(formatFromPrice(100)).toMatch(/^من /);
  });
});

describe("selling unit presentation", () => {
  it("maps Piece and Meter to Arabic labels", () => {
    expect(formatSellingUnit("Piece")).toBe("قطعة");
    expect(formatSellingUnit("Meter")).toBe("متر");
  });

  it("describes quantity increments", () => {
    expect(formatQuantityIncrement(1, "Piece")).toContain("قطعة");
    expect(formatQuantityIncrement(0.5, "Meter")).toContain("متر");
  });
});

describe("product list search params", () => {
  it("parses only supported filters and trims values", () => {
    expect(
      parseProductListSearchParams({
        search: "  كابل  ",
        categoryId: "  abc  ",
        sort: "price",
      }),
    ).toEqual({
      search: "كابل",
      categoryId: "abc",
    });
  });

  it("ignores empty values", () => {
    expect(parseProductListSearchParams({ search: "  ", categoryId: "" })).toEqual(
      {},
    );
  });

  it("builds products href from filters", () => {
    expect(buildProductsHref()).toBe("/products");
    expect(buildProductsHref({ search: "كابل" })).toBe(
      `/products?search=${encodeURIComponent("كابل")}`,
    );
    expect(buildProductsHref({ categoryId: "id-1", search: "سلك" })).toContain(
      "categoryId=id-1",
    );
  });
});

describe("product images", () => {
  const images: ProductImageDto[] = [
    {
      id: "2",
      url: "https://example.com/b.jpg",
      isPrimary: false,
      sortOrder: 2,
    },
    {
      id: "1",
      url: "https://example.com/a.jpg",
      isPrimary: true,
      sortOrder: 5,
    },
    {
      id: "3",
      url: "https://example.com/c.jpg",
      isPrimary: false,
      sortOrder: 1,
    },
  ];

  it("orders primary first then by sortOrder", () => {
    expect(sortProductImages(images).map((image) => image.id)).toEqual([
      "1",
      "3",
      "2",
    ]);
  });

  it("returns the primary image", () => {
    expect(getPrimaryProductImage(images)?.id).toBe("1");
  });
});

describe("product JSON-LD", () => {
  const product: CatalogProductDto = {
    id: "p1",
    name: "كابل",
    description: "وصف",
    categoryId: "c1",
    categoryName: "أسلاك",
    images: [
      {
        id: "i1",
        url: "https://example.com/a.jpg",
        isPrimary: true,
        sortOrder: 0,
      },
    ],
    variants: [
      {
        id: "v1",
        name: "1 متر",
        sku: "SKU-1",
        price: 100,
        sellingUnit: "Meter",
        quantityIncrement: 0.5,
        isActive: true,
        availableQuantity: 10,
        isInStock: true,
      },
      {
        id: "v2",
        name: "مخفي",
        sku: "SKU-2",
        price: 50,
        sellingUnit: "Piece",
        quantityIncrement: 1,
        isActive: false,
        availableQuantity: 0,
        isInStock: false,
      },
    ],
  };

  it("emits Product offers from active variants only", () => {
    const jsonLd = buildProductJsonLd(product, "https://store.test/products/p1");
    expect(jsonLd).not.toBeNull();
    expect(jsonLd!["@type"]).toBe("Product");
    expect(jsonLd!.name).toBe("كابل");
    expect(jsonLd!.sku).toBe("SKU-1");
    expect(jsonLd!.offers).toMatchObject({
      "@type": "Offer",
      sku: "SKU-1",
      price: 100,
      priceCurrency: "SYP",
      availability: "https://schema.org/InStock",
    });
    expect(jsonLd).not.toHaveProperty("aggregateRating");
    expect(jsonLd).not.toHaveProperty("brand");
    expect(jsonLd).not.toHaveProperty("gtin");
  });

  it("returns null when there are no active variants", () => {
    expect(
      buildProductJsonLd(
        { ...product, variants: product.variants.map((v) => ({ ...v, isActive: false })) },
        "https://store.test/products/p1",
      ),
    ).toBeNull();
  });

  it("escapes angle brackets when serializing", () => {
    expect(serializeJsonLd({ name: "a <b> c" })).toContain("\\u003c");
  });
});
