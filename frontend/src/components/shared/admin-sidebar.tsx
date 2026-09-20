import Link from "next/link";
import { brand } from "@/config/brand";
import { adminNav } from "@/config/navigation";

export function AdminSidebar() {
  return (
    <aside className="flex w-full flex-col border-b border-border bg-card md:min-h-screen md:w-64 md:shrink-0 md:border-b-0 md:border-e">
      <div className="border-b border-border px-4 py-4">
        <p className="text-xs text-muted-foreground">لوحة الإدارة</p>
        <p className="text-base font-semibold text-foreground">{brand.shortName}</p>
      </div>
      <nav aria-label="تنقل لوحة الإدارة" className="flex-1 px-2 py-3">
        <ul className="flex flex-row gap-1 overflow-x-auto md:flex-col md:overflow-visible">
          {adminNav.map((item) => (
            <li key={item.label} className="shrink-0">
              {item.disabled ? (
                <span className="block cursor-not-allowed rounded-md px-3 py-2 text-sm text-muted-foreground">
                  {item.label}
                </span>
              ) : (
                <Link
                  href={item.href}
                  className="block rounded-md px-3 py-2 text-sm text-foreground hover:bg-accent hover:text-accent-foreground"
                >
                  {item.label}
                </Link>
              )}
            </li>
          ))}
        </ul>
      </nav>
    </aside>
  );
}
