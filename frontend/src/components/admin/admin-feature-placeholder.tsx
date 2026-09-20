"use client";

import {
  AdminEmptyState,
  AdminPageHeader,
  AdminPermissionGate,
} from "@/components/admin";

type AdminFeaturePlaceholderProps = {
  title: string;
  featureLabel: string;
  anyOf: readonly string[];
  description?: string;
};

export function AdminFeaturePlaceholder({
  title,
  featureLabel,
  anyOf,
  description,
}: AdminFeaturePlaceholderProps) {
  return (
    <AdminPermissionGate anyOf={anyOf}>
      <div className="space-y-6">
        <AdminPageHeader
          title={title}
          description={description}
        />
        <AdminEmptyState
          title="قريباً"
          description={`إدارة ${featureLabel} ستُبنى في المرحلة التالية. هذه الصفحة أساس للصلاحيات والتنقّل فقط.`}
        />
      </div>
    </AdminPermissionGate>
  );
}
