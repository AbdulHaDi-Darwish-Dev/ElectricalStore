/**
 * Commercial brand identity — centralized and temporary.
 * ElectricalStore is the technical project name only; do not treat it as the store brand.
 * Replace these placeholders when the commercial identity is finalized.
 */
export const brand = {
  /** Temporary commercial display name (Arabic). */
  name: "اسم المتجر",
  /** Short label for compact UI (header mark, mobile). */
  shortName: "المتجر",
  /** Neutral temporary description — not marketing copy. */
  description: "واجهة مؤقتة للمتجر. سيتم استبدال الاسم والشعار والهوية عند اعتماد العلامة التجارية.",
  /** Public logo path under /public, or null until assets exist. */
  logoSrc: null as string | null,
  logoAlt: "شعار المتجر",
  /** Optional dark-variant logo; unused until a dark theme exists. */
  logoDarkSrc: null as string | null,
  /** Favicon path; leave null to use the app default until brand assets exist. */
  faviconSrc: null as string | null,
} as const;

export type BrandConfig = typeof brand;
