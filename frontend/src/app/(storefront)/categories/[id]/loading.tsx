import { ProductGridSkeleton } from "@/components/storefront/skeletons";

export default function CategoryDetailLoading() {
  return (
    <div className="mx-auto w-full max-w-6xl space-y-10 px-4 py-10 sm:px-6">
      <div className="grid gap-6 lg:grid-cols-2">
        <div className="space-y-3">
          <div className="h-4 w-16 animate-pulse rounded bg-muted" />
          <div className="h-10 w-2/3 animate-pulse rounded bg-muted" />
          <div className="h-16 w-full animate-pulse rounded bg-muted" />
        </div>
        <div className="aspect-[16/10] animate-pulse rounded-md bg-muted" />
      </div>
      <ProductGridSkeleton count={4} />
    </div>
  );
}
