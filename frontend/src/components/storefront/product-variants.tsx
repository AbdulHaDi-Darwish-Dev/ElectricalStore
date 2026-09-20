import type { CatalogProductVariantDto } from "@/features/catalog";
import { formatPrice, formatQuantityIncrement, formatSellingUnit } from "@/lib/format";
import { StockBadge } from "./stock-badge";

type ProductVariantsProps = {
  variants: CatalogProductVariantDto[];
};

export function ProductVariants({ variants }: ProductVariantsProps) {
  const active = variants.filter((variant) => variant.isActive);

  if (active.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        لا توجد متغيرات معروضة لهذا المنتج حالياً.
      </p>
    );
  }

  return (
    <div className="space-y-3">
      <h2 className="text-lg font-medium text-foreground">المتغيرات</h2>
      <ul className="divide-y divide-border rounded-md border border-border bg-card">
        {active.map((variant) => (
          <li key={variant.id} className="flex flex-col gap-2 px-4 py-4 sm:px-5">
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <h3 className="font-medium text-foreground">{variant.name}</h3>
              <p className="text-base font-semibold text-foreground">
                {formatPrice(variant.price)}
              </p>
            </div>
            <dl className="grid grid-cols-1 gap-1 text-sm text-muted-foreground sm:grid-cols-2">
              <div className="flex gap-2">
                <dt>رمز التخزين:</dt>
                <dd className="text-foreground">{variant.sku}</dd>
              </div>
              <div className="flex gap-2">
                <dt>وحدة البيع:</dt>
                <dd className="text-foreground">
                  {formatSellingUnit(variant.sellingUnit)}
                </dd>
              </div>
              <div className="flex gap-2 sm:col-span-2">
                <dt className="sr-only">زيادة الكمية</dt>
                <dd>{formatQuantityIncrement(variant.quantityIncrement, variant.sellingUnit)}</dd>
              </div>
            </dl>
            <StockBadge
              inStock={variant.isInStock}
              availableQuantity={variant.availableQuantity}
            />
          </li>
        ))}
      </ul>
    </div>
  );
}
