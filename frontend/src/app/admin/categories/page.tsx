import type { Metadata } from "next";
import { Suspense } from "react";
import { AdminLoadingState } from "@/components/admin";
import { CategoriesListView } from "@/components/admin-categories";

export const metadata: Metadata = {
  title: "التصنيفات",
  robots: { index: false, follow: false },
};

export default function AdminCategoriesPage() {
  return (
    <Suspense fallback={<AdminLoadingState label="جاري تحميل التصنيفات…" />}>
      <CategoriesListView />
    </Suspense>
  );
}
