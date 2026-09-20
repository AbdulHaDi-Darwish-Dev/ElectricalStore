/**
 * Frontend permission catalog — codes must match ASP.NET / Permixa exactly.
 * UX only; backend remains the authorization authority.
 * Do NOT authorize by role name. Do NOT invent codes.
 */

/** Application-owned permissions (ElectricalStore.Application.Authorization.AppPermissions). */
export const AppPermission = {
  categories: {
    manage: "Categories.Manage",
  },
  products: {
    manage: "Products.Manage",
  },
  inventory: {
    read: "Inventory.Read",
    adjust: "Inventory.Adjust",
  },
  shipping: {
    manage: "Shipping.Manage",
  },
  orders: {
    read: "Orders.Read",
    manage: "Orders.Manage",
  },
  settings: {
    manage: "Settings.Manage",
  },
} as const;

/**
 * Permixa IAM permissions used by host Access Management adapters
 * and verified against Permixa / integration contracts.
 */
export const IamPermission = {
  users: {
    read: "Iam.Users.Read",
    lock: "Iam.Users.Lock",
    changeEmail: "Iam.Users.ChangeEmail",
  },
  userRoles: {
    manage: "Iam.UserRoles.Manage",
  },
  userPermissionOverrides: {
    manage: "Iam.UserPermissionOverrides.Manage",
  },
  roles: {
    read: "Iam.Roles.Read",
    create: "Iam.Roles.Create",
    update: "Iam.Roles.Update",
    delete: "Iam.Roles.Delete",
  },
  rolePermissions: {
    manage: "Iam.RolePermissions.Manage",
  },
  permissions: {
    read: "Iam.Permissions.Read",
    create: "Iam.Permissions.Create",
    update: "Iam.Permissions.Update",
  },
  sessions: {
    read: "Iam.Sessions.Read",
  },
  audit: {
    read: "Iam.Audit.Read",
  },
} as const;

/** Flat list of all application permission codes. */
export const APP_PERMISSION_CODES: readonly string[] = [
  AppPermission.categories.manage,
  AppPermission.products.manage,
  AppPermission.inventory.read,
  AppPermission.inventory.adjust,
  AppPermission.shipping.manage,
  AppPermission.orders.read,
  AppPermission.orders.manage,
  AppPermission.settings.manage,
];

/** Flat list of IAM permission codes known to this host. */
export const IAM_PERMISSION_CODES: readonly string[] = [
  IamPermission.users.read,
  IamPermission.users.lock,
  IamPermission.users.changeEmail,
  IamPermission.userRoles.manage,
  IamPermission.userPermissionOverrides.manage,
  IamPermission.roles.read,
  IamPermission.roles.create,
  IamPermission.roles.update,
  IamPermission.roles.delete,
  IamPermission.rolePermissions.manage,
  IamPermission.permissions.read,
  IamPermission.permissions.create,
  IamPermission.permissions.update,
  IamPermission.sessions.read,
  IamPermission.audit.read,
];

/**
 * Any of these grants meaningful Admin-shell access.
 * There is no invented Admin.Access permission.
 */
export const ADMIN_SHELL_PERMISSION_CODES: readonly string[] = [
  ...APP_PERMISSION_CODES,
  ...IAM_PERMISSION_CODES,
];

export const categoriesManageCodes = [AppPermission.categories.manage] as const;
export const productsManageCodes = [AppPermission.products.manage] as const;
export const inventoryAccessCodes = [
  AppPermission.inventory.read,
  AppPermission.inventory.adjust,
] as const;
export const ordersAccessCodes = [
  AppPermission.orders.read,
  AppPermission.orders.manage,
] as const;
export const shippingManageCodes = [AppPermission.shipping.manage] as const;
export const settingsManageCodes = [AppPermission.settings.manage] as const;

/** Enough to open Access Management area (Users/Roles/Permissions/Audit later). */
export const accessManagementCodes = [
  IamPermission.users.read,
  IamPermission.userRoles.manage,
  IamPermission.userPermissionOverrides.manage,
  IamPermission.roles.read,
  IamPermission.roles.create,
  IamPermission.roles.update,
  IamPermission.roles.delete,
  IamPermission.rolePermissions.manage,
  IamPermission.permissions.read,
  IamPermission.permissions.create,
  IamPermission.permissions.update,
  IamPermission.sessions.read,
  IamPermission.audit.read,
] as const;
