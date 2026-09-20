import { brand } from "@/config/brand";

export function StorefrontFooter() {
  return (
    <footer className="mt-auto border-t border-border bg-card">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-2 px-4 py-6 text-sm text-muted-foreground sm:px-6">
        <p className="font-medium text-foreground">{brand.name}</p>
        <p>{brand.description}</p>
      </div>
    </footer>
  );
}
