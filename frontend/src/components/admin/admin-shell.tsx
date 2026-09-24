"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import { BrandLogo } from "@/components/brand/brand-logo";
import { brand } from "@/config/brand";
import { useAuthActions } from "@/lib/auth";
import { AdminSidebarNav } from "./admin-sidebar-nav";

export function AdminShellChrome({ children }: { children: ReactNode }) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const { logout } = useAuthActions();
  const [loggingOut, setLoggingOut] = useState(false);

  async function onLogout() {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
      setMobileOpen(false);
    }
  }

  return (
    <div className="flex min-h-screen bg-background">
      {/* Desktop sidebar — sticky full viewport height; RTL start edge via border-e */}
      <aside className="sticky top-0 hidden h-screen w-64 shrink-0 flex-col border-e border-sidebar-border bg-sidebar text-sidebar-foreground md:flex">
        <div className="border-b border-sidebar-border px-4 py-4">
          <p className="text-xs text-sidebar-muted">لوحة الإدارة</p>
          <div className="mt-2">
            <BrandLogo surface="dark" markSize={24} />
          </div>
        </div>
        <AdminSidebarNav />
        <div className="mt-auto space-y-1 border-t border-sidebar-border p-3 text-sm">
          <Link
            href="/"
            className="block rounded-md px-3 py-2 text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
          >
            العودة إلى المتجر
          </Link>
          <button
            type="button"
            onClick={() => void onLogout()}
            disabled={loggingOut}
            className="block w-full rounded-md px-3 py-2 text-start text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground disabled:opacity-60"
          >
            {loggingOut ? "جاري الخروج…" : "تسجيل الخروج"}
          </button>
        </div>
      </aside>

      <div className="flex min-h-screen min-w-0 flex-1 flex-col">
        <header className="border-b border-border bg-card">
          <div className="mx-auto flex w-full max-w-[1400px] items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-8">
            <div className="flex min-w-0 items-center gap-3">
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-md border border-border px-3 py-2 text-sm md:hidden"
                aria-expanded={mobileOpen}
                aria-controls="admin-mobile-nav"
                onClick={() => setMobileOpen((v) => !v)}
              >
                {mobileOpen ? "إغلاق القائمة" : "القائمة"}
              </button>
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-foreground sm:text-base">
                  إدارة {brand.shortName}
                </p>
                <p className="hidden text-xs text-muted-foreground sm:block">
                  واجهة تشغيلية — الصلاحيات من الخادم
                </p>
              </div>
            </div>
            <div className="hidden items-center gap-3 text-sm md:flex">
              <Link href="/" className="text-foreground hover:text-primary">
                المتجر
              </Link>
              <Link href="/account" className="text-foreground hover:text-primary">
                حسابي
              </Link>
            </div>
          </div>
        </header>

        {mobileOpen ? (
          <div
            id="admin-mobile-nav"
            className="border-b border-sidebar-border bg-sidebar text-sidebar-foreground md:hidden"
          >
            <AdminSidebarNav onNavigate={() => setMobileOpen(false)} />
            <div className="space-y-1 border-t border-sidebar-border p-3 text-sm">
              <Link
                href="/"
                onClick={() => setMobileOpen(false)}
                className="block rounded-md px-3 py-2 hover:bg-sidebar-accent"
              >
                العودة إلى المتجر
              </Link>
              <Link
                href="/account"
                onClick={() => setMobileOpen(false)}
                className="block rounded-md px-3 py-2 hover:bg-sidebar-accent"
              >
                حسابي
              </Link>
              <button
                type="button"
                onClick={() => void onLogout()}
                disabled={loggingOut}
                className="block w-full rounded-md px-3 py-2 text-start hover:bg-sidebar-accent disabled:opacity-60"
              >
                {loggingOut ? "جاري الخروج…" : "تسجيل الخروج"}
              </button>
            </div>
          </div>
        ) : null}

        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <div className="mx-auto w-full max-w-[1400px] space-y-6">{children}</div>
        </main>
      </div>
    </div>
  );
}
