import type { Metadata } from "next";
import { CategoryEditView } from "@/components/admin-categories";

export const metadata: Metadata = {
  title: "تعديل التصنيف",
  robots: { index: false, follow: false },
};

type AdminCategoryEditPageProps = {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ status?: string }>;
};

export default async function AdminCategoryEditPage({
  params,
  searchParams,
}: AdminCategoryEditPageProps) {
  const { id } = await params;
  const { status } = await searchParams;
  return <CategoryEditView categoryId={id} initialStatus={status ?? null} />;
}
