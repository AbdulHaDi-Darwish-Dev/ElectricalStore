import type { ReactNode } from "react";
import { StorefrontFooter } from "@/components/shared/storefront-footer";
import { StorefrontHeader } from "@/components/shared/storefront-header";

export default function StorefrontLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  return (
    <div className="flex min-h-full flex-col">
      <StorefrontHeader />
      <main className="flex-1">{children}</main>
      <StorefrontFooter />
    </div>
  );
}
