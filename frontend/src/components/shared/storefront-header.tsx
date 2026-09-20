import Link from "next/link";
import { brand } from "@/config/brand";
import { storefrontNav } from "@/config/navigation";

export function StorefrontHeader() {
  return (
    <header className="border-b border-border bg-card">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-4 py-4 sm:px-6">
        <Link
          href="/"
          className="text-lg font-semibold text-foreground hover:text-primary"
        >
          {brand.shortName}
        </Link>
        <nav aria-label="التنقل الرئيسي" className="hidden sm:block">
          <ul className="flex items-center gap-6 text-sm">
            {storefrontNav.map((item) => (
              <li key={item.label}>
                {item.disabled ? (
                  <span className="cursor-not-allowed text-muted-foreground">
                    {item.label}
                  </span>
                ) : (
                  <Link
                    href={item.href}
                    className="text-foreground hover:text-primary"
                  >
                    {item.label}
                  </Link>
                )}
              </li>
            ))}
          </ul>
        </nav>
        <p className="text-xs text-muted-foreground sm:hidden">{brand.shortName}</p>
      </div>
    </header>
  );
}
