import { brand } from "@/config/brand";

type BrandLogoProps = {
  /** Visual surface: light (default) or dark. */
  surface?: "light" | "dark";
  /** lockup = mark + wordmark; mark = symbol only. */
  variant?: "lockup" | "mark";
  /** Prefer Arabic wordmark (storefront default) or Latin. */
  locale?: "ar" | "en";
  className?: string;
  /** Mark pixel size. */
  markSize?: number;
};

/**
 * Presentation-only brand lockup. Uses app fonts for readable wordmarks.
 */
export function BrandLogo({
  surface = "light",
  variant = "lockup",
  locale = "ar",
  className = "",
  markSize = 28,
}: BrandLogoProps) {
  const markSrc =
    surface === "dark" ? brand.logoMarkDarkSrc : brand.logoMarkSrc;
  const word =
    locale === "en" ? brand.nameEn : brand.name;
  const wordClass =
    surface === "dark"
      ? "text-sidebar-foreground"
      : "text-foreground";

  return (
    <span
      className={`inline-flex items-center gap-2.5 ${className}`}
      aria-label={brand.logoAlt}
    >
      {/* eslint-disable-next-line @next/next/no-img-element -- SVG brand asset from /public */}
      <img
        src={markSrc}
        alt=""
        width={markSize}
        height={markSize}
        className="shrink-0"
        decoding="async"
      />
      {variant === "lockup" ? (
        <span
          className={`text-lg font-semibold tracking-tight ${wordClass} ${
            locale === "en" ? "tracking-[0.08em]" : ""
          }`}
        >
          {word}
        </span>
      ) : null}
    </span>
  );
}
