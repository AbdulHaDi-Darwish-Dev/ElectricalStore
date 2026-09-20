import type { MetadataRoute } from "next";
import { site } from "@/config/site";

/**
 * Public catalog is indexable. Admin and future private routes are disallowed.
 */
export default function robots(): MetadataRoute.Robots {
  const siteUrl = site.url.replace(/\/$/, "");

  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
        disallow: [
          "/admin",
          "/admin/",
          "/account",
          "/account/",
          "/cart",
          "/checkout",
          "/orders",
        ],
      },
    ],
    sitemap: `${siteUrl}/sitemap.xml`,
  };
}
