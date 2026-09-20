import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { productsManageCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "المنتجات",
  robots: { index: false, follow: false },
};

export default function AdminProductsPage() {
  return (
    <AdminFeaturePlaceholder
      title="المنتجات"
      featureLabel="المنتجات"
      anyOf={productsManageCodes}
      description="إدارة المنتجات والمتغيرات والوسائط."
    />
  );
}
