"use client";

import type {
  FieldErrors,
  FieldValues,
  Path,
  UseFormRegister,
  UseFormSetValue,
  UseFormWatch,
} from "react-hook-form";
import type { SellingUnit } from "@/features/catalog";
import {
  defaultQuantityIncrement,
  SELLING_UNITS,
} from "@/features/admin-products";
import { formatSellingUnit } from "@/lib/format";

type VariantFieldShape = {
  name: string;
  sku: string;
  price: number;
  sellingUnit: SellingUnit;
  quantityIncrement: number;
  isActive?: boolean;
};

type VariantFieldsProps<T extends FieldValues & VariantFieldShape> = {
  register: UseFormRegister<T>;
  errors: FieldErrors<T>;
  watch: UseFormWatch<T>;
  setValue: UseFormSetValue<T>;
  disabled?: boolean;
  showActiveToggle?: boolean;
  idPrefix?: string;
};

export function VariantFields<T extends FieldValues & VariantFieldShape>({
  register,
  errors,
  watch,
  setValue,
  disabled,
  showActiveToggle = true,
  idPrefix = "variant",
}: VariantFieldsProps<T>) {
  const sellingUnit = watch("sellingUnit" as Path<T>) as SellingUnit;

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <label htmlFor={`${idPrefix}-name`} className="text-sm font-medium">
            اسم الخيار
          </label>
          <input
            id={`${idPrefix}-name`}
            disabled={disabled}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
            aria-invalid={errors.name ? true : undefined}
            {...register("name" as Path<T>)}
          />
          {errors.name ? (
            <p className="text-sm text-destructive" role="alert">
              {String(errors.name.message)}
            </p>
          ) : null}
        </div>

        <div className="space-y-2">
          <label htmlFor={`${idPrefix}-sku`} className="text-sm font-medium">
            رمز SKU
          </label>
          <input
            id={`${idPrefix}-sku`}
            disabled={disabled}
            className="w-full rounded-md border border-border bg-background px-3 py-2 font-mono text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
            aria-invalid={errors.sku ? true : undefined}
            {...register("sku" as Path<T>)}
          />
          {errors.sku ? (
            <p className="text-sm text-destructive" role="alert">
              {String(errors.sku.message)}
            </p>
          ) : null}
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="space-y-2">
          <label htmlFor={`${idPrefix}-price`} className="text-sm font-medium">
            السعر (ل.س)
          </label>
          <input
            id={`${idPrefix}-price`}
            type="number"
            step="0.01"
            min="0"
            disabled={disabled}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
            aria-invalid={errors.price ? true : undefined}
            {...register("price" as Path<T>, { valueAsNumber: true })}
          />
          {errors.price ? (
            <p className="text-sm text-destructive" role="alert">
              {String(errors.price.message)}
            </p>
          ) : null}
        </div>

        <div className="space-y-2">
          <label
            htmlFor={`${idPrefix}-sellingUnit`}
            className="text-sm font-medium"
          >
            وحدة البيع
          </label>
          <select
            id={`${idPrefix}-sellingUnit`}
            disabled={disabled}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
            {...register("sellingUnit" as Path<T>, {
              onChange: (e) => {
                const unit = e.target.value as SellingUnit;
                setValue(
                  "quantityIncrement" as Path<T>,
                  defaultQuantityIncrement(unit) as T[Path<T>],
                  { shouldValidate: true },
                );
              },
            })}
          >
            {SELLING_UNITS.map((unit) => (
              <option key={unit} value={unit}>
                {formatSellingUnit(unit)}
              </option>
            ))}
          </select>
          {errors.sellingUnit ? (
            <p className="text-sm text-destructive" role="alert">
              {String(errors.sellingUnit.message)}
            </p>
          ) : null}
        </div>

        <div className="space-y-2">
          <label htmlFor={`${idPrefix}-qty`} className="text-sm font-medium">
            خطوة الكمية
          </label>
          <input
            id={`${idPrefix}-qty`}
            type="number"
            step="any"
            min="0"
            disabled={disabled || sellingUnit === "Piece"}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm outline-none ring-primary focus:ring-2 disabled:opacity-60"
            aria-invalid={errors.quantityIncrement ? true : undefined}
            {...register("quantityIncrement" as Path<T>, {
              valueAsNumber: true,
            })}
          />
          {errors.quantityIncrement ? (
            <p className="text-sm text-destructive" role="alert">
              {String(errors.quantityIncrement.message)}
            </p>
          ) : (
            <p className="text-xs text-muted-foreground">
              أقل خطوة يمكن للعميل زيادتها عند الشراء.
              {sellingUnit === "Piece" ? " للقطعة ثابتة على 1." : null}
            </p>
          )}
        </div>
      </div>

      {showActiveToggle ? (
        <label className="flex items-start gap-3 text-sm">
          <input
            type="checkbox"
            className="mt-1 size-4 accent-primary"
            disabled={disabled}
            {...register("isActive" as Path<T>)}
          />
          <span>
            <span className="font-medium">خيار فعّال</span>
            <span className="mt-1 block text-muted-foreground">
              الخيارات غير الفعّالة لا تُعرض للشراء في المتجر.
            </span>
          </span>
        </label>
      ) : null}
    </div>
  );
}
