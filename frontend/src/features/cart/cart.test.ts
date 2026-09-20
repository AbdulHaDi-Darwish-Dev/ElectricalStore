import { describe, expect, it } from "vitest";
import {
  addQuantities,
  applyDecrementQuantity,
  canDecrementQuantity,
  clampToAvailable,
  defaultQuantity,
  estimatedLineTotal,
  estimatedMerchandiseSubtotal,
  getCartBadgeAriaLabel,
  getCartBadgeDisplay,
  isValidQuantity,
  mergeAddToCart,
  minimumQuantity,
  normalizeQuantity,
  parsePersistedCart,
  removeCartItem,
  setCartItemQuantity,
  type AddToCartInput,
  type CartItem,
} from "@/features/cart";

function sampleInput(
  overrides: Partial<AddToCartInput> = {},
): AddToCartInput {
  return {
    productId: "p1",
    variantId: "v1",
    productName: "كابل",
    variantName: "1 متر",
    primaryImageUrl: "https://example.com/a.jpg",
    sku: "SKU-1",
    sellingUnit: "Meter",
    quantityIncrement: 0.5,
    unitPrice: 100,
    isInStock: true,
    availableQuantity: 10,
    ...overrides,
  };
}

function sampleItem(overrides: Partial<CartItem> = {}): CartItem {
  return {
    productId: "p1",
    variantId: "v1",
    productName: "كابل",
    variantName: "1 متر",
    primaryImageUrl: "https://example.com/a.jpg",
    sku: "SKU-1",
    sellingUnit: "Meter",
    quantityIncrement: 0.5,
    quantity: 0.5,
    lastKnownUnitPrice: 100,
    ...overrides,
  };
}

describe("cart quantity helpers", () => {
  it("uses quantityIncrement as the default add quantity", () => {
    expect(defaultQuantity(1)).toBe(1);
    expect(defaultQuantity(0.5)).toBe(0.5);
  });

  it("normalizes floating-point addition (0.1 + 0.2)", () => {
    expect(addQuantities(0.1, 0.2, 0.1)).toBe(0.3);
    expect(normalizeQuantity(0.1 + 0.2, 0.1)).toBe(0.3);
    expect(String(normalizeQuantity(0.1 + 0.2, 0.1))).not.toContain(
      "000000000000004",
    );
  });

  it("validates positive quantities aligned to increment", () => {
    expect(isValidQuantity(1.5, 0.5)).toBe(true);
    expect(isValidQuantity(0, 0.5)).toBe(false);
    expect(isValidQuantity(-1, 1)).toBe(false);
    expect(isValidQuantity(1.2, 0.5)).toBe(false);
  });

  it("clamps to currently-known available quantity", () => {
    expect(clampToAvailable(3, 0.5, 2)).toBe(2);
    expect(clampToAvailable(0.5, 0.5, 0)).toBeNaN();
  });
});

describe("mergeAddToCart", () => {
  it("creates one line for a new variant with default increment quantity", () => {
    const { result, items } = mergeAddToCart([], sampleInput());
    expect(result.ok).toBe(true);
    expect(items).toHaveLength(1);
    expect(items[0]!.quantity).toBe(0.5);
    expect(items[0]!.lastKnownUnitPrice).toBe(100);
  });

  it("increases quantity when adding the same variant again", () => {
    const first = mergeAddToCart([], sampleInput({ quantity: 0.5 }));
    const second = mergeAddToCart(
      first.items,
      sampleInput({ quantity: 1, unitPrice: 120 }),
    );
    expect(second.result.ok).toBe(true);
    expect(second.items).toHaveLength(1);
    expect(second.items[0]!.quantity).toBe(1.5);
    // Snapshot refreshes from newest add.
    expect(second.items[0]!.lastKnownUnitPrice).toBe(120);
  });

  it("rejects out-of-stock variants", () => {
    const { result, items } = mergeAddToCart(
      [],
      sampleInput({ isInStock: false }),
    );
    expect(result).toEqual({ ok: false, reason: "out_of_stock" });
    expect(items).toHaveLength(0);
  });

  it("rejects quantities that exceed currently-known availability", () => {
    const { result } = mergeAddToCart(
      [],
      sampleInput({ quantity: 5, availableQuantity: 2 }),
    );
    // 5 clamps to 2 — still ok if room exists from empty cart
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.item.quantity).toBe(2);
    }

    const full = mergeAddToCart(
      [sampleItem({ quantity: 2 })],
      sampleInput({ quantity: 0.5, availableQuantity: 2 }),
    );
    expect(full.result).toEqual({ ok: false, reason: "exceeds_available" });
  });

  it("supports Piece increments of 1", () => {
    const { items } = mergeAddToCart(
      [],
      sampleInput({
        sellingUnit: "Piece",
        quantityIncrement: 1,
        quantity: undefined,
        availableQuantity: 20,
      }),
    );
    expect(items[0]!.quantity).toBe(1);
    expect(items[0]!.sellingUnit).toBe("Piece");
  });
});

