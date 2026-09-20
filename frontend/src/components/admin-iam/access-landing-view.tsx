"use client";

import Link from "next/link";
import {
  AdminEmptyState,
  AdminPageHeader,
  AdminPermissionGate,
} from "@/components/admin";
import { accessManagementCodes } from "@/features/admin";
import { getVisibleIamModules } from "@/features/admin-iam";
import { useAuthStore } from "@/lib/auth";

export function AccessLandingView() {
  return (
    <AdminPermissionGate anyOf={accessManagementCodes}>
      <AccessLandingContent />
    </AdminPermissionGate>
  );
}

function AccessLandingContent() {
  const permissions = useAuthStore((s) => s.permissions);
  const modules = getVisibleIamModules(permissions);

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="إدارة الوصول"
        description="إدارة المستخدمين والأدوار والصلاحيات عبر Permixa. الترخيص يعتمد على رموز الصلاحيات فقط — وليس أسماء الأدوار. أولوية الاستثناءات: رفض المستخدم > سماح المستخدم > الدور > الرفض الافتراضي."
      />

      {modules.length === 0 ? (
        <AdminEmptyState
          title="لا توجد وحدات وصول متاحة"
          description="حسابك لا يملك صلاحيات IAM كافية لعرض وحدات إدارة الوصول."
        />
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2">
          {modules.map((m) => (
            <li key={m.id}>
              <Link
                href={m.href}
                className="block h-full rounded-md border border-border bg-card p-4 transition hover:bg-muted/40"
              >
                <h2 className="text-base font-semibold">{m.title}</h2>
                <p className="mt-1 text-sm leading-6 text-muted-foreground">
                  {m.description}
                </p>
              </Link>
            </li>
          ))}
        </ul>
      )}

      <p className="text-xs leading-5 text-muted-foreground">
        قفل المستخدمين وتغيير البريد وجلسات الأجهزة غير مفعّلة عبر واجهة الإدارة
        حالياً (مؤجلة في الخادم). إنشاء صلاحيات جديدة ديناميكياً غير مفعّل أيضاً.
      </p>
    </div>
  );
}
