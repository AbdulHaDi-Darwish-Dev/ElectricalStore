"use client";

import "./globals.css";
import { brand } from "@/config/brand";

type GlobalErrorProps = {
  error: Error & { digest?: string };
  reset: () => void;
};

/**
 * Root error UI. Must define its own html/body because it replaces the root layout.
 */
export default function GlobalError({ error, reset }: GlobalErrorProps) {
  return (
    <html lang="ar" dir="rtl">
      <body className="mx-auto flex min-h-full max-w-lg flex-col justify-center gap-4 bg-background px-4 py-16 font-sans text-foreground antialiased">
        <p className="text-sm text-muted-foreground">{brand.shortName}</p>
        <h1 className="text-2xl font-semibold">حدث خطأ غير متوقع</h1>
        <p className="text-muted-foreground">
          تعذر إكمال الطلب. يمكنك المحاولة مرة أخرى.
        </p>
        {error.digest ? (
          <p className="text-xs text-muted-foreground">رمز مرجعي: {error.digest}</p>
        ) : null}
        <button
          type="button"
          onClick={reset}
          className="w-fit rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground"
        >
          إعادة المحاولة
        </button>
      </body>
    </html>
  );
}
