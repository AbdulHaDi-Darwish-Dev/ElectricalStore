/**
 * Store-level frontend configuration that is NOT part of backend DTOs.
 * Backend money fields are bare decimals; currency is a frontend concern for now.
 */
export const storeConfig = {
  /** Business currency for display / SEO later. Not returned by the API. */
  currencyCode: "SYP",
  currencyLocale: "ar-SY",
} as const;
