import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import {
  buildProductJsonLd,
  getPrimaryProductImage,
  getProduct,
  serializeJsonLd,
} from "@/features/catalog";
import { isNotFoundError } from "@/lib/api";
import { ProductGallery } from "@/components/storefront/product-gallery";
import { ProductVariants } from "@/components/storefront/product-variants";

type ProductPageProps = {
  params: Promise<{ id: string }>;
};

export async function generateMetadata({
  params,
}: ProductPageProps): Promise<Metadata> {
  const { id } = await params;

  try {
    const product = await getProduct(id);
    const description = product.description?.trim() || site.catalogDescription;
    const primaryImage = getPrimaryProductImage(product.images);

    return {
      title: product.name,
      description,
      alternates: {
        canonical: `/products/${product.id}`,
      },
      openGraph: {
        title: `${product.name} | ${brand.name}`,
        description,
        ...(primaryImage
          ? { images: [{ url: primaryImage.url, alt: product.name }] }
          : {}),
      },
    };
  } catch (error) {
    if (isNotFoundError(error)) {
      return {
        title: "منتج غير موجود",
      };
    }
    throw error;
  }
}

export default async function ProductDetailPage({ params }: ProductPageProps) {
  const { id } = await params;

  let product;
  try {
    product = await getProduct(id);
  } catch (error) {
    if (isNotFoundError(error)) {
      notFound();
    }
    throw error;
  }

  const productUrl = `${site.url}/products/${product.id}`;
  const jsonLd = buildProductJsonLd(product, productUrl);

  return (
    <div className="mx-auto w-full max-w-6xl space-y-10 px-4 py-10 sm:px-6">
      {jsonLd ? (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: serializeJsonLd(jsonLd) }}
        />
      ) : null}

      <nav aria-label="مسار التصفح" className="text-sm text-muted-foreground">
        <ol className="flex flex-wrap items-center gap-2">
          <li>
            <Link href="/products" className="hover:text-primary">
              المنتجات
            </Link>
          </li>
          <li aria-hidden>/</li>
          <li>
            <Link
              href={`/categories/${product.categoryId}`}
              className="hover:text-primary"
            >
              {product.categoryName}
            </Link>
          </li>
          <li aria-hidden>/</li>
          <li className="text-foreground">{product.name}</li>
        </ol>
      </nav>

      <div className="grid gap-10 lg:grid-cols-2 lg:items-start">
        <ProductGallery images={product.images} productName={product.name} />

        <div className="space-y-6">
          <header className="space-y-3">
            <p className="text-sm text-muted-foreground">
              <Link
                href={`/categories/${product.categoryId}`}
                className="hover:text-primary"
              >
                {product.categoryName}
              </Link>
            </p>
            <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
              {product.name}
            </h1>
            {product.description ? (
              <p className="text-base leading-7 text-muted-foreground">
                {product.description}
              </p>
            ) : null}
          </header>

          <ProductVariants
            productId={product.id}
            productName={product.name}
            primaryImageUrl={getPrimaryProductImage(product.images)?.url ?? null}
            variants={product.variants}
          />
        </div>
      </div>
    </div>
  );
}
