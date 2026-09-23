import type { CategoryDto } from "@/features/catalog";
import { CategoryCard } from "./category-card";

type CategoryGridProps = {
  categories: CategoryDto[];
};

export function CategoryGrid({ categories }: CategoryGridProps) {
  return (
    <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:gap-5 lg:grid-cols-3">
      {categories.map((category) => (
        <li key={category.id}>
          <CategoryCard category={category} />
        </li>
      ))}
    </ul>
  );
}
