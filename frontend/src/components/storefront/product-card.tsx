import Image from "next/image";
import Link from "next/link";
import type { CatalogProductListItemDto } from "@/features/catalog";
import { formatFromPrice } from "@/lib/format";
import { StockBadge } from "./stock-badge";

type ProductCardProps = {
  product: CatalogProductListItemDto;
};

export function ProductCard({ product }: ProductCardProps) {
  return (
    <Link
      href={`/products/${product.id}`}
      className="group flex h-full flex-col overflow-hidden rounded-lg border border-border bg-card shadow-[var(--shadow-card-sm)] transition duration-200 hover:border-primary/30 hover:shadow-[var(--shadow-card)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
    >
      <div className="relative aspect-[4/3] w-full overflow-hidden bg-muted">
        {product.primaryImageUrl ? (
          <Image
            src={product.primaryImageUrl}
            alt={product.name}
            fill
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 25vw"
            className="object-cover transition duration-300 group-hover:scale-[1.03]"
          />
        ) : (
          <div
            aria-hidden
            className="flex h-full items-center justify-center text-sm text-muted-foreground"
          >
            بدون صورة
          </div>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-2.5 p-4 sm:p-5">
        <p className="text-xs font-medium tracking-wide text-muted-foreground">
          {product.categoryName}
        </p>
        <h3 className="text-base font-semibold leading-6 text-foreground transition group-hover:text-primary">
          {product.name}
        </h3>
        <div className="mt-auto space-y-2 pt-1">
          <p className="text-lg font-semibold tracking-tight text-foreground tabular-nums">
            {formatFromPrice(product.fromPrice)}
          </p>
          <StockBadge inStock={product.hasInStock} />
        </div>
      </div>
    </Link>
  );
}
