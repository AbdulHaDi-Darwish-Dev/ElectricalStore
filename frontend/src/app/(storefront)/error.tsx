"use client";

import Link from "next/link";
import { brand } from "@/config/brand";

type ErrorProps = {
  error: Error & { digest?: string };
  reset: () => void;
};

export default function StorefrontError({ error, reset }: ErrorProps) {
  return (
    <div className="mx-auto flex w-full max-w-lg flex-col gap-4 px-4 py-16 sm:px-6">
      <p className="text-sm text-muted-foreground">{brand.shortName}</p>
      <h1 className="text-2xl font-semibold text-foreground">تعذر تحميل الصفحة</h1>
      <p className="text-muted-foreground">
        حدث خطأ أثناء جلب البيانات. يمكنك المحاولة مرة أخرى أو العودة إلى المتجر.
      </p>
      {process.env.NODE_ENV === "development" && error.message ? (
        <p className="rounded-md bg-muted px-3 py-2 text-xs text-muted-foreground">
          {error.message}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-3">
        <button
          type="button"
          onClick={reset}
          className="rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground"
        >
          إعادة المحاولة
        </button>
        <Link
          href="/"
          className="rounded-md border border-border px-4 py-2 text-sm text-foreground"
        >
          الرئيسية
        </Link>
      </div>
    </div>
  );
}
