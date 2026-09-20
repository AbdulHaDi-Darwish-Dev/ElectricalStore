import { ProductGridSkeleton } from "@/components/storefront/skeletons";

export default function ProductsLoading() {
  return (
    <div className="mx-auto w-full max-w-6xl space-y-8 px-4 py-10 sm:px-6">
      <div className="space-y-2">
        <div className="h-9 w-36 animate-pulse rounded bg-muted" />
        <div className="h-4 w-80 animate-pulse rounded bg-muted" />
      </div>
      <div className="h-28 animate-pulse rounded-md bg-muted" />
      <ProductGridSkeleton />
    </div>
  );
}
