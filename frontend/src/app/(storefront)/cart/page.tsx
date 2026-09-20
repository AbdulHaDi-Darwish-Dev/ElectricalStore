import type { Metadata } from "next";
import { brand } from "@/config/brand";
import { CartView } from "@/components/cart/cart-view";

export const metadata: Metadata = {
  title: "سلة التسوق",
  description: `سلة التسوق في ${brand.name}. الأسعار تقديرية حتى إتمام الطلب.`,
  robots: {
    index: false,
    follow: false,
  },
};

export default function CartPage() {
  return (
    <div className="mx-auto w-full max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          سلة التسوق
        </h1>
        <p className="max-w-2xl text-muted-foreground">
          راجع الأصناف والكميات قبل المتابعة. سيتم التحقق من السعر والتوفر عند إتمام
          الطلب.
        </p>
      </header>

      <CartView />
    </div>
  );
}
