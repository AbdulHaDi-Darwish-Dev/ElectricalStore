export function ProductGridSkeleton({ count = 8 }: { count?: number }) {
  return (
    <ul
      aria-hidden
      className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
    >
      {Array.from({ length: count }, (_, index) => (
        <li
          key={index}
          className="overflow-hidden rounded-md border border-border bg-card"
        >
          <div className="aspect-[4/3] animate-pulse bg-muted" />
          <div className="space-y-2 p-4">
            <div className="h-3 w-1/3 animate-pulse rounded bg-muted" />
            <div className="h-4 w-3/4 animate-pulse rounded bg-muted" />
            <div className="h-4 w-1/2 animate-pulse rounded bg-muted" />
          </div>
        </li>
      ))}
    </ul>
  );
}

export function CategoryGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <ul
      aria-hidden
      className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3"
    >
      {Array.from({ length: count }, (_, index) => (
        <li
          key={index}
          className="overflow-hidden rounded-md border border-border bg-card"
        >
          <div className="aspect-[16/10] animate-pulse bg-muted" />
          <div className="space-y-2 p-4">
            <div className="h-4 w-2/3 animate-pulse rounded bg-muted" />
            <div className="h-3 w-full animate-pulse rounded bg-muted" />
          </div>
        </li>
      ))}
    </ul>
  );
}

export function ProductDetailSkeleton() {
  return (
    <div aria-hidden className="grid gap-8 lg:grid-cols-2">
      <div className="aspect-square animate-pulse rounded-md bg-muted" />
      <div className="space-y-4">
        <div className="h-4 w-1/4 animate-pulse rounded bg-muted" />
        <div className="h-8 w-3/4 animate-pulse rounded bg-muted" />
        <div className="h-20 w-full animate-pulse rounded bg-muted" />
        <div className="h-24 w-full animate-pulse rounded bg-muted" />
      </div>
    </div>
  );
}
