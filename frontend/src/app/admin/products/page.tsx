import type { Metadata } from "next";
import { Suspense } from "react";
import { AdminLoadingState } from "@/components/admin";
import { ProductsListView } from "@/components/admin-products";

export const metadata: Metadata = {
  title: "المنتجات",
  robots: { index: false, follow: false },
};

export default function AdminProductsPage() {
  return (
    <Suspense fallback={<AdminLoadingState label="جاري تحميل المنتجات…" />}>
      <ProductsListView />
    </Suspense>
  );
}
