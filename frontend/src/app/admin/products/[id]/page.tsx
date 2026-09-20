import type { Metadata } from "next";
import { ProductEditView } from "@/components/admin-products";

export const metadata: Metadata = {
  title: "إدارة المنتج",
  robots: { index: false, follow: false },
};

type AdminProductEditPageProps = {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ status?: string }>;
};

export default async function AdminProductEditPage({
  params,
  searchParams,
}: AdminProductEditPageProps) {
  const { id } = await params;
  const { status } = await searchParams;
  return <ProductEditView productId={id} initialStatus={status ?? null} />;
}
