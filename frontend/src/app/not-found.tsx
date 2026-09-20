import Link from "next/link";
import { brand } from "@/config/brand";

export default function NotFound() {
  return (
    <div className="mx-auto flex min-h-[70vh] w-full max-w-lg flex-col items-start justify-center gap-4 px-4 py-16 sm:px-6">
      <p className="text-sm text-muted-foreground">{brand.shortName}</p>
      <h1 className="text-2xl font-semibold text-foreground">الصفحة غير موجودة</h1>
      <p className="leading-7 text-muted-foreground">
        تعذر العثور على الصفحة أو المنتج أو التصنيف المطلوب. تحقق من الرابط أو عد
        إلى المتجر.
      </p>
      <div className="flex flex-wrap gap-3">
        <Link
          href="/"
          className="rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground hover:opacity-90"
        >
          الرئيسية
        </Link>
        <Link
          href="/categories"
          className="rounded-md border border-border px-4 py-2 text-sm text-foreground"
        >
          التصنيفات
        </Link>
        <Link
          href="/products"
          className="rounded-md border border-border px-4 py-2 text-sm text-foreground"
        >
          المنتجات
        </Link>
      </div>
    </div>
  );
}
