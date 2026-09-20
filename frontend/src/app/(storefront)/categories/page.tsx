import type { Metadata } from "next";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import { getCategories } from "@/features/catalog";
import { CategoryGrid } from "@/components/storefront/category-grid";
import { EmptyState } from "@/components/storefront/empty-state";

export const metadata: Metadata = {
  title: "التصنيفات",
  description: site.catalogDescription,
  alternates: {
    canonical: "/categories",
  },
  openGraph: {
    title: `التصنيفات | ${brand.name}`,
    description: site.catalogDescription,
  },
};

export default async function CategoriesPage() {
  const categories = await getCategories();

  return (
    <div className="mx-auto w-full max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          التصنيفات
        </h1>
        <p className="max-w-2xl text-muted-foreground">
          تصفح التصنيفات المتاحة في المتجر.
        </p>
      </header>

      {categories.length === 0 ? (
        <EmptyState
          title="لا توجد تصنيفات حالياً"
          description="لم تُنشر أي تصنيفات للعرض العام بعد."
          actionHref="/products"
          actionLabel="تصفح المنتجات"
        />
      ) : (
        <CategoryGrid categories={categories} />
      )}
    </div>
  );
}
