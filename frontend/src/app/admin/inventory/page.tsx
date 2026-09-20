import type { Metadata } from "next";
import { Suspense } from "react";
import { AdminLoadingState } from "@/components/admin";
import { InventoryListView } from "@/components/admin-inventory";

export const metadata: Metadata = {
  title: "المخزون",
  robots: { index: false, follow: false },
};

export default function AdminInventoryPage() {
  return (
    <Suspense fallback={<AdminLoadingState label="جاري تحميل المخزون…" />}>
      <InventoryListView />
    </Suspense>
  );
}
