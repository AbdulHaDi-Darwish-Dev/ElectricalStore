export {
  CART_PERSISTENCE_VERSION,
  CART_STORAGE_KEY,
  type AddToCartFailureReason,
  type AddToCartInput,
  type AddToCartResult,
  type CartItem,
  type CartPersistenceVersion,
  type CartState,
  type PersistedCartV1,
} from "./types";
export {
  addQuantities,
  applyDecrementQuantity,
  canDecrementQuantity,
  clampToAvailable,
  countDecimalPlaces,
  decrementQuantity,
  defaultQuantity,
  estimatedLineTotal,
  estimatedMerchandiseSubtotal,
  incrementQuantity,
  isValidQuantity,
  minimumQuantity,
  normalizeQuantity,
  roundQuantity,
} from "./quantity";
export { mergeAddToCart, removeCartItem, setCartItemQuantity } from "./logic";
export { parsePersistedCart, sanitizeCartItem } from "./persistence";
export {
  getCartBadgeAriaLabel,
  getCartBadgeDisplay,
  type CartBadgeDisplay,
} from "./badge";
export { selectCartLineCount, useCartStore } from "./store";
