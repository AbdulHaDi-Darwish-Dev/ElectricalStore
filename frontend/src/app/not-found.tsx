import Link from "next/link";
import { brand } from "@/config/brand";

export default function NotFound() {
  return (
    <div className="mx-auto flex min-h-full w-full max-w-lg flex-col items-start justify-center gap-4 px-4 py-16">
      <p className="text-sm text-muted-foreground">{brand.shortName}</p>
      <h1 className="text-2xl font-semibold text-foreground">الصفحة غير موجودة</h1>
      <p className="text-muted-foreground">
        تعذر العثور على الصفحة المطلوبة. تحقق من الرابط أو عد إلى الصفحة الرئيسية.
      </p>
      <Link
        href="/"
        className="rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground hover:opacity-90"
      >
        العودة إلى الرئيسية
      </Link>
    </div>
  );
}
