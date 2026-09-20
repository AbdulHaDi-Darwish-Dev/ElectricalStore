"use client";

import Link from "next/link";
import { getAdminDashboardModules } from "@/features/admin";
import { useAuthStore } from "@/lib/auth";
import { AdminEmptyState } from "./admin-empty-state";
import { AdminPageHeader } from "./admin-page-header";
import { AdminSection } from "./admin-section";

export function AdminDashboard() {
  const permissions = useAuthStore((s) => s.permissions);
  const modules = getAdminDashboardModules(permissions);

  return (
    <div className="space-y-8">
      <AdminPageHeader
        title="لوحة التحكم"
        description="اختر وحدة تشغيلية متاحة لحسابك. لا تُعرض أرقام أو مؤشرات وهمية — البيانات التشغيلية تُجلب من الخادم في مراحل لاحقة."
      />

      <AdminSection
        title="الوحدات المتاحة"
        description="تظهر فقط الوحدات التي لديك صلاحية للوصول إليها."
      >
        {modules.length === 0 ? (
          <AdminEmptyState
            title="لا توجد وحدات فرعية ظاهرة"
            description="يمكنك البقاء في لوحة التحكم، لكن حسابك لا يملك صلاحيات لوحدات الكتالوج أو التشغيل أو الإعدادات أو إدارة الوصول."
          />
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {modules.map((mod) => (
              <li key={mod.href}>
                <Link
                  href={mod.href}
                  className="flex h-full flex-col gap-2 rounded-md border border-border bg-card p-4 transition hover:border-primary/40 hover:bg-accent/40"
                >
                  <p className="text-[11px] font-semibold tracking-wide text-muted-foreground uppercase">
                    {mod.sectionLabel}
                  </p>
                  <p className="text-base font-semibold text-foreground">
                    {mod.label}
                  </p>
                  <p className="text-sm leading-6 text-muted-foreground">
                    {mod.description}
                  </p>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </AdminSection>

      <AdminSection title="تذكير أمني">
        <p className="rounded-md border border-border bg-muted/40 px-4 py-3 text-sm leading-7 text-muted-foreground">
          واجهة الإدارة تعتمد على الصلاحيات الفعّالة من الخادم للعرض فقط. رفض
          الوصول النهائي يبقى من مسؤولية واجهات ASP.NET. لا تعتمد على إخفاء
          عناصر القائمة كحماية أمنية.
        </p>
      </AdminSection>
    </div>
  );
}
