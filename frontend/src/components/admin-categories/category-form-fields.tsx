"use client";

import type { UseFormReturn } from "react-hook-form";
import type { CategoryFormValues } from "@/features/admin-categories";

type CategoryFormFieldsProps = {
  form: UseFormReturn<CategoryFormValues>;
  /** When true, show isActive checkbox (create). Edit uses activate/deactivate. */
  showActiveToggle?: boolean;
  disabled?: boolean;
};

export function CategoryFormFields({
  form,
  showActiveToggle = false,
  disabled,
}: CategoryFormFieldsProps) {
  const {
    register,
    formState: { errors },
  } = form;

  return (
    <div className="space-y-5">
      <div className="space-y-2">
        <label htmlFor="category-name" className="text-sm font-medium">
          اسم التصنيف
        </label>
        <input
          id="category-name"
          autoComplete="off"
          disabled={disabled}
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
          aria-invalid={errors.name ? true : undefined}
          aria-describedby={errors.name ? "category-name-error" : undefined}
          {...register("name")}
        />
        {errors.name ? (
          <p
            id="category-name-error"
            className="text-sm text-destructive"
            role="alert"
          >
            {errors.name.message}
          </p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label htmlFor="category-description" className="text-sm font-medium">
          الوصف{" "}
          <span className="font-normal text-muted-foreground">(اختياري)</span>
        </label>
        <textarea
          id="category-description"
          rows={4}
          disabled={disabled}
          className="w-full resize-y rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
          aria-invalid={errors.description ? true : undefined}
          aria-describedby={
            errors.description ? "category-description-error" : undefined
          }
          {...register("description")}
        />
        {errors.description ? (
          <p
            id="category-description-error"
            className="text-sm text-destructive"
            role="alert"
          >
            {errors.description.message}
          </p>
        ) : null}
      </div>

      {showActiveToggle ? (
        <label className="flex items-start gap-3 rounded-md border border-border bg-card px-3 py-3 text-sm">
          <input
            type="checkbox"
            className="mt-1 size-4 accent-primary"
            disabled={disabled}
            {...register("isActive")}
          />
          <span>
            <span className="font-medium text-foreground">نشط عند الإنشاء</span>
            <span className="mt-1 block text-muted-foreground">
              التصنيف غير النشط لا يظهر في الكتالوج العام حتى لو وُجدت صورة.
            </span>
          </span>
        </label>
      ) : null}
    </div>
  );
}
