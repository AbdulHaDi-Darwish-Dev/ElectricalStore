/**
 * Pure cart badge presentation — keeps hydration-safe UI free of numeric `0`.
 */
export type CartBadgeDisplay =
  | { kind: "pending" }
  | { kind: "empty" }
  | { kind: "count"; count: number };

export function getCartBadgeDisplay(
  hasHydrated: boolean,
  lineCount: number,
): CartBadgeDisplay {
  if (!hasHydrated) {
    return { kind: "pending" };
  }
  if (lineCount <= 0) {
    return { kind: "empty" };
  }
  return { kind: "count", count: lineCount };
}

export function getCartBadgeAriaLabel(display: CartBadgeDisplay): string {
  switch (display.kind) {
    case "pending":
      return "سلة التسوق";
    case "empty":
      return "سلة التسوق فارغة";
    case "count":
      return `سلة التسوق، ${display.count} ${display.count === 1 ? "صنف" : "أصناف"}`;
  }
}
