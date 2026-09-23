import type { Metadata } from "next";
import { Suspense } from "react";
import { ResetPasswordForm } from "@/components/auth/reset-password-form";

export const metadata: Metadata = {
  title: "إعادة تعيين كلمة المرور",
  robots: { index: false, follow: false },
};

export default function ResetPasswordPage() {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-8 px-4 py-10 sm:px-6">
      <header className="space-y-2 text-center">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          إعادة تعيين كلمة المرور
        </h1>
        <p className="text-sm text-muted-foreground">
          اختر كلمة مرور جديدة لحسابك.
        </p>
      </header>
      <div className="rounded-md border border-border bg-card p-6 shadow-sm">
        <Suspense
          fallback={
            <p className="text-center text-sm text-muted-foreground">جاري التحميل…</p>
          }
        >
          <ResetPasswordForm />
        </Suspense>
      </div>
    </div>
  );
}
