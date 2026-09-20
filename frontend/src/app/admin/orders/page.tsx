import type { Metadata } from "next";
import { Suspense } from "react";
import { AdminLoadingState } from "@/components/admin";
import { OrdersListView } from "@/components/admin-orders";

export const metadata: Metadata = {
  title: "الطلبات",
  robots: { index: false, follow: false },
};

export default function AdminOrdersPage() {
  return (
    <Suspense fallback={<AdminLoadingState label="جاري تحميل الطلبات…" />}>
      <OrdersListView />
    </Suspense>
  );
}
