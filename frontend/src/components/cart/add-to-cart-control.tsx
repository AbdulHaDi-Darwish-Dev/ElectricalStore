"use client";

import { useState } from "react";
import type { CatalogProductVariantDto } from "@/features/catalog";
import {
  defaultQuantity,
  useCartStore,
  type AddToCartFailureReason,
} from "@/features/cart";
import { formatPrice, formatQuantityIncrement, formatSellingUnit } from "@/lib/format";
import { StockBadge } from "@/components/storefront/stock-badge";
import { QuantityStepper } from "./quantity-stepper";

type AddToCartControlProps = {
  productId: string;
  productName: string;
  primaryImageUrl: string | null;
  variant: CatalogProductVariantDto;
};

const FAILURE_MESSAGES: Record<AddToCartFailureReason, string> = {
  out_of_stock: "هذا المتغير غير متوفر حالياً.",
  invalid_quantity: "الكمية غير صالحة لهذه وحدة البيع.",
  exceeds_available: "الكمية المطلوبة تتجاوز الكمية المتاحة حالياً.",
};

export function AddToCartControl({
  productId,
  productName,
  primaryImageUrl,
  variant,
}: AddToCartControlProps) {
  const addItem = useCartStore((state) => state.addItem);
  const [quantity, setQuantity] = useState(() =>
    defaultQuantity(variant.quantityIncrement),
  );
  const [message, setMessage] = useState<string | null>(null);
  const [messageTone, setMessageTone] = useState<"success" | "error">("success");

  const outOfStock = !variant.isInStock;

  function onAdd() {
    const result = addItem({
      productId,
      productName,
      primaryImageUrl,
      variantId: variant.id,
      variantName: variant.name,
      sku: variant.sku,
      sellingUnit: variant.sellingUnit,
      quantityIncrement: variant.quantityIncrement,
      quantity,
      unitPrice: variant.price,
      isInStock: variant.isInStock,
      availableQuantity: variant.availableQuantity,
    });

    if (result.ok) {
      setMessageTone("success");
      setMessage("تمت الإضافة إلى السلة.");
      return;
    }

    setMessageTone("error");
    setMessage(FAILURE_MESSAGES[result.reason]);
  }

  return (
    <div className="mt-3 space-y-3 border-t border-border/80 pt-3">
      <StockBadge
        inStock={variant.isInStock}
        availableQuantity={variant.availableQuantity}
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
        <div className="space-y-1">
          <p className="text-xs text-muted-foreground">
            {formatQuantityIncrement(variant.quantityIncrement, variant.sellingUnit)}
          </p>
          <QuantityStepper
            label={variant.name}
            value={Number.isFinite(quantity) ? quantity : variant.quantityIncrement}
            increment={variant.quantityIncrement}
            sellingUnit={variant.sellingUnit}
            onChange={setQuantity}
            disabled={outOfStock}
          />
        </div>

        <button
          type="button"
          disabled={outOfStock}
          onClick={onAdd}
          aria-disabled={outOfStock}
          className="inline-flex h-11 items-center justify-center rounded-md bg-primary px-5 text-sm font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:bg-muted disabled:text-muted-foreground"
        >
          {outOfStock ? "غير متوفر للإضافة" : "أضف إلى السلة"}
        </button>
      </div>

      <p className="text-xs text-muted-foreground">
        السعر المعروض {formatPrice(variant.price)} /{" "}
        {formatSellingUnit(variant.sellingUnit)} — تقديري حتى إتمام الطلب.
      </p>

      {message ? (
        <p
          role="status"
          aria-live="polite"
          className={
            messageTone === "success"
              ? "text-sm font-medium text-success-foreground"
              : "text-sm font-medium text-destructive"
          }
        >
          {message}
        </p>
      ) : null}
    </div>
  );
}
