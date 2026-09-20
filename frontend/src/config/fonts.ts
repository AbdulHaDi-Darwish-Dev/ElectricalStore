/**
 * Font configuration for Arabic UI.
 * Swap `family` / Google font import in root layout when brand typography is chosen.
 */
export const fonts = {
  /** CSS variable name attached by next/font. */
  sansVariable: "--font-arabic-sans",
  /** Human-readable note for future brand typography decisions. */
  preferredFamily: "Noto Sans Arabic",
} as const;
