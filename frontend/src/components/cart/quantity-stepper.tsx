"use client";

import { useId } from "react";
import { formatSellingUnit } from "@/lib/format";
import type { SellingUnit } from "@/features/catalog";
import {
  canDecrementQuantity,
  incrementQuantity,
  isValidQuantity,
  normalizeQuantity,
} from "@/features/cart";

type QuantityStepperProps = {
  value: number;
  increment: number;
  sellingUnit: SellingUnit;
  onChange: (next: number) => void;
  disabled?: boolean;
  id?: string;
  /** Accessible name prefix, e.g. variant name */
  label: string;
  minLabel?: string;
};

/**
 * Decimal-aware quantity control (+ / − / typed input).
 * Aligns values to quantityIncrement.
 */
export function QuantityStepper({
  value,
  increment,
  sellingUnit,
  onChange,
  disabled = false,
  id,
  label,
}: QuantityStepperProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const unitLabel = formatSellingUnit(sellingUnit);
  const canDecrement = !disabled && canDecrementQuantity(value, increment);
  const canIncrement = !disabled;

  function commit(raw: number) {
    const normalized = normalizeQuantity(raw, increment);
    if (isValidQuantity(normalized, increment)) {
      onChange(normalized);
    }
  }

  return (
    <div className="inline-flex items-stretch overflow-hidden rounded-md border border-border bg-card">
      <button
        type="button"
        disabled={!canDecrement}
        aria-label={`إنقاص كمية ${label}`}
        onClick={() => {
          const next = normalizeQuantity(value - increment, increment);
          if (isValidQuantity(next, increment)) {
            onChange(next);
          }
        }}
        className="flex h-11 w-11 items-center justify-center text-lg text-foreground transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40"
      >
        −
      </button>
      <label htmlFor={inputId} className="sr-only">
        كمية {label} بال{unitLabel}
      </label>
      <input
        id={inputId}
        type="number"
        inputMode="decimal"
        step={increment}
        min={increment}
        disabled={disabled}
        value={value}
        onChange={(event) => {
          const next = Number(event.target.value);
          if (Number.isFinite(next)) {
            onChange(next);
          }
        }}
        onBlur={() => commit(value)}
        className="w-20 border-x border-border bg-transparent px-2 text-center text-sm tabular-nums text-foreground outline-none disabled:opacity-40"
      />
      <button
        type="button"
        disabled={!canIncrement}
        aria-label={`زيادة كمية ${label}`}
        onClick={() => commit(incrementQuantity(value, increment))}
        className="flex h-11 w-11 items-center justify-center text-lg text-foreground transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40"
      >
        +
      </button>
    </div>
  );
}
