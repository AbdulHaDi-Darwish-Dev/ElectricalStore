"use client";

import Image from "next/image";
import { useId, useState, type KeyboardEvent } from "react";
import type { ProductImageDto } from "@/features/catalog";
import { sortProductImages } from "@/features/catalog";

type ProductGalleryProps = {
  images: ProductImageDto[];
  productName: string;
};

export function ProductGallery({ images, productName }: ProductGalleryProps) {
  const sorted = sortProductImages(images);
  const labelId = useId();
  const [activeIndex, setActiveIndex] = useState(0);
  const active = sorted[activeIndex] ?? sorted[0];

  if (!active) {
    return (
      <div className="flex aspect-square items-center justify-center rounded-md bg-muted text-sm text-muted-foreground">
        لا توجد صور لهذا المنتج
      </div>
    );
  }

  function select(index: number) {
    setActiveIndex(index);
  }

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (sorted.length < 2) return;
    if (event.key === "ArrowLeft") {
      event.preventDefault();
      setActiveIndex((current) => (current + 1) % sorted.length);
    } else if (event.key === "ArrowRight") {
      event.preventDefault();
      setActiveIndex((current) => (current - 1 + sorted.length) % sorted.length);
    } else if (event.key === "Home") {
      event.preventDefault();
      setActiveIndex(0);
    } else if (event.key === "End") {
      event.preventDefault();
      setActiveIndex(sorted.length - 1);
    }
  }

  return (
    <div className="space-y-3" tabIndex={0} onKeyDown={onKeyDown}>
      <div
        className="relative aspect-square overflow-hidden rounded-md bg-muted"
        role="img"
        aria-labelledby={labelId}
      >
        <Image
          src={active.url}
          alt={`${productName} — صورة ${activeIndex + 1}`}
          fill
          priority
          sizes="(max-width: 1024px) 100vw, 50vw"
          className="object-cover"
        />
        <span id={labelId} className="sr-only">
          معرض صور {productName}
        </span>
      </div>

      {sorted.length > 1 ? (
        <ul className="flex flex-wrap gap-2" role="list">
          {sorted.map((image, index) => {
            const selected = index === activeIndex;
            return (
              <li key={image.id}>
                <button
                  type="button"
                  aria-label={`عرض الصورة ${index + 1}`}
                  aria-current={selected ? "true" : undefined}
                  onClick={() => select(index)}
                  className={`relative h-16 w-16 overflow-hidden rounded-md border-2 transition ${
                    selected
                      ? "border-primary"
                      : "border-transparent hover:border-border"
                  }`}
                >
                  <Image
                    src={image.url}
                    alt=""
                    fill
                    sizes="64px"
                    className="object-cover"
                  />
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}
