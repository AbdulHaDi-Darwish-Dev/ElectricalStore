import type { Metadata } from "next";
import { AuthGate } from "@/components/auth/auth-gate";
import { AccountProfileView } from "@/components/account/account-profile-view";

export const metadata: Metadata = {
  title: "حسابي",
  robots: { index: false, follow: false },
};

export default function AccountPage() {
  return (
    <AuthGate>
      <div className="mx-auto w-full max-w-3xl space-y-8 px-4 py-10 sm:px-6">
        <header className="space-y-2">
          <h1 className="text-3xl font-semibold tracking-tight">حسابي</h1>
          <p className="text-sm text-muted-foreground">
            إدارة بياناتك الشخصية وأمان الحساب وطلباتك.
          </p>
        </header>
        <AccountProfileView />
      </div>
    </AuthGate>
  );
}
