import type { Metadata } from "next";
import { AdminFeaturePlaceholder } from "@/components/admin";
import { shippingManageCodes } from "@/features/admin";

export const metadata: Metadata = {
  title: "الشحن",
  robots: { index: false, follow: false },
};

export default function AdminShippingPage() {
  return (
    <AdminFeaturePlaceholder
      title="الشحن"
      featureLabel="الشحن"
      anyOf={shippingManageCodes}
      description="مناطق التوصيل ورسوم الشحن الثابتة."
    />
  );
}
