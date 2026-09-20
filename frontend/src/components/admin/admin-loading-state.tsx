type AdminLoadingStateProps = {
  label?: string;
};

export function AdminLoadingState({
  label = "جاري التحميل…",
}: AdminLoadingStateProps) {
  return (
    <div className="space-y-3" aria-busy="true" aria-live="polite">
      <p className="sr-only">{label}</p>
      <div className="h-8 w-48 animate-pulse rounded bg-muted" />
      <div className="h-24 animate-pulse rounded-md bg-muted" />
      <div className="h-24 animate-pulse rounded-md bg-muted" />
    </div>
  );
}
