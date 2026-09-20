import type { Metadata } from "next";
import { ShippingZoneCreateView } from "@/components/admin-shipping";

export const metadata: Metadata = {
  title: "منطقة شحن جديدة",
  robots: { index: false, follow: false },
};

export default function AdminShippingNewPage() {
  return <ShippingZoneCreateView />;
}
