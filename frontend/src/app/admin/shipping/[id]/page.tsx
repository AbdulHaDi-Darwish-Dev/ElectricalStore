import type { Metadata } from "next";
import { ShippingZoneEditView } from "@/components/admin-shipping";

type PageProps = {
  params: Promise<{ id: string }>;
};

export const metadata: Metadata = {
  title: "تعديل منطقة الشحن",
  robots: { index: false, follow: false },
};

export default async function AdminShippingEditPage({ params }: PageProps) {
  const { id } = await params;
  return <ShippingZoneEditView zoneId={id} />;
}
