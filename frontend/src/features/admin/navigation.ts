import {
  canAccessAccessManagement,
  canAccessCategories,
  canAccessInventory,
  canAccessOrders,
  canAccessProducts,
  canAccessSettings,
  canAccessShipping,
} from "./access";
import {
  accessManagementCodes,
  categoriesManageCodes,
  inventoryAccessCodes,
  ordersAccessCodes,
  productsManageCodes,
  settingsManageCodes,
  shippingManageCodes,
} from "./permission-catalog";

export type AdminNavSectionId =
  | "overview"
  | "catalog"
  | "operations"
  | "configuration"
  | "access";

export type AdminNavItem = {
  id: string;
  href: string;
  label: string;
  description: string;
  /** Permission codes that reveal this item (any). */
  anyOf: readonly string[];
  isVisible: (permissions: readonly string[] | null | undefined) => boolean;
};

export type AdminNavSection = {
  id: AdminNavSectionId;
  label: string;
  items: AdminNavItem[];
};

/**
 * Permission-aware Admin navigation.
 * Items are omitted (not merely disabled) when the user lacks access.
 */
export const adminNavigation: AdminNavSection[] = [
  {
    id: "overview",
    label: "نظرة عامة",
    items: [
      {
        id: "dashboard",
        href: "/admin",
        label: "لوحة التحكم",
        description: "ملخص الوحدات المتاحة لحسابك.",
        anyOf: [],
        isVisible: () => true,
      },
    ],
  },
  {
    id: "catalog",
    label: "الكتالوج",
    items: [
      {
        id: "categories",
        href: "/admin/categories",
        label: "التصنيفات",
        description: "إدارة تصنيفات المتجر.",
        anyOf: categoriesManageCodes,
        isVisible: canAccessCategories,
      },
      {
        id: "products",
        href: "/admin/products",
        label: "المنتجات",
        description: "إدارة المنتجات والمتغيرات.",
        anyOf: productsManageCodes,
        isVisible: canAccessProducts,
      },
    ],
  },
  {
    id: "operations",
    label: "التشغيل",
    items: [
      {
        id: "inventory",
        href: "/admin/inventory",
        label: "المخزون",
        description: "متابعة المخزون وتعديل الكميات.",
        anyOf: inventoryAccessCodes,
        isVisible: canAccessInventory,
      },
      {
        id: "orders",
        href: "/admin/orders",
        label: "الطلبات",
        description: "متابعة دورة حياة الطلبات.",
        anyOf: ordersAccessCodes,
        isVisible: canAccessOrders,
      },
      {
        id: "shipping",
        href: "/admin/shipping",
        label: "الشحن",
        description: "مناطق التوصيل ورسوم الشحن.",
        anyOf: shippingManageCodes,
        isVisible: canAccessShipping,
      },
    ],
  },
  {
    id: "configuration",
    label: "الإعدادات",
    items: [
      {
        id: "settings",
        href: "/admin/settings",
        label: "إعدادات الطلب",
        description: "الحد الأدنى للطلب والإعدادات التشغيلية.",
        anyOf: settingsManageCodes,
        isVisible: canAccessSettings,
      },
    ],
  },
  {
    id: "access",
    label: "إدارة الوصول",
    items: [
      {
        id: "access-management",
        href: "/admin/access",
        label: "المستخدمون والصلاحيات",
        description: "المستخدمون والأدوار والصلاحيات الفعّالة.",
        anyOf: accessManagementCodes,
        isVisible: canAccessAccessManagement,
      },
    ],
  },
];

export function getVisibleAdminNavigation(
  permissions: readonly string[] | null | undefined,
): AdminNavSection[] {
  return adminNavigation
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => item.isVisible(permissions)),
    }))
    .filter((section) => section.items.length > 0);
}

export function getVisibleAdminModules(
  permissions: readonly string[] | null | undefined,
) {
  return getVisibleAdminNavigation(permissions).flatMap((section) =>
    section.items.filter((item) => item.id !== "dashboard"),
  );
}

export type AdminModuleCard = {
  href: string;
  label: string;
  description: string;
  sectionLabel: string;
};

export function getAdminDashboardModules(
  permissions: readonly string[] | null | undefined,
): AdminModuleCard[] {
  const cards: AdminModuleCard[] = [];
  for (const section of getVisibleAdminNavigation(permissions)) {
    for (const item of section.items) {
      if (item.id === "dashboard") continue;
      cards.push({
        href: item.href,
        label: item.label,
        description: item.description,
        sectionLabel: section.label,
      });
    }
  }
  return cards;
}

/** Placeholder copy for feature pages deferred past F7. */
export function adminFeaturePlaceholderMessage(featureLabel: string): string {
  return `إدارة ${featureLabel} ستُبنى في مرحلة لاحقة. هذه الصفحة أساس للصلاحيات والتنقّل فقط.`;
}
