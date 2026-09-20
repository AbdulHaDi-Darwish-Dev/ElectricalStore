import { storeConfig } from "@/config/store";

/**
 * Centralized Arabic-friendly price formatting for SYP.
 * Reusable across catalog, cart, checkout, and admin later.
 */
export function formatPrice(amount: number): string {
  if (!Number.isFinite(amount)) {
    return "—";
  }

  const fractionDigits = Number.isInteger(amount) ? 0 : 2;
  const formatted = new Intl.NumberFormat(storeConfig.currencyLocale, {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: 2,
  }).format(amount);

  return `${formatted} ${storeConfig.currencyDisplay}`;
}

/** Format a "from" list price, e.g. "من 1٬000 ل.س". */
export function formatFromPrice(amount: number): string {
  return `من ${formatPrice(amount)}`;
}
