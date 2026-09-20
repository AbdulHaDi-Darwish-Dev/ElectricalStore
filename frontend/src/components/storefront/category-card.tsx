import Image from "next/image";
import Link from "next/link";
import type { CategoryDto } from "@/features/catalog";

type CategoryCardProps = {
  category: CategoryDto;
};

export function CategoryCard({ category }: CategoryCardProps) {
  return (
    <Link
      href={`/categories/${category.id}`}
      className="group flex h-full flex-col overflow-hidden rounded-md border border-border bg-card transition hover:border-primary/40 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
    >
      <div className="relative aspect-[16/10] w-full bg-muted">
        {category.imageUrl ? (
          <Image
            src={category.imageUrl}
            alt={category.name}
            fill
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
            className="object-cover transition duration-300 group-hover:scale-[1.02]"
          />
        ) : (
          <div
            aria-hidden
            className="flex h-full items-center justify-center bg-secondary text-sm text-muted-foreground"
          >
            بدون صورة
          </div>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-2 p-4">
        <h3 className="text-base font-medium text-foreground group-hover:text-primary">
          {category.name}
        </h3>
        {category.description ? (
          <p className="line-clamp-2 text-sm leading-6 text-muted-foreground">
            {category.description}
          </p>
        ) : null}
      </div>
    </Link>
  );
}
