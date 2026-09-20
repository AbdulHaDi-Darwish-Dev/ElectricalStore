import type { MetadataRoute } from "next";
import { site } from "@/config/site";
import { getCategories, getProducts } from "@/features/catalog";

/**
 * Public catalog sitemap.
 *
 * Normal path (API available):
 * - static public shells: `/`, `/categories`, `/products`
 * - plus each public category and product GUID URL from the live catalog APIs
 *
 * Failure path (API unreachable / error):
 * - returns ONLY the three static shells above
 * - does NOT invent product/category URLs
 * - is therefore an incomplete sitemap, not a claim that the catalog is empty
 *
 * No lastModified per entity: the API does not expose updatedAtUtc.
 */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const siteUrl = site.url.replace(/\/$/, "");

  const staticEntries: MetadataRoute.Sitemap = [
    {
      url: siteUrl,
      changeFrequency: "weekly",
      priority: 1,
    },
    {
      url: `${siteUrl}/categories`,
      changeFrequency: "weekly",
      priority: 0.8,
    },
    {
      url: `${siteUrl}/products`,
      changeFrequency: "weekly",
      priority: 0.8,
    },
  ];

  try {
    const [categories, products] = await Promise.all([
      getCategories(),
      getProducts(),
    ]);

    return [
      ...staticEntries,
      ...categories.map((category) => ({
        url: `${siteUrl}/categories/${category.id}`,
        changeFrequency: "weekly" as const,
        priority: 0.7,
      })),
      ...products.map((product) => ({
        url: `${siteUrl}/products/${product.id}`,
        changeFrequency: "weekly" as const,
        priority: 0.7,
      })),
    ];
  } catch {
    // Incomplete sitemap (static shells only) — never fabricate entity URLs.
    return staticEntries;
  }
}
