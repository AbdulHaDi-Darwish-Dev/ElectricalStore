import { brand } from "@/config/brand";

type AdminTopBarProps = {
  title?: string;
};

export function AdminTopBar({ title = "لوحة التحكم" }: AdminTopBarProps) {
  return (
    <header className="flex items-center justify-between gap-3 border-b border-border bg-card px-4 py-3 sm:px-6">
      <div>
        <h1 className="text-base font-semibold text-foreground sm:text-lg">
          {title}
        </h1>
        <p className="text-xs text-muted-foreground">{brand.name} — واجهة مؤقتة</p>
      </div>
    </header>
  );
}
