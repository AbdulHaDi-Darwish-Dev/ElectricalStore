import type { Metadata } from "next";
import { OrderDetailView } from "@/components/admin-orders";

type PageProps = {
  params: Promise<{ id: string }>;
};

export const metadata: Metadata = {
  title: "تفاصيل الطلب",
  robots: { index: false, follow: false },
};

export default async function AdminOrderDetailPage({ params }: PageProps) {
  const { id } = await params;
  return <OrderDetailView orderId={id} />;
}
