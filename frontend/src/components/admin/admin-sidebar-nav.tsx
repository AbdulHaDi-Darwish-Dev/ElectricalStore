"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { getVisibleAdminNavigation } from "@/features/admin";
import { useAuthStore } from "@/lib/auth";

type AdminSidebarProps = {
  onNavigate?: () => void;
};

function isActivePath(pathname: string, href: string): boolean {
  if (href === "/admin") {
    return pathname === "/admin";
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function AdminSidebarNav({ onNavigate }: AdminSidebarProps) {
  const pathname = usePathname();
  const permissions = useAuthStore((s) => s.permissions);
  const sections = getVisibleAdminNavigation(permissions);

  return (
    <nav aria-label="تنقل لوحة الإدارة" className="flex-1 overflow-y-auto px-2 py-3">
      <ul className="space-y-4">
        {sections.map((section) => (
          <li key={section.id}>
            <p className="px-3 pb-1 text-[11px] font-semibold tracking-wide text-sidebar-muted uppercase">
              {section.label}
            </p>
            <ul className="space-y-0.5">
              {section.items.map((item) => {
                const active = isActivePath(pathname, item.href);
                return (
                  <li key={item.id}>
                    <Link
                      href={item.href}
                      onClick={onNavigate}
                      aria-current={active ? "page" : undefined}
                      className={
                        active
                          ? "block rounded-md bg-sidebar-accent px-3 py-2 text-sm font-medium text-sidebar-accent-foreground"
                          : "block rounded-md px-3 py-2 text-sm text-sidebar-foreground/90 hover:bg-sidebar-accent/70 hover:text-sidebar-accent-foreground"
                      }
                    >
                      {item.label}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </li>
        ))}
      </ul>
    </nav>
  );
}
