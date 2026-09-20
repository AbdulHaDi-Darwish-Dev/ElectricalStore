"use client";

import { useEffect, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  selectAuthReady,
  selectIsAuthenticated,
  useAuthStore,
} from "@/lib/auth";
import { canAccessAdminShell } from "@/features/admin";
import { AdminAccessDenied } from "./admin-access-denied";
import { AdminLoadingState } from "./admin-loading-state";

type AdminGateProps = {
  children: ReactNode;
};

/**
 * Protects /admin: wait for auth init, redirect anonymous, deny without Admin permissions.
 */
export function AdminGate({ children }: AdminGateProps) {
  const router = useRouter();
  const pathname = usePathname();
  const ready = useAuthStore(selectAuthReady);
  const authenticated = useAuthStore(selectIsAuthenticated);
  const permissions = useAuthStore((s) => s.permissions);

  useEffect(() => {
    if (!ready) return;
    if (!authenticated) {
      const returnTo = encodeURIComponent(pathname || "/admin");
      router.replace(`/login?returnTo=${returnTo}`);
    }
  }, [ready, authenticated, pathname, router]);

  if (!ready) {
    return (
      <div className="flex min-h-full items-center justify-center bg-background px-4 py-16">
        <div className="w-full max-w-md">
          <AdminLoadingState label="جاري التحقق من الجلسة…" />
        </div>
      </div>
    );
  }

  if (!authenticated) {
    return (
      <div className="flex min-h-full items-center justify-center bg-background px-4 py-16 text-sm text-muted-foreground">
        جاري التحويل لتسجيل الدخول…
      </div>
    );
  }

  if (!canAccessAdminShell(permissions)) {
    return (
      <div className="flex min-h-full items-center justify-center bg-background px-4 py-16">
        <AdminAccessDenied
          title="لا يمكن فتح لوحة الإدارة"
          message="حسابك مسجّل الدخول لكنّه لا يملك أي صلاحية إدارية معروفة. لوحة الإدارة مخصّصة للحسابات التي لديها صلاحيات تشغيل أو إدارة وصول."
        />
      </div>
    );
  }

  return <>{children}</>;
}
