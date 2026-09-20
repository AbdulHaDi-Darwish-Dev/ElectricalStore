import type { Metadata } from "next";
import { ProductCreateView } from "@/components/admin-products";

export const metadata: Metadata = {
  title: "منتج جديد",
  robots: { index: false, follow: false },
};

export default function AdminProductCreatePage() {
  return <ProductCreateView />;
}
