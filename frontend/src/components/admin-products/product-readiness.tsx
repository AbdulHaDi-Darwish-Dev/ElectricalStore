import type { AdminProductDto } from "@/features/admin-products";
import { getProductReadiness } from "@/features/admin-products";

type ProductReadinessPanelProps = {
  product: AdminProductDto;
};

export function ProductReadinessPanel({ product }: ProductReadinessPanelProps) {
  const { checks, likelyPublic } = getProductReadiness(product);

  return (
    <div className="rounded-md border border-border bg-card px-4 py-3">
      <p className="text-sm font-semibold text-foreground">
        جاهزية الظهور في المتجر
      </p>
      <p className="mt-1 text-xs leading-5 text-muted-foreground">
        إرشادي فقط — الكتالوج العام يعتمد على قواعد الخادم. قد يتأخر ظهور
        التغييرات حتى دقيقة تقريباً.
      </p>
      <ul className="mt-3 space-y-2">
        {checks.map((check) => (
          <li key={check.id} className="flex items-start gap-2 text-sm">
            <span
              aria-hidden
              className={
                check.met
                  ? "mt-1.5 size-1.5 shrink-0 rounded-full bg-primary"
                  : "mt-1.5 size-1.5 shrink-0 rounded-full border border-muted-foreground"
              }
            />
            <span className={check.met ? "text-foreground" : "text-muted-foreground"}>
              {check.met ? "✓ " : "○ "}
              {check.label}
            </span>
          </li>
        ))}
      </ul>
      <p className="mt-3 text-sm font-medium text-foreground" role="status">
        {likelyPublic
          ? "يُتوقع ظهور المنتج في المتجر العام عند استيفاء الشروط."
          : "المنتج غير ظاهر حالياً في المتجر العام حتى تكتمل الشروط أعلاه."}
      </p>
    </div>
  );
}
