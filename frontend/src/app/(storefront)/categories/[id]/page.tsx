import type { Metadata } from "next";
import Image from "next/image";
import { notFound } from "next/navigation";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import { getCategory, getProducts } from "@/features/catalog";
import { isNotFoundError } from "@/lib/api";
import { EmptyState } from "@/components/storefront/empty-state";
import { ProductGrid } from "@/components/storefront/product-grid";

type CategoryPageProps = {
  params: Promise<{ id: string }>;
};

export async function generateMetadata({
  params,
}: CategoryPageProps): Promise<Metadata> {
  const { id } = await params;

  try {
    const category = await getCategory(id);
    const description = category.description?.trim() || site.catalogDescription;

    return {
      title: category.name,
      description,
      alternates: {
        canonical: `/categories/${category.id}`,
      },
      openGraph: {
        title: `${category.name} | ${brand.name}`,
        description,
        ...(category.imageUrl
          ? { images: [{ url: category.imageUrl, alt: category.name }] }
          : {}),
      },
    };
  } catch (error) {
    if (isNotFoundError(error)) {
      return {
        title: "تصنيف غير موجود",
      };
    }
    throw error;
  }
}

export default async function CategoryDetailPage({ params }: CategoryPageProps) {
  const { id } = await params;

  let category;
  try {
    category = await getCategory(id);
  } catch (error) {
    if (isNotFoundError(error)) {
      notFound();
    }
    throw error;
  }

  const products = await getProducts({ categoryId: category.id });

  return (
    <div className="mx-auto w-full max-w-6xl space-y-10 px-4 py-10 sm:px-6">
      <header className="grid gap-6 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)] lg:items-start">
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">تصنيف</p>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
            {category.name}
          </h1>
          {category.description ? (
            <p className="max-w-2xl text-base leading-7 text-muted-foreground">
              {category.description}
            </p>
          ) : null}
        </div>
        {category.imageUrl ? (
          <div className="relative aspect-[16/10] overflow-hidden rounded-md bg-muted">
            <Image
              src={category.imageUrl}
              alt={category.name}
              fill
              priority
              sizes="(max-width: 1024px) 100vw, 40vw"
              className="object-cover"
            />
          </div>
        ) : null}
      </header>

      <section className="space-y-4">
        <h2 className="text-xl font-medium text-foreground">منتجات هذا التصنيف</h2>
        {products.length === 0 ? (
          <EmptyState
            title="لا توجد منتجات في هذا التصنيف"
            description="لا توجد منتجات ظاهرة للعرض العام ضمن هذا التصنيف حالياً."
            actionHref="/products"
            actionLabel="تصفح كل المنتجات"
          />
        ) : (
          <ProductGrid products={products} />
        )}
      </section>
    </div>
  );
}
