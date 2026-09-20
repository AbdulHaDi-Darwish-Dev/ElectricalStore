import Link from "next/link";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import { getCategories, getProducts } from "@/features/catalog";
import { CategoryGrid } from "@/components/storefront/category-grid";
import { EmptyState } from "@/components/storefront/empty-state";
import { ProductGrid } from "@/components/storefront/product-grid";

export const metadata = {
  title: {
    absolute: brand.name,
  },
  description: site.catalogDescription,
  alternates: {
    canonical: "/",
  },
};

const HOME_PRODUCT_LIMIT = 8;

export default async function StorefrontHomePage() {
  const [categories, products] = await Promise.all([
    getCategories(),
    getProducts(),
  ]);

  const featuredProducts = products.slice(0, HOME_PRODUCT_LIMIT);

  return (
    <>
      <section className="relative overflow-hidden border-b border-border">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--accent)_0%,_transparent_55%),linear-gradient(180deg,_var(--secondary)_0%,_var(--background)_100%)]"
        />
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-[0.35] [background-image:linear-gradient(var(--border)_1px,transparent_1px),linear-gradient(90deg,var(--border)_1px,transparent_1px)] [background-size:48px_48px] [mask-image:linear-gradient(to_bottom,black,transparent)]"
        />
        <div className="relative mx-auto flex min-h-[70vh] w-full max-w-6xl flex-col justify-end gap-6 px-4 pb-16 pt-20 sm:px-6 sm:pb-20 sm:pt-28">
          <p className="text-sm font-medium tracking-wide text-accent-foreground">
            {brand.shortName}
          </p>
          <h1 className="max-w-3xl text-4xl font-semibold leading-tight tracking-tight text-foreground sm:text-5xl lg:text-6xl">
            {brand.name}
          </h1>
          <p className="max-w-xl text-base leading-7 text-muted-foreground sm:text-lg">
            تصفح التصنيفات والمنتجات المتاحة في المتجر.
          </p>
          <div className="flex flex-wrap gap-3">
            <Link
              href="/categories"
              className="rounded-md bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-90"
            >
              التصنيفات
            </Link>
            <Link
              href="/products"
              className="rounded-md border border-border bg-card px-5 py-2.5 text-sm font-medium text-foreground hover:border-primary/40"
            >
              المنتجات
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-12 sm:px-6">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div className="space-y-1">
            <h2 className="text-2xl font-semibold text-foreground">التصنيفات</h2>
            <p className="text-sm text-muted-foreground">
              ابدأ من التصنيف المناسب لما تبحث عنه.
            </p>
          </div>
          <Link href="/categories" className="text-sm text-primary hover:underline">
            عرض كل التصنيفات
          </Link>
        </div>
        {categories.length === 0 ? (
          <EmptyState
            title="لا توجد تصنيفات حالياً"
            description="لم تُنشر أي تصنيفات للعرض العام بعد."
          />
        ) : (
          <CategoryGrid categories={categories.slice(0, 6)} />
        )}
      </section>

      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 pb-16 sm:px-6">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div className="space-y-1">
            <h2 className="text-2xl font-semibold text-foreground">منتجات للتصفح</h2>
            <p className="text-sm text-muted-foreground">
              عيّنة من المنتجات المتاحة في الكتالوج.
            </p>
          </div>
          <Link href="/products" className="text-sm text-primary hover:underline">
            عرض كل المنتجات
          </Link>
        </div>
        {featuredProducts.length === 0 ? (
          <EmptyState
            title="لا توجد منتجات حالياً"
            description="لم تُنشر أي منتجات للعرض العام بعد."
            actionHref="/categories"
            actionLabel="تصفح التصنيفات"
          />
        ) : (
          <ProductGrid products={featuredProducts} />
        )}
      </section>
    </>
  );
}
