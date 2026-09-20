import type { Metadata } from "next";
import { RegisterForm } from "@/components/auth/register-form";

export const metadata: Metadata = {
  title: "إنشاء حساب",
  robots: { index: false, follow: false },
};

export default function RegisterPage() {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-8 px-4 py-10 sm:px-6">
      <header className="space-y-2 text-center">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          إنشاء حساب
        </h1>
        <p className="text-sm text-muted-foreground">
          أنشئ حساباً جديداً ببياناتك الأساسية فقط.
        </p>
      </header>
      <div className="rounded-md border border-border bg-card p-6 shadow-sm">
        <RegisterForm />
      </div>
    </div>
  );
}
