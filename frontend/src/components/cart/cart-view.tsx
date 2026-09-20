"use client";

import Image from "next/image";
import Link from "next/link";
import {
  estimatedLineTotal,
  estimatedMerchandiseSubtotal,
  useCartStore,
  type CartItem,
} from "@/features/cart";
import { formatPrice, formatSellingUnit } from "@/lib/format";
import { EmptyState } from "@/components/storefront/empty-state";
import { QuantityStepper } from "./quantity-stepper";

export function CartView() {
  const hasHydrated = useCartStore((state) => state.hasHydrated);
  const items = useCartStore((state) => state.items);
  const setQuantity = useCartStore((state) => state.setQuantity);
  const removeItem = useCartStore((state) => state.removeItem);
  const clearCart = useCartStore((state) => state.clearCart);

  if (!hasHydrated) {
    return (
      <div className="space-y-4" aria-busy="true" aria-live="polite">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <div className="h-32 animate-pulse rounded-md bg-muted" />
        <div className="h-32 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <EmptyState
        title="سلة التسوق فارغة"
        description="لم تُضف أي منتجات بعد. تصفح الكتالوج لإضافة أصناف إلى السلة."
        actionHref="/products"
        actionLabel="تصفح المنتجات"
      />
    );
  }

  const subtotal = estimatedMerchandiseSubtotal(items);

  return (
    <div className="space-y-8">
      <ul className="space-y-4">
        {items.map((item) => (
          <li key={item.variantId}>
            <CartLine
              item={item}
              onQuantityChange={(quantity) => setQuantity(item.variantId, quantity)}
              onRemove={() => removeItem(item.variantId)}
            />
          </li>
        ))}
      </ul>

      <section className="rounded-md border border-border bg-card p-5 sm:p-6">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div className="space-y-1">
            <h2 className="text-lg font-medium text-foreground">
              المجموع التقديري للبضاعة
            </h2>
            <p className="max-w-md text-sm leading-6 text-muted-foreground">
              الأسعار تقديرية من آخر بيانات معروفة. سيتم التحقق من السعر والتوفر عند
              إتمام الطلب.
            </p>
          </div>
          <p className="text-2xl font-semibold tabular-nums text-foreground">
            {formatPrice(subtotal)}
          </p>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
          <Link
            href="/products"
            className="inline-flex h-11 items-center justify-center rounded-md border border-border px-5 text-sm text-foreground transition hover:border-primary/40"
          >
            متابعة التسوق
          </Link>
          <button
            type="button"
            onClick={clearCart}
            className="inline-flex h-11 items-center justify-center rounded-md px-5 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
          >
            تفريغ السلة
          </button>
        </div>

        <p className="mt-5 rounded-md bg-muted/60 px-4 py-3 text-sm leading-6 text-muted-foreground">
          إتمام الطلب سيكون متاحاً في المرحلة التالية بعد التحقق من الأسعار والمخزون
          والشحن عبر الخادم.
        </p>
      </section>
    </div>
  );
}

function CartLine({
  item,
  onQuantityChange,
  onRemove,
}: {
  item: CartItem;
  onQuantityChange: (quantity: number) => void;
  onRemove: () => void;
}) {
  const lineTotal = estimatedLineTotal(item.lastKnownUnitPrice, item.quantity);

  return (
    <article className="grid gap-4 rounded-md border border-border bg-card p-4 sm:grid-cols-[6.5rem_minmax(0,1fr)] sm:p-5">
      <Link
        href={`/products/${item.productId}`}
        className="relative aspect-square overflow-hidden rounded-md bg-muted sm:aspect-auto sm:h-28 sm:w-[6.5rem]"
      >
        {item.primaryImageUrl ? (
          <Image
            src={item.primaryImageUrl}
            alt={item.productName}
            fill
            sizes="112px"
            className="object-cover"
          />
        ) : (
          <span className="flex h-full items-center justify-center text-xs text-muted-foreground">
            بدون صورة
          </span>
        )}
      </Link>

      <div className="flex min-w-0 flex-col gap-3">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0 space-y-1">
            <h2 className="text-base font-medium leading-6 text-foreground">
              <Link
                href={`/products/${item.productId}`}
                className="hover:text-primary"
              >
                {item.productName}
              </Link>
            </h2>
            <p className="text-sm text-muted-foreground">{item.variantName}</p>
            <p className="text-xs text-muted-foreground">
              {item.sku} · {formatSellingUnit(item.sellingUnit)}
            </p>
          </div>
          <div className="text-start sm:text-end">
            <p className="text-sm text-muted-foreground">سعر تقديري للوحدة</p>
            <p className="font-medium tabular-nums text-foreground">
              {formatPrice(item.lastKnownUnitPrice)}
            </p>
          </div>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
          <QuantityStepper
            label={`${item.productName} — ${item.variantName}`}
            value={item.quantity}
            increment={item.quantityIncrement}
            sellingUnit={item.sellingUnit}
            onChange={onQuantityChange}
          />
          <div className="flex flex-wrap items-center gap-4">
            <p className="text-sm">
              <span className="text-muted-foreground">الإجمالي التقديري: </span>
              <span className="font-semibold tabular-nums text-foreground">
                {formatPrice(lineTotal)}
              </span>
            </p>
            <button
              type="button"
              onClick={onRemove}
              aria-label={`إزالة ${item.productName} — ${item.variantName} من السلة`}
              className="text-sm text-muted-foreground underline-offset-2 transition hover:text-destructive hover:underline"
            >
              إزالة
            </button>
          </div>
        </div>
      </div>
    </article>
  );
}
