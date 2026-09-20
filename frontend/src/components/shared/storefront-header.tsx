"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useId, useState } from "react";
import { brand } from "@/config/brand";
import { storefrontNav } from "@/config/navigation";

export function StorefrontHeader() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const menuId = useId();

  function closeMenu() {
    setOpen(false);
  }

  return (
    <header className="sticky top-0 z-40 border-b border-border/80 bg-card/95 backdrop-blur-sm">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
        <Link
          href="/"
          onClick={closeMenu}
          className="text-lg font-semibold tracking-tight text-foreground hover:text-primary"
        >
          {brand.shortName}
        </Link>

        <nav aria-label="التنقل الرئيسي" className="hidden md:block">
          <ul className="flex items-center gap-6 text-sm">
            {storefrontNav.map((item) => {
              const active =
                item.href === "/"
                  ? pathname === "/"
                  : pathname === item.href || pathname.startsWith(`${item.href}/`);
              return (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    aria-current={active ? "page" : undefined}
                    className={
                      active
                        ? "font-medium text-primary"
                        : "text-foreground hover:text-primary"
                    }
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>

        <button
          type="button"
          className="inline-flex items-center justify-center rounded-md border border-border px-3 py-2 text-sm text-foreground md:hidden"
          aria-expanded={open}
          aria-controls={menuId}
          onClick={() => setOpen((value) => !value)}
        >
          {open ? "إغلاق" : "القائمة"}
        </button>
      </div>

      {open ? (
        <nav
          id={menuId}
          aria-label="التنقل للجوال"
          className="border-t border-border bg-card md:hidden"
        >
          <ul className="mx-auto flex w-full max-w-6xl flex-col px-4 py-2 sm:px-6">
            {storefrontNav.map((item) => {
              const active =
                item.href === "/"
                  ? pathname === "/"
                  : pathname === item.href || pathname.startsWith(`${item.href}/`);
              return (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    onClick={closeMenu}
                    aria-current={active ? "page" : undefined}
                    className={`block py-3 text-sm ${
                      active ? "font-medium text-primary" : "text-foreground"
                    }`}
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>
      ) : null}
    </header>
  );
}
