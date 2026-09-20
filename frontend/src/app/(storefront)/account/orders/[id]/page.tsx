import type { Metadata } from "next";
import { AuthGate } from "@/components/auth/auth-gate";
import { MyOrderDetailView } from "@/components/account/my-order-detail-view";

export const metadata: Metadata = {
  title: "تفاصيل الطلب",
  robots: { index: false, follow: false },
};

export default function AccountOrderDetailPage() {
  return (
    <AuthGate>
      <div className="mx-auto w-full max-w-3xl px-4 py-10 sm:px-6">
        <MyOrderDetailView />
      </div>
    </AuthGate>
  );
}
