/**
 * Storefront and admin navigation labels (Arabic UI copy).
 */

export type NavItem = {
  href: string;
  label: string;
  disabled?: boolean;
};

export const storefrontNav: NavItem[] = [
  { href: "/", label: "الرئيسية" },
  { href: "/categories", label: "التصنيفات" },
  { href: "/products", label: "المنتجات" },
];

export const adminNav: NavItem[] = [
  { href: "/admin", label: "لوحة التحكم" },
  { href: "#", label: "التصنيفات", disabled: true },
  { href: "#", label: "المنتجات", disabled: true },
  { href: "#", label: "المخزون", disabled: true },
  { href: "#", label: "الطلبات", disabled: true },
  { href: "#", label: "الشحن", disabled: true },
  { href: "#", label: "الإعدادات", disabled: true },
];
