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
      <section className="relative overflow-hidden border-b border-border bg-secondary/60">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_120%_90%_at_70%_-10%,_color-mix(in_oklab,var(--primary)_18%,transparent)_0%,_transparent_58%)]"
        />
        <div className="volt-line hidden sm:block" aria-hidden />
        <div className="relative mx-auto flex w-full max-w-6xl flex-col justify-center gap-5 px-4 py-16 sm:gap-6 sm:px-6 sm:py-20 lg:py-24">
          <p className="text-sm font-semibold tracking-[0.14em] text-primary">
            {brand.nameEn}
          </p>
          <h1 className="max-w-3xl text-4xl font-semibold leading-[1.15] tracking-tight text-foreground sm:text-5xl lg:text-[3.25rem]">
            {brand.name}
          </h1>
          <p className="max-w-xl text-base leading-7 text-muted-foreground sm:text-lg sm:leading-8">
            {brand.description}
          </p>
          <div className="flex flex-wrap items-center gap-3 pt-1">
            <Link
              href="/categories"
              className="inline-flex h-11 items-center justify-center rounded-md bg-primary px-6 text-sm font-medium text-primary-foreground transition hover:opacity-95 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              تصفح التصنيفات
            </Link>
            <Link
              href="/products"
              className="inline-flex h-11 items-center justify-center rounded-md border border-border bg-card px-6 text-sm font-medium text-foreground transition hover:border-primary/35 hover:bg-muted/50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              عرض المنتجات
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-12 sm:space-y-7 sm:px-6 sm:py-14">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div className="space-y-1.5">
            <h2 className="text-2xl font-semibold tracking-tight text-foreground">
              التصنيفات
            </h2>
            <p className="text-sm leading-6 text-muted-foreground">
              ابدأ من التصنيف المناسب لما تبحث عنه.
            </p>
          </div>
          <Link
            href="/categories"
            className="text-sm font-medium text-primary transition hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
          >
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

      <section className="border-t border-border bg-card/40">
        <div className="mx-auto w-full max-w-6xl space-y-6 px-4 py-12 sm:space-y-7 sm:px-6 sm:py-14">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div className="space-y-1.5">
              <h2 className="text-2xl font-semibold tracking-tight text-foreground">
                منتجات للتصفح
              </h2>
              <p className="text-sm leading-6 text-muted-foreground">
                عيّنة من المنتجات المتاحة في الكتالوج.
              </p>
            </div>
            <Link
              href="/products"
              className="text-sm font-medium text-primary transition hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
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
        </div>
      </section>
    </>
  );
}
