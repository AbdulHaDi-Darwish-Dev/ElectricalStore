"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { buildProductsHref } from "@/features/catalog";

type ProductSearchFormProps = {
  initialSearch?: string;
  categoryId?: string;
  /** Accessible name for the form region. */
  id?: string;
};

/**
 * Client boundary for input + URL navigation only.
 * Does not fetch the catalog — the server page reads URL params.
 */
export function ProductSearchForm({
  initialSearch = "",
  categoryId,
  id,
}: ProductSearchFormProps) {
  const router = useRouter();
  const [value, setValue] = useState(initialSearch);

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    router.push(
      buildProductsHref({
        categoryId,
        search: value.trim() || undefined,
      }),
    );
  }

  return (
    <form
      id={id}
      role="search"
      onSubmit={onSubmit}
      className="flex w-full flex-col gap-2 sm:flex-row sm:items-end"
    >
      <div className="min-w-0 flex-1 space-y-1">
        <label htmlFor="product-search" className="text-sm text-muted-foreground">
          البحث باسم المنتج
        </label>
        <input
          id="product-search"
          name="search"
          type="search"
          value={value}
          onChange={(event) => setValue(event.target.value)}
          placeholder="مثال: كابل"
          autoComplete="off"
          className="w-full rounded-md border border-border bg-card px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground"
        />
      </div>
      <button
        type="submit"
        className="rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground hover:opacity-90 sm:mb-0"
      >
        بحث
      </button>
    </form>
  );
}
