import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { accessManagementCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "إدارة الوصول",
  robots: { index: false, follow: false },
};

export default function AdminAccessPage() {
  return (
    <AdminFeaturePlaceholder
      title="إدارة الوصول"
      featureLabel="المستخدمين والأدوار والصلاحيات"
      anyOf={accessManagementCodes}
      description="سطح إدارة الهوية والصلاحيات فوق Permixa."
    />
  );
}
