import type { CatalogProductListItemDto } from "@/features/catalog";
import { ProductCard } from "./product-card";

type ProductGridProps = {
  products: CatalogProductListItemDto[];
};

export function ProductGrid({ products }: ProductGridProps) {
  return (
    <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:gap-5 lg:grid-cols-3 xl:grid-cols-4">
      {products.map((product) => (
        <li key={product.id}>
          <ProductCard product={product} />
        </li>
      ))}
    </ul>
  );
}
