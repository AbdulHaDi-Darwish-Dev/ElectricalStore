import { CategoryGridSkeleton } from "@/components/storefront/skeletons";

export default function CategoriesLoading() {
  return (
    <div className="mx-auto w-full max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <div className="space-y-2">
        <div className="h-9 w-40 animate-pulse rounded bg-muted" />
        <div className="h-4 w-72 animate-pulse rounded bg-muted" />
      </div>
      <CategoryGridSkeleton />
    </div>
  );
}
