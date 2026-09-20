"use client";

import type { ReactNode } from "react";
import { hasAnyPermission, useAuthStore } from "@/lib/auth";
import { AdminAccessDenied } from "./admin-access-denied";

type AdminPermissionGateProps = {
  anyOf: readonly string[];
  children: ReactNode;
  deniedTitle?: string;
  deniedMessage?: string;
};

/**
 * Route-level UX gate for a module. Hidden nav is not enough for direct URLs.
 * Backend remains authoritative for API calls.
 */
export function AdminPermissionGate({
  anyOf,
  children,
  deniedTitle,
  deniedMessage,
}: AdminPermissionGateProps) {
  const permissions = useAuthStore((s) => s.permissions);
  const allowed = hasAnyPermission(permissions, anyOf);

  if (!allowed) {
    return (
      <AdminAccessDenied
        title={deniedTitle ?? "غير مصرح لهذه الوحدة"}
        message={
          deniedMessage ??
          "ليس لديك الصلاحية اللازمة لعرض هذه الصفحة. عناصر القائمة تظهر فقط للوحدات المتاحة لحسابك."
        }
      />
    );
  }

  return <>{children}</>;
}
