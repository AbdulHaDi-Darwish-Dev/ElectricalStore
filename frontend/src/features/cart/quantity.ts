/**
 * Cart quantity helpers.
 * Uses the variant's actual quantityIncrement — never hardcodes Piece=1.
 */

/** Count decimal places of a finite number (pragmatic, no decimal.js). */
export function countDecimalPlaces(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }
  const normalized = value.toString().toLowerCase();
  if (normalized.includes("e")) {
    const [mantissa, expRaw] = normalized.split("e");
    const exp = Number(expRaw);
    const decimals = (mantissa?.split(".")[1] ?? "").length;
    return Math.max(0, decimals - exp);
  }
  const parts = normalized.split(".");
  return parts[1]?.length ?? 0;
}

/** Round a value to a safe fixed precision derived from the increment. */
export function roundQuantity(value: number, increment: number): number {
  if (!Number.isFinite(value)) {
    return Number.NaN;
  }
  const places = Math.min(Math.max(countDecimalPlaces(increment), 0), 6);
  const factor = 10 ** places;
  return Math.round(value * factor) / factor;
}

/**
 * Snap to the nearest positive multiple of increment.
 * Fixes float noise such as 0.1 + 0.2 → 0.3 (not 0.30000000000000004).
 */
export function normalizeQuantity(value: number, increment: number): number {
  if (!Number.isFinite(value) || !Number.isFinite(increment) || increment <= 0) {
    return Number.NaN;
  }
  const multiples = Math.round(value / increment);
  return roundQuantity(multiples * increment, increment);
}

/** True when quantity > 0 and aligns to increment within float tolerance. */
export function isValidQuantity(quantity: number, increment: number): boolean {
  if (
    !Number.isFinite(quantity) ||
    !Number.isFinite(increment) ||
    quantity <= 0 ||
    increment <= 0
  ) {
    return false;
  }
  const normalized = normalizeQuantity(quantity, increment);
  if (!Number.isFinite(normalized) || normalized <= 0) {
    return false;
  }
  return Math.abs(normalized - quantity) < 1e-8 * Math.max(1, increment);
}

/** Default add-to-cart quantity = the variant's quantityIncrement. */
export function defaultQuantity(increment: number): number {
  if (!Number.isFinite(increment) || increment <= 0) {
    return Number.NaN;
  }
  return roundQuantity(increment, increment);
}

export function addQuantities(
  current: number,
  delta: number,
  increment: number,
): number {
  return normalizeQuantity(current + delta, increment);
}

export function incrementQuantity(current: number, increment: number): number {
  return addQuantities(current, increment, increment);
}

export function decrementQuantity(current: number, increment: number): number {
  return addQuantities(current, -increment, increment);
}

/** Minimum valid cart quantity for a variant = its quantityIncrement. */
export function minimumQuantity(increment: number): number {
  return defaultQuantity(increment);
}

/** True when decrement would still land on a valid quantity (≥ minimum). */
export function canDecrementQuantity(
  current: number,
  increment: number,
): boolean {
  return isValidQuantity(decrementQuantity(current, increment), increment);
}

/**
 * Apply one decrement step.
 * Returns null when already at minimum — does NOT imply line removal.
 */
export function applyDecrementQuantity(
  current: number,
  increment: number,
): number | null {
  const next = decrementQuantity(current, increment);
  if (!isValidQuantity(next, increment)) {
    return null;
  }
  return next;
}

/**
 * Clamp quantity to currently-known available stock when provided.
 * Result remains aligned to increment (floors to last valid multiple).
 */
export function clampToAvailable(
  quantity: number,
  increment: number,
  availableQuantity: number | undefined,
): number {
  const normalized = normalizeQuantity(quantity, increment);
  if (!Number.isFinite(normalized) || normalized <= 0) {
    return Number.NaN;
  }
  if (
    availableQuantity === undefined ||
    !Number.isFinite(availableQuantity) ||
    availableQuantity < 0
  ) {
    return normalized;
  }
  if (normalized <= availableQuantity) {
    return normalized;
  }
  const maxMultiples = Math.floor(availableQuantity / increment + 1e-9);
  if (maxMultiples <= 0) {
    return Number.NaN;
  }
  return roundQuantity(maxMultiples * increment, increment);
}

/** Display-only estimated line total (not authoritative). */
export function estimatedLineTotal(
  lastKnownUnitPrice: number,
  quantity: number,
): number {
  if (!Number.isFinite(lastKnownUnitPrice) || !Number.isFinite(quantity)) {
    return Number.NaN;
  }
  return roundQuantity(lastKnownUnitPrice * quantity, 0.01);
}

/** Display-only estimated merchandise subtotal across lines. */
export function estimatedMerchandiseSubtotal(
  lines: ReadonlyArray<{ lastKnownUnitPrice: number; quantity: number }>,
): number {
  const sum = lines.reduce((acc, line) => {
    const lineTotal = estimatedLineTotal(line.lastKnownUnitPrice, line.quantity);
    return Number.isFinite(lineTotal) ? acc + lineTotal : acc;
  }, 0);
  return roundQuantity(sum, 0.01);
}