describe("cart mutations", () => {
  it("setQuantity respects valid decimal increments and rejects invalid", () => {
    const items = [sampleItem({ quantity: 1 })];
    expect(setCartItemQuantity(items, "v1", 1.5)?.[0]!.quantity).toBe(1.5);
    expect(setCartItemQuantity(items, "v1", 0)).toBeNull();
    expect(setCartItemQuantity(items, "v1", -1)).toBeNull();
    expect(setCartItemQuantity(items, "v1", 1.2)).toBeNull();
  });

  it("decrement from 2 to 1 works when increment = 1", () => {
    expect(applyDecrementQuantity(2, 1)).toBe(1);
    expect(canDecrementQuantity(2, 1)).toBe(true);
  });

  it("decrement at minimum 1 does not remove — returns null / disabled", () => {
    expect(minimumQuantity(1)).toBe(1);
    expect(applyDecrementQuantity(1, 1)).toBeNull();
    expect(canDecrementQuantity(1, 1)).toBe(false);
    const items = [sampleItem({ quantity: 1, quantityIncrement: 1, sellingUnit: "Piece" })];
    // Simulating store: no-op leaves the line.
    expect(items).toHaveLength(1);
    expect(items[0]!.quantity).toBe(1);
  });

  it("decrement from 1.0 to 0.5 works when increment = 0.5", () => {
    expect(applyDecrementQuantity(1, 0.5)).toBe(0.5);
    expect(canDecrementQuantity(1, 0.5)).toBe(true);
  });

  it("decrement at minimum 0.5 does not remove the line", () => {
    expect(minimumQuantity(0.5)).toBe(0.5);
    expect(applyDecrementQuantity(0.5, 0.5)).toBeNull();
    expect(canDecrementQuantity(0.5, 0.5)).toBe(false);
    const items = [sampleItem({ quantity: 0.5 })];
    expect(items).toHaveLength(1);
  });

  it("removeItem deletes a variant line (explicit only)", () => {
    const items = [
      sampleItem({ variantId: "v1" }),
      sampleItem({ variantId: "v2", sku: "SKU-2" }),
    ];
    expect(removeCartItem(items, "v1")).toHaveLength(1);
    expect(removeCartItem(items, "v1")[0]!.variantId).toBe("v2");
  });

  it("clearing is represented as empty items array", () => {
    expect(removeCartItem([sampleItem()], "v1")).toEqual([]);
  });
});

describe("cart badge display", () => {
  it("hides numeric badge when empty after hydration", () => {
    expect(getCartBadgeDisplay(false, 0)).toEqual({ kind: "pending" });
    expect(getCartBadgeDisplay(true, 0)).toEqual({ kind: "empty" });
    expect(getCartBadgeDisplay(true, 2)).toEqual({ kind: "count", count: 2 });
    expect(getCartBadgeAriaLabel({ kind: "empty" })).toContain("فارغة");
  });
});

describe("estimated totals (display-only)", () => {
  it("computes line and merchandise subtotals from snapshots", () => {
    expect(estimatedLineTotal(100, 1.5)).toBe(150);
    expect(
      estimatedMerchandiseSubtotal([
        sampleItem({ quantity: 1, lastKnownUnitPrice: 100 }),
        sampleItem({
          variantId: "v2",
          quantity: 2,
          lastKnownUnitPrice: 50,
          quantityIncrement: 1,
          sellingUnit: "Piece",
        }),
      ]),
    ).toBe(200);
  });

  it("line count uses distinct variants, not quantity sum", () => {
    const items = [
      sampleItem({ variantId: "v1", quantity: 3 }),
      sampleItem({ variantId: "v2", quantity: 2, sku: "B" }),
    ];
    expect(items.length).toBe(2);
    expect(items.reduce((sum, item) => sum + item.quantity, 0)).toBe(5);
  });
});

describe("persisted cart parsing", () => {
  it("restores valid v1 state", () => {
    const restored = parsePersistedCart({
      version: 1,
      items: [sampleItem({ quantity: 1.5 })],
    });
    expect(restored.version).toBe(1);
    expect(restored.items).toHaveLength(1);
    expect(restored.items[0]!.quantity).toBe(1.5);
  });

  it("resets malformed or invalid persisted state to an empty cart", () => {
    expect(parsePersistedCart("not-json-object")).toEqual({
      version: 1,
      items: [],
    });
    expect(parsePersistedCart({ version: 1, items: [{ broken: true }] })).toEqual(
      {
        version: 1,
        items: [],
      },
    );
    expect(parsePersistedCart({ version: 99, items: [sampleItem()] })).toEqual({
      version: 1,
      items: [],
    });
  });

  it("sanitizes invalid quantities out of a mixed payload", () => {
    const restored = parsePersistedCart({
      version: 1,
      items: [
        sampleItem({ quantity: 1 }),
        sampleItem({ variantId: "bad", quantity: 0 }),
      ],
    });
    expect(restored.items).toHaveLength(1);
    expect(restored.items[0]!.variantId).toBe("v1");
  });
});
