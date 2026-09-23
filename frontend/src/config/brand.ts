/**
 * Commercial brand identity (presentation only).
 * ElectricalStore remains the technical project name — not the storefront brand.
 */
export const brand = {
  /** Primary commercial display name (Arabic-first storefront). */
  name: "فولتانو",
  /** Latin wordmark. */
  nameEn: "VOLTANO",
  /** Compact label for tight UI. */
  shortName: "فولتانو",
  /** Brand concept (not embedded in the logo lockup). */
  concept: "CONTROLLED ENERGY",
  /** Storefront-facing description. */
  description: "حلول كهربائية موثوقة، من المنتج إلى المشروع.",
  /** Primary mark (light surfaces). */
  logoMarkSrc: "/brand/mark.svg",
  /** Mark for dark surfaces. */
  logoMarkDarkSrc: "/brand/mark-reversed.svg",
  /** Monochrome mark. */
  logoMarkMonoSrc: "/brand/mark-mono.svg",
  /** Horizontal English lockup SVG (asset reference). */
  logoSrc: "/brand/lockup-horizontal.svg",
  /** Horizontal lockup for dark surfaces. */
  logoDarkSrc: "/brand/lockup-horizontal-dark.svg",
  /** Arabic lockup SVG (asset reference). */
  logoArSrc: "/brand/lockup-ar.svg",
  logoAlt: "فولتانو — VOLTANO",
  faviconSrc: "/brand/favicon.svg",
  /** Optional Open Graph brand image. */
  ogImageSrc: "/brand/og-mark.svg",
  themeColor: "#1769FF",
} as const;

export type BrandConfig = typeof brand;
