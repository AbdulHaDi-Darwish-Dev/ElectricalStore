import { ProductDetailSkeleton } from "@/components/storefront/skeletons";

export default function ProductDetailLoading() {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-10 sm:px-6">
      <ProductDetailSkeleton />
    </div>
  );
}
