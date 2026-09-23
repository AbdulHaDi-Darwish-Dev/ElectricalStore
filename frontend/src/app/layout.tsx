import type { Metadata, Viewport } from "next";
import { Alexandria, IBM_Plex_Sans_Arabic, Manrope } from "next/font/google";
import { Providers } from "./providers";
import { brand } from "@/config/brand";
import { site } from "@/config/site";
import "./globals.css";

const uiArabic = IBM_Plex_Sans_Arabic({
  subsets: ["arabic"],
  variable: "--font-ui",
  display: "swap",
  weight: ["400", "500", "600", "700"],
});

const headingArabic = Alexandria({
  subsets: ["arabic", "latin"],
  variable: "--font-heading",
  display: "swap",
  weight: ["500", "600", "700"],
});

const latin = Manrope({
  subsets: ["latin"],
  variable: "--font-latin",
  display: "swap",
  weight: ["500", "600", "700"],
});

export const metadata: Metadata = {
  metadataBase: new URL(site.url),
  title: {
    default: brand.name,
    template: `%s | ${brand.name}`,
  },
  description: brand.description,
  applicationName: brand.nameEn,
  icons: {
    icon: [{ url: brand.faviconSrc, type: "image/svg+xml" }],
    shortcut: brand.faviconSrc,
  },
  openGraph: {
    title: brand.name,
    description: brand.description,
    siteName: brand.nameEn,
    images: [{ url: brand.ogImageSrc, width: 1200, height: 630, alt: brand.logoAlt }],
    locale: "ar_SY",
    type: "website",
  },
  robots: {
    index: true,
    follow: true,
  },
};

export const viewport: Viewport = {
  themeColor: brand.themeColor,
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang={site.htmlLang}
      dir={site.dir}
      className={`${uiArabic.variable} ${headingArabic.variable} ${latin.variable} h-full`}
    >
      <body className="min-h-screen bg-background font-sans text-foreground antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
