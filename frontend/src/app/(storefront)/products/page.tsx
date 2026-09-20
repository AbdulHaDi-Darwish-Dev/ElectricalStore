import type { Metadata } from "next";
import Link from "next/link";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import {
  getCategories,
  getProducts,
  parseProductListSearchParams,
} from "@/features/catalog";
import { EmptyState } from "@/components/storefront/empty-state";
import { ProductGrid } from "@/components/storefront/product-grid";
import { ProductSearchForm } from "@/components/storefront/product-search-form";

type ProductsPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export const metadata: Metadata = {
  title: "المنتجات",
  description: site.catalogDescription,
  alternates: {
    canonical: "/products",
  },
  openGraph: {
    title: `المنتجات | ${brand.name}`,
    description: site.catalogDescription,
  },
};

export default async function ProductsPage({ searchParams }: ProductsPageProps) {
  const rawParams = await searchParams;
  const filters = parseProductListSearchParams(rawParams);
  const [products, categories] = await Promise.all([
    getProducts(filters),
    getCategories(),
  ]);

  const activeCategory = filters.categoryId
    ? categories.find((category) => category.id === filters.categoryId)
    : undefined;

  const hasFilters = Boolean(filters.search || filters.categoryId);

  return (
    <div className="mx-auto w-full max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          المنتجات
        </h1>
        <p className="max-w-2xl text-muted-foreground">
          ابحث باسم المنتج أو صفّ حسب التصنيف. البحث يشمل اسم المنتج فقط.
        </p>
      </header>

      <div className="space-y-4 rounded-md border border-border bg-card p-4 sm:p-5">
        <ProductSearchForm
          initialSearch={filters.search ?? ""}
          categoryId={filters.categoryId}
        />

        {categories.length > 0 ? (
          <div className="space-y-2">
            <p className="text-sm text-muted-foreground">تصفية حسب التصنيف</p>
            <ul className="flex flex-wrap gap-2">
              <li>
                <FilterChip
                  href={
                    filters.search
                      ? `/products?search=${encodeURIComponent(filters.search)}`
                      : "/products"
                  }
                  label="الكل"
                  active={!filters.categoryId}
                />
              </li>
              {categories.map((category) => {
                const params = new URLSearchParams();
                params.set("categoryId", category.id);
                if (filters.search) {
                  params.set("search", filters.search);
                }
                return (
                  <li key={category.id}>
                    <FilterChip
                      href={`/products?${params.toString()}`}
                      label={category.name}
                      active={filters.categoryId === category.id}
                    />
                  </li>
                );
              })}
            </ul>
          </div>
        ) : null}
      </div>

      {hasFilters ? (
        <p className="text-sm text-muted-foreground">
          {filters.search ? (
            <>
              نتائج البحث عن «{filters.search}»
              {activeCategory ? ` ضمن ${activeCategory.name}` : null}
            </>
          ) : activeCategory ? (
            <>عرض منتجات تصنيف {activeCategory.name}</>
          ) : null}
        </p>
      ) : null}

      {products.length === 0 ? (
        <EmptyState
          title={
            filters.search
              ? "لا توجد نتائج مطابقة"
              : filters.categoryId
                ? "لا توجد منتجات في هذا التصنيف"
                : "لا توجد منتجات حالياً"
          }
          description={
            filters.search
              ? "جرّب كلمات أخرى أو أزل عوامل التصفية. البحث يشمل اسم المنتج فقط."
              : "لم تُنشر أي منتجات مطابقة للعرض العام حالياً."
          }
          actionHref="/products"
          actionLabel={hasFilters ? "مسح التصفية" : undefined}
        />
      ) : (
        <ProductGrid products={products} />
      )}
    </div>
  );
}

function FilterChip({
  href,
  label,
  active,
}: {
  href: string;
  label: string;
  active: boolean;
}) {
  return (
    <Link
      href={href}
      aria-current={active ? "true" : undefined}
      className={`inline-flex rounded-md px-3 py-1.5 text-sm transition ${
        active
          ? "bg-primary text-primary-foreground"
          : "bg-muted text-foreground hover:bg-accent"
      }`}
    >
      {label}
    </Link>
  );
}
