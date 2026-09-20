import type { Metadata } from "next";
import { CategoryCreateView } from "@/components/admin-categories";

export const metadata: Metadata = {
  title: "تصنيف جديد",
  robots: { index: false, follow: false },
};

export default function AdminCategoryCreatePage() {
  return <CategoryCreateView />;
}
