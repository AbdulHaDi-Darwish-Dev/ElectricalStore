"use client";

import { Controller, useFormContext } from "react-hook-form";
import type { ShippingZoneFormValues } from "@/features/admin-shipping";
import { storeConfig } from "@/config/store";

type ShippingZoneFormFieldsProps = {
  /** When true, show isActive (create). Edit uses lifecycle endpoints. */
  showActiveToggle?: boolean;
  disabled?: boolean;
};

export function ShippingZoneFormFields({
  showActiveToggle = false,
  disabled = false,
}: ShippingZoneFormFieldsProps) {
  const {
    register,
    control,
    formState: { errors },
  } = useFormContext<ShippingZoneFormValues>();

  return (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label htmlFor="zone-name" className="block text-sm font-medium">
          اسم المنطقة
        </label>
        <input
          id="zone-name"
          type="text"
          disabled={disabled}
          className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
          aria-invalid={errors.name ? true : undefined}
          {...register("name")}
        />
        {errors.name ? (
          <p className="text-sm text-destructive" role="alert">
            {errors.name.message}
          </p>
        ) : null}
      </div>

      <div className="space-y-1.5">
        <label htmlFor="zone-fee" className="block text-sm font-medium">
          رسوم الشحن ({storeConfig.currencyDisplay})
        </label>
        <Controller
          name="fee"
          control={control}
          render={({ field, fieldState }) => (
            <>
              <input
                id="zone-fee"
                type="number"
                min={0}
                step="0.01"
                inputMode="decimal"
                disabled={disabled}
                className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
                aria-invalid={fieldState.error ? true : undefined}
                aria-describedby="zone-fee-hint"
                name={field.name}
                ref={field.ref}
                onBlur={field.onBlur}
                value={
                  typeof field.value === "number" && Number.isFinite(field.value)
                    ? field.value
                    : ""
                }
                onChange={(e) => {
                  const raw = e.target.value;
                  if (raw === "") {
                    field.onChange(undefined);
                    return;
                  }
                  field.onChange(e.target.valueAsNumber);
                }}
              />
              <p
                id="zone-fee-hint"
                className="text-xs leading-5 text-muted-foreground"
              >
                صفر يعني شحن مجاني لهذه المنطقة. لا يُسمح بالقيم السالبة.
              </p>
              {fieldState.error ? (
                <p className="text-sm text-destructive" role="alert">
                  {fieldState.error.message}
                </p>
              ) : null}
            </>
          )}
        />
      </div>

      {showActiveToggle ? (
        <div className="flex items-start gap-2">
          <input
            id="zone-active"
            type="checkbox"
            disabled={disabled}
            className="mt-1 size-4 rounded border-border"
            {...register("isActive")}
          />
          <label htmlFor="zone-active" className="text-sm leading-6">
            تفعيل المنطقة عند الإنشاء
            <span className="mt-0.5 block text-xs text-muted-foreground">
              المناطق غير النشطة لا تظهر في اختيار الشحن عند الدفع.
            </span>
          </label>
        </div>
      ) : null}
    </div>
  );
}
