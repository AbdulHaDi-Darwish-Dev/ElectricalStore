import { brand } from "./brand";

/**
 * Site-level settings (locale, URLs, metadata defaults).
 * Brand display strings live in brand.ts — keep them separate.
 */
export const site = {
  /** Absolute site origin for metadata / canonical URLs. */
  url: process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3100",
  locale: "ar",
  htmlLang: "ar",
  dir: "rtl" as const,
  /**
   * Neutral catalog SEO fallback when a product/category has no description.
   * Editable centrally — not marketing copy.
   */
  catalogDescription: "تصفح تصنيفات ومنتجات فولتانو.",
  /** Default document title template; uses brand.name. */
  get defaultTitle() {
    return brand.name;
  },
  get defaultDescription() {
    return brand.description;
  },
} as const;

export type SiteConfig = typeof site;
