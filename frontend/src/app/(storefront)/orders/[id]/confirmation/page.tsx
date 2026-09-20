import type { Metadata } from "next";
import { OrderConfirmationView } from "@/components/orders/order-confirmation-view";

export const metadata: Metadata = {
  title: "تأكيد الطلب",
  robots: { index: false, follow: false },
};

type ConfirmationPageProps = {
  params: Promise<{ id: string }>;
};

export default async function OrderConfirmationPage({
  params,
}: ConfirmationPageProps) {
  await params;

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-10 sm:px-6">
      <OrderConfirmationView />
    </div>
  );
}
