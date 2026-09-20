/**
 * Placeholder navigation labels (Arabic UI copy).
 * Routes are shells only — no business pages yet.
 */

export type NavItem = {
  href: string;
  label: string;
  disabled?: boolean;
};

export const storefrontNav: NavItem[] = [
  { href: "/", label: "الرئيسية" },
  { href: "#", label: "التصنيفات", disabled: true },
  { href: "#", label: "المنتجات", disabled: true },
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
