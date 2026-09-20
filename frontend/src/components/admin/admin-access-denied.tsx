import Link from "next/link";

type AdminAccessDeniedProps = {
  title?: string;
  message?: string;
  showStorefrontLink?: boolean;
};

export function AdminAccessDenied({
  title = "غير مصرح",
  message = "ليس لديك صلاحية للوصول إلى هذا الجزء من لوحة الإدارة. إن كنت تعتقد أن هذا خطأ، تواصل مع صاحب الصلاحيات.",
  showStorefrontLink = true,
}: AdminAccessDeniedProps) {
  return (
    <div
      className="mx-auto flex w-full max-w-lg flex-col gap-4 rounded-md border border-border bg-card px-5 py-8"
      role="alert"
    >
      <div className="space-y-2">
        <p className="text-sm font-medium text-accent-foreground">رفض الوصول</p>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">
          {title}
        </h1>
        <p className="text-sm leading-7 text-muted-foreground">{message}</p>
      </div>
      <div className="flex flex-wrap gap-3 text-sm">
        <Link href="/admin" className="text-primary hover:underline">
          العودة إلى لوحة التحكم
        </Link>
        {showStorefrontLink ? (
          <Link href="/" className="text-primary hover:underline">
            المتجر
          </Link>
        ) : null}
      </div>
    </div>
  );
}
