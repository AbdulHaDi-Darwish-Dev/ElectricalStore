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
      className="group flex h-full flex-col overflow-hidden rounded-md border border-border bg-card transition hover:border-primary/40 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
    >
      <div className="relative aspect-[4/3] w-full bg-muted">
        {product.primaryImageUrl ? (
          <Image
            src={product.primaryImageUrl}
            alt={product.name}
            fill
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 25vw"
            className="object-cover transition duration-300 group-hover:scale-[1.02]"
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
      <div className="flex flex-1 flex-col gap-2 p-4">
        <p className="text-xs text-muted-foreground">{product.categoryName}</p>
        <h3 className="text-base font-medium leading-6 text-foreground group-hover:text-primary">
          {product.name}
        </h3>
        <p className="mt-auto text-sm font-semibold text-foreground">
          {formatFromPrice(product.fromPrice)}
        </p>
        <StockBadge inStock={product.hasInStock} />
      </div>
    </Link>
  );
}
