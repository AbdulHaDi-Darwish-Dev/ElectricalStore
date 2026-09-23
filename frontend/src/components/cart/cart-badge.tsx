"use client";

import Link from "next/link";
import {
  getCartBadgeAriaLabel,
  getCartBadgeDisplay,
  selectCartLineCount,
  useCartStore,
} from "@/features/cart";

/**
 * Header cart entry. Numeric badge only when lineCount > 0 after hydration.
 */
export function CartBadge() {
  const hasHydrated = useCartStore((state) => state.hasHydrated);
  const lineCount = useCartStore(selectCartLineCount);
  const display = getCartBadgeDisplay(hasHydrated, lineCount);

  return (
    <Link
      href="/cart"
      aria-label={getCartBadgeAriaLabel(display)}
      className="relative inline-flex h-9 items-center gap-2 rounded-md border border-border bg-card px-3 text-sm text-foreground transition hover:border-primary/35 hover:text-primary"
    >
      <span>السلة</span>
      {display.kind === "pending" ? (
        <span
          aria-hidden
          className="inline-flex min-w-5 items-center justify-center rounded-full bg-muted px-1.5 py-0.5 text-xs text-muted-foreground"
        >
          ·
        </span>
      ) : null}
      {display.kind === "count" ? (
        <span className="inline-flex min-w-5 items-center justify-center rounded-full bg-primary px-1.5 py-0.5 text-xs font-medium text-primary-foreground tabular-nums">
          {display.count}
        </span>
      ) : null}
    </Link>
  );
}
