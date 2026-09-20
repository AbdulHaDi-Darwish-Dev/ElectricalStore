import type { Metadata } from "next";
import Link from "next/link";
import { AuthGate } from "@/components/auth/auth-gate";

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
            منطقة العميل المحمية. لا نعرض بريداً أو اسماً لأن واجهة الحساب الحالية
            لا توفّرهما.
          </p>
        </header>

        <section className="rounded-md border border-border bg-card p-6">
          <h2 className="text-lg font-medium">طلباتي</h2>
          <p className="mt-2 text-sm text-muted-foreground">
            تصفّح طلباتك السابقة وتفاصيل كل طلب.
          </p>
          <Link
            href="/account/orders"
            className="mt-4 inline-flex rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
          >
            عرض طلباتي
          </Link>
        </section>
      </div>
    </AuthGate>
  );
}
