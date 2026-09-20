import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { ordersAccessCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "الطلبات",
  robots: { index: false, follow: false },
};

export default function AdminOrdersPage() {
  return (
    <AdminFeaturePlaceholder
      title="الطلبات"
      featureLabel="الطلبات"
      anyOf={ordersAccessCodes}
      description="متابعة دورة حياة الطلبات من لوحة التشغيل."
    />
  );
}
