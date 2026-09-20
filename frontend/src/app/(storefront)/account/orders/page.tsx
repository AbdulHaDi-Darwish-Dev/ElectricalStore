import type { Metadata } from "next";
import { AuthGate } from "@/components/auth/auth-gate";
import { MyOrdersView } from "@/components/account/my-orders-view";

export const metadata: Metadata = {
  title: "طلباتي",
  robots: { index: false, follow: false },
};

export default function AccountOrdersPage() {
  return (
    <AuthGate>
      <div className="mx-auto w-full max-w-3xl space-y-6 px-4 py-10 sm:px-6">
        <header className="space-y-2">
          <h1 className="text-3xl font-semibold tracking-tight">طلباتي</h1>
          <p className="text-sm text-muted-foreground">
            قائمة طلباتك المرتبطة بحسابك.
          </p>
        </header>
        <MyOrdersView />
      </div>
    </AuthGate>
  );
}
