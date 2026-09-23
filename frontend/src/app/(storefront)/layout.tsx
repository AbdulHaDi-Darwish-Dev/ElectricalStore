import type { ReactNode } from "react";
import { StorefrontFooter } from "@/components/shared/storefront-footer";
import { StorefrontHeader } from "@/components/shared/storefront-header";

/**
 * Storefront application shell.
 * min-h-screen + flex-col + main flex-1 keeps the footer at the bottom of the
 * viewport when content is short, without position:fixed.
 */
export default function StorefrontLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  return (
    <div className="flex min-h-screen flex-col">
      <StorefrontHeader />
      <main className="flex min-h-0 flex-1 flex-col">{children}</main>
      <StorefrontFooter />
    </div>
  );
}
