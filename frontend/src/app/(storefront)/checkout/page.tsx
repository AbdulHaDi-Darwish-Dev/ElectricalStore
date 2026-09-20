import type { Metadata } from "next";
import { brand } from "@/config/brand";
import { CheckoutView } from "@/components/checkout/checkout-view";

export const metadata: Metadata = {
  title: "إتمام الطلب",
  description: `إتمام الطلب كضيف في ${brand.name}. الدفع عند الاستلام.`,
  robots: { index: false, follow: false },
};

export default function CheckoutPage() {
  return (
    <div className="mx-auto w-full max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          إتمام الطلب
        </h1>
        <p className="max-w-2xl text-muted-foreground">
          راجع الملخص وأدخل بيانات التوصيل. الأسعار النهائية تُحسب عبر الخادم قبل
          التأكيد.
        </p>
      </header>

      <CheckoutView />
    </div>
  );
}
