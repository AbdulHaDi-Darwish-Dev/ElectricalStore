import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { categoriesManageCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "التصنيفات",
  robots: { index: false, follow: false },
};

export default function AdminCategoriesPage() {
  return (
    <AdminFeaturePlaceholder
      title="التصنيفات"
      featureLabel="التصنيفات"
      anyOf={categoriesManageCodes}
      description="إدارة تصنيفات الكتالوج العام."
    />
  );
}
