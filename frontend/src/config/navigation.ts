/**
 * Storefront navigation labels (Arabic UI copy).
 * Admin navigation lives in features/admin (permission-aware).
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
