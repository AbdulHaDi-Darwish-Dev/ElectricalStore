import { hasAnyPermission } from "@/lib/auth";
import {
  ADMIN_SHELL_PERMISSION_CODES,
  accessManagementCodes,
  categoriesManageCodes,
  inventoryAccessCodes,
  ordersAccessCodes,
  productsManageCodes,
  settingsManageCodes,
  shippingManageCodes,
} from "./permission-catalog";

/** UX: user may enter the Admin shell (not a security boundary). */
export function canAccessAdminShell(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, ADMIN_SHELL_PERMISSION_CODES);
}

export function canAccessCategories(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, categoriesManageCodes);
}

export function canAccessProducts(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, productsManageCodes);
}

export function canAccessInventory(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, inventoryAccessCodes);
}

export function canAccessOrders(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, ordersAccessCodes);
}

export function canAccessShipping(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, shippingManageCodes);
}

export function canAccessSettings(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, settingsManageCodes);
}

export function canAccessAccessManagement(
  permissions: readonly string[] | null | undefined,
): boolean {
  return hasAnyPermission(permissions, accessManagementCodes);
}
