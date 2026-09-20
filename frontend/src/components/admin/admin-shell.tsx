"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
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
    <div className="flex min-h-full bg-background">
      {/* Desktop sidebar — natural RTL start edge via border-e */}
      <aside className="hidden w-64 shrink-0 flex-col border-e border-border bg-card md:flex">
        <div className="border-b border-border px-4 py-4">
          <p className="text-xs text-muted-foreground">لوحة الإدارة</p>
          <p className="text-base font-semibold text-foreground">{brand.shortName}</p>
        </div>
        <AdminSidebarNav />
        <div className="mt-auto space-y-1 border-t border-border p-3 text-sm">
          <Link
            href="/"
            className="block rounded-md px-3 py-2 text-foreground hover:bg-muted"
          >
            العودة إلى المتجر
          </Link>
          <button
            type="button"
            onClick={() => void onLogout()}
            disabled={loggingOut}
            className="block w-full rounded-md px-3 py-2 text-start text-foreground hover:bg-muted disabled:opacity-60"
          >
            {loggingOut ? "جاري الخروج…" : "تسجيل الخروج"}
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between gap-3 border-b border-border bg-card px-4 py-3 sm:px-6">
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
        </header>

        {mobileOpen ? (
          <div
            id="admin-mobile-nav"
            className="border-b border-border bg-card md:hidden"
          >
            <AdminSidebarNav onNavigate={() => setMobileOpen(false)} />
            <div className="space-y-1 border-t border-border p-3 text-sm">
              <Link
                href="/"
                onClick={() => setMobileOpen(false)}
                className="block rounded-md px-3 py-2 hover:bg-muted"
              >
                العودة إلى المتجر
              </Link>
              <Link
                href="/account"
                onClick={() => setMobileOpen(false)}
                className="block rounded-md px-3 py-2 hover:bg-muted"
              >
                حسابي
              </Link>
              <button
                type="button"
                onClick={() => void onLogout()}
                disabled={loggingOut}
                className="block w-full rounded-md px-3 py-2 text-start hover:bg-muted disabled:opacity-60"
              >
                {loggingOut ? "جاري الخروج…" : "تسجيل الخروج"}
              </button>
            </div>
          </div>
        ) : null}

        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <div className="mx-auto w-full max-w-5xl space-y-6">{children}</div>
        </main>
      </div>
    </div>
  );
}
