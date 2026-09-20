import type { Metadata } from "next";
import { Noto_Sans_Arabic } from "next/font/google";
import { Providers } from "./providers";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import "./globals.css";

const arabicSans = Noto_Sans_Arabic({
  subsets: ["arabic"],
  variable: "--font-arabic-sans",
  display: "swap",
  weight: ["400", "500", "600", "700"],
});

export const metadata: Metadata = {
  metadataBase: new URL(site.url),
  title: {
    default: brand.name,
    template: `%s | ${brand.name}`,
  },
  description: brand.description,
  robots: {
    index: true,
    follow: true,
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang={site.htmlLang} dir={site.dir} className={`${arabicSans.variable} h-full`}>
      <body className="min-h-full bg-background font-sans text-foreground antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
