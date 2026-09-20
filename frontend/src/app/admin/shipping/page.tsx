import type { Metadata } from "next";
import { Suspense } from "react";
import { AdminLoadingState } from "@/components/admin";
import { ShippingZonesListView } from "@/components/admin-shipping";

export const metadata: Metadata = {
  title: "الشحن",
  robots: { index: false, follow: false },
};

export default function AdminShippingPage() {
  return (
    <Suspense fallback={<AdminLoadingState label="جاري تحميل مناطق الشحن…" />}>
      <ShippingZonesListView />
    </Suspense>
  );
}
