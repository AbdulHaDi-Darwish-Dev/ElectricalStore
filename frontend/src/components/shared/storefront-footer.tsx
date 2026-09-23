import Link from "next/link";
import { brand } from "@/config/brand";
import { storefrontNav } from "@/config/navigation";
import { BrandLogo } from "@/components/brand/brand-logo";

export function StorefrontFooter() {
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-border bg-[var(--voltano-night)] text-sidebar-foreground">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-4 py-8 sm:px-6 sm:py-9 lg:flex-row lg:items-start lg:justify-between lg:gap-10">
        <div className="max-w-md space-y-3">
          <BrandLogo surface="dark" markSize={28} />
          <p className="text-sm leading-6 text-sidebar-muted">{brand.description}</p>
          <div className="volt-line-footer" aria-hidden />
        </div>

        <nav aria-label="روابط التذييل" className="shrink-0">
          <p className="mb-2.5 text-xs font-semibold tracking-wide text-sidebar-muted">
            تصفح المتجر
          </p>
          <ul className="flex flex-wrap gap-x-5 gap-y-2">
            {storefrontNav.map((item) => (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className="text-sm text-sidebar-foreground transition hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
                >
                  {item.label}
                </Link>
              </li>
            ))}
            <li>
              <Link
                href="/cart"
                className="text-sm text-sidebar-foreground transition hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
              >
                السلة
              </Link>
            </li>
          </ul>
        </nav>
      </div>

      <div className="border-t border-sidebar-border">
        <div className="mx-auto flex w-full max-w-6xl flex-col gap-1 px-4 py-3.5 text-xs text-sidebar-muted sm:flex-row sm:items-center sm:justify-between sm:px-6">
          <p>
            © {year} {brand.nameEn}
          </p>
          <p className="tracking-[0.08em]">{brand.concept}</p>
        </div>
      </div>
    </footer>
  );
}
