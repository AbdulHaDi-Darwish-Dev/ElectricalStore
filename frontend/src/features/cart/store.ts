"use client";

import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import {
  CART_PERSISTENCE_VERSION,
  CART_STORAGE_KEY,
  type AddToCartInput,
  type AddToCartResult,
  type CartItem,
  type PersistedCartV1,
} from "./types";
import {
  mergeAddToCart,
  removeCartItem,
  setCartItemQuantity,
} from "./logic";
import { parsePersistedCart } from "./persistence";
import {
  decrementQuantity,
  estimatedMerchandiseSubtotal,
  incrementQuantity,
  isValidQuantity,
} from "./quantity";

type CartStore = {
  items: CartItem[];
  hasHydrated: boolean;
  setHasHydrated: (value: boolean) => void;
  addItem: (input: AddToCartInput) => AddToCartResult;
  removeItem: (variantId: string) => void;
  setQuantity: (variantId: string, quantity: number) => boolean;
  incrementItem: (variantId: string) => boolean;
  /** Decrements by one increment. At minimum: no-op (does not remove the line). */
  decrementItem: (variantId: string) => boolean;
  clearCart: () => void;
  lineCount: () => number;
  estimatedSubtotal: () => number;
};

export const useCartStore = create<CartStore>()(
  persist(
    (set, get) => ({
      items: [],
      hasHydrated: false,

      setHasHydrated: (value) => set({ hasHydrated: value }),

      addItem: (input) => {
        const { result, items } = mergeAddToCart(get().items, input);
        if (result.ok) {
          set({ items });
        }
        return result;
      },

      removeItem: (variantId) => {
        set({ items: removeCartItem(get().items, variantId) });
      },

      setQuantity: (variantId, quantity) => {
        const next = setCartItemQuantity(get().items, variantId, quantity);
        if (!next) {
          return false;
        }
        set({ items: next });
        return true;
      },

      incrementItem: (variantId) => {
        const item = get().items.find((line) => line.variantId === variantId);
        if (!item) {
          return false;
        }
        const nextQty = incrementQuantity(item.quantity, item.quantityIncrement);
        if (!isValidQuantity(nextQty, item.quantityIncrement)) {
          return false;
        }
        return get().setQuantity(variantId, nextQty);
      },

      decrementItem: (variantId) => {
        const item = get().items.find((line) => line.variantId === variantId);
        if (!item) {
          return false;
        }
        const nextQty = decrementQuantity(item.quantity, item.quantityIncrement);
        // At minimum: leave the line in place — removal is explicit only.
        if (!isValidQuantity(nextQty, item.quantityIncrement)) {
          return false;
        }
        return get().setQuantity(variantId, nextQty);
      },

      clearCart: () => set({ items: [] }),

      lineCount: () => get().items.length,

      estimatedSubtotal: () => estimatedMerchandiseSubtotal(get().items),
    }),
    {
      name: CART_STORAGE_KEY,
      version: CART_PERSISTENCE_VERSION,
      storage: createJSONStorage(() => localStorage),
      partialize: (state): PersistedCartV1 => ({
        version: CART_PERSISTENCE_VERSION,
        items: state.items,
      }),
      merge: (persisted, current) => {
        const parsed = parsePersistedCart(persisted);
        return {
          ...current,
          items: parsed.items,
        };
      },
      onRehydrateStorage: () => (state, error) => {
        if (error) {
          // Corrupted storage — reset quietly.
          useCartStore.setState({ items: [], hasHydrated: true });
          return;
        }
        state?.setHasHydrated(true);
      },
    },
  ),
);

/** Distinct cart lines — badge count (not quantity sum). */
export function selectCartLineCount(state: CartStore): number {
  return state.items.length;
}
