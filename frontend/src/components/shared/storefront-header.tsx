"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useId, useState } from "react";
import { brand } from "@/config/brand";
import { storefrontNav } from "@/config/navigation";
import { CartBadge } from "@/components/cart/cart-badge";
import {
  selectAuthReady,
  selectIsAuthenticated,
  useAuthActions,
  useAuthStore,
} from "@/lib/auth";

export function StorefrontHeader() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const menuId = useId();
  const authReady = useAuthStore(selectAuthReady);
  const authenticated = useAuthStore(selectIsAuthenticated);
  const { logout } = useAuthActions();
  const [loggingOut, setLoggingOut] = useState(false);

  function closeMenu() {
    setOpen(false);
  }

  async function onLogout() {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
      closeMenu();
    }
  }

  const authLinks = !authReady ? (
    <span className="inline-block h-4 w-20 animate-pulse rounded bg-muted" aria-hidden />
  ) : authenticated ? (
    <>
      <Link
        href="/account"
        className="text-sm text-foreground hover:text-primary"
        onClick={closeMenu}
      >
        حسابي
      </Link>
      <button
        type="button"
        onClick={() => void onLogout()}
        disabled={loggingOut}
        className="text-sm text-foreground hover:text-primary disabled:opacity-60"
      >
        {loggingOut ? "جاري الخروج…" : "تسجيل الخروج"}
      </button>
    </>
  ) : (
    <>
      <Link
        href="/login"
        className="text-sm text-foreground hover:text-primary"
        onClick={closeMenu}
      >
        تسجيل الدخول
      </Link>
      <Link
        href="/register"
        className="text-sm text-foreground hover:text-primary"
        onClick={closeMenu}
      >
        إنشاء حساب
      </Link>
    </>
  );

  return (
    <header className="sticky top-0 z-40 border-b border-border/80 bg-card/95 backdrop-blur-sm">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-3 px-4 py-3 sm:gap-4 sm:px-6">
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

        <div className="flex items-center gap-3">
          <div className="hidden items-center gap-3 md:flex">{authLinks}</div>
          <CartBadge />
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
            <li>
              <Link
                href="/cart"
                onClick={closeMenu}
                aria-current={pathname === "/cart" ? "page" : undefined}
                className={`block py-3 text-sm ${
                  pathname === "/cart"
                    ? "font-medium text-primary"
                    : "text-foreground"
                }`}
              >
                السلة
              </Link>
            </li>
            {!authReady ? (
              <li className="py-3">
                <span className="inline-block h-4 w-24 animate-pulse rounded bg-muted" />
              </li>
            ) : authenticated ? (
              <>
                <li>
                  <Link
                    href="/account"
                    onClick={closeMenu}
                    className="block py-3 text-sm text-foreground"
                  >
                    حسابي
                  </Link>
                </li>
                <li>
                  <button
                    type="button"
                    onClick={() => void onLogout()}
                    disabled={loggingOut}
                    className="block w-full py-3 text-start text-sm text-foreground disabled:opacity-60"
                  >
                    {loggingOut ? "جاري الخروج…" : "تسجيل الخروج"}
                  </button>
                </li>
              </>
            ) : (
              <>
                <li>
                  <Link
                    href="/login"
                    onClick={closeMenu}
                    className="block py-3 text-sm text-foreground"
                  >
                    تسجيل الدخول
                  </Link>
                </li>
                <li>
                  <Link
                    href="/register"
                    onClick={closeMenu}
                    className="block py-3 text-sm text-foreground"
                  >
                    إنشاء حساب
                  </Link>
                </li>
              </>
            )}
          </ul>
        </nav>
      ) : null}
    </header>
  );
}
