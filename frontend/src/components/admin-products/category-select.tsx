"use client";

import { useQuery } from "@tanstack/react-query";
import {
  listAdminCategories,
  adminCategoryKeys,
} from "@/features/admin-categories";
import { adminOperationalQueryDefaults } from "@/features/admin";

type CategorySelectProps = {
  id: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  errorId?: string;
  invalid?: boolean;
};

export function CategorySelect({
  id,
  value,
  onChange,
  disabled,
  errorId,
  invalid,
}: CategorySelectProps) {
  const categoriesQuery = useQuery({
    queryKey: adminCategoryKeys.list(),
    queryFn: ({ signal }) => listAdminCategories(signal),
    ...adminOperationalQueryDefaults,
  });

  if (categoriesQuery.isLoading) {
    return (
      <p className="text-sm text-muted-foreground" aria-live="polite">
        جاري تحميل التصنيفات…
      </p>
    );
  }

  if (categoriesQuery.isError) {
    return (
      <p className="text-sm text-destructive" role="alert">
        تعذر تحميل التصنيفات. أعد تحميل الصفحة.
      </p>
    );
  }

  const categories = categoriesQuery.data ?? [];

  return (
    <select
      id={id}
      value={value}
      disabled={disabled}
      aria-invalid={invalid || undefined}
      aria-describedby={errorId}
      className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
      onChange={(e) => onChange(e.target.value)}
    >
      <option value="">اختر تصنيفاً…</option>
      {categories.map((cat) => (
        <option key={cat.id} value={cat.id}>
          {cat.name}
          {cat.isActive ? "" : " (غير نشط)"}
          {cat.hasImage ? "" : " · بلا صورة"}
        </option>
      ))}
    </select>
  );
}
