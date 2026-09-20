import type { Metadata } from "next";
import { Suspense } from "react";
import { LoginForm } from "@/components/auth/login-form";

export const metadata: Metadata = {
  title: "تسجيل الدخول",
  robots: { index: false, follow: false },
};

export default function LoginPage() {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-8 px-4 py-10 sm:px-6">
      <header className="space-y-2 text-center">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">
          تسجيل الدخول
        </h1>
        <p className="text-sm text-muted-foreground">
          ادخل إلى حسابك لمتابعة طلباتك وإتمام الشراء بثقة.
        </p>
      </header>
      <div className="rounded-md border border-border bg-card p-6 shadow-sm">
        <Suspense fallback={<div className="h-40 animate-pulse rounded bg-muted" />}>
          <LoginForm />
        </Suspense>
      </div>
    </div>
  );
}
