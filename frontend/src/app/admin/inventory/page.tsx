import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { inventoryAccessCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "المخزون",
  robots: { index: false, follow: false },
};

export default function AdminInventoryPage() {
  return (
    <AdminFeaturePlaceholder
      title="المخزون"
      featureLabel="المخزون"
      anyOf={inventoryAccessCodes}
      description="قراءة المخزون وتعديل الكميات المتاحة."
    />
  );
}
