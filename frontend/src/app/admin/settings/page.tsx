import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { settingsManageCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "الإعدادات",
  robots: { index: false, follow: false },
};

export default function AdminSettingsPage() {
  return (
    <AdminFeaturePlaceholder
      title="إعدادات الطلب"
      featureLabel="الإعدادات"
      anyOf={settingsManageCodes}
      description="الحد الأدنى للطلب والإعدادات التشغيلية."
    />
  );
}
