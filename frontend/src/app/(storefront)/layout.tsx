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
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6">
        {children}
      </main>
      <StorefrontFooter />
    </div>
  );
}
