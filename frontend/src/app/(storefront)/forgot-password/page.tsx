import type { Metadata } from "next";
import { ForgotPasswordForm } from "@/components/auth/forgot-password-form";

export const metadata: Metadata = {
  title: "نسيت كلمة المرور",
  robots: { index: false, follow: false },
};

export default function ForgotPasswordPage() {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-8 px-4 py-10 sm:px-6">
      <header className="space-y-2 text-center">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          نسيت كلمة المرور؟
        </h1>
        <p className="text-sm text-muted-foreground">
          أدخل بريدك الإلكتروني وسنرسل رابط إعادة التعيين إن وُجد حساب مؤهل.
        </p>
      </header>
      <div className="rounded-md border border-border bg-card p-6 shadow-sm">
        <ForgotPasswordForm />
      </div>
    </div>
  );
}
