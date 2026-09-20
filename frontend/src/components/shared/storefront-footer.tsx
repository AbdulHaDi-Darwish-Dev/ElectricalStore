import Link from "next/link";
import { brand } from "@/config/brand";
import { storefrontNav } from "@/config/navigation";

export function StorefrontFooter() {
  return (
    <footer className="mt-auto border-t border-border bg-card">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-4 py-8 text-sm text-muted-foreground sm:px-6">
        <p className="text-base font-medium text-foreground">{brand.name}</p>
        <p className="max-w-2xl leading-6">{brand.description}</p>
        <nav aria-label="روابط التذييل">
          <ul className="flex flex-wrap gap-4">
            {storefrontNav.map((item) => (
              <li key={item.href}>
                <Link href={item.href} className="hover:text-primary">
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      </div>
    </footer>
  );
}
