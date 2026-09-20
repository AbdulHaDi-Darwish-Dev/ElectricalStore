"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import {
  adjustInventoryFormSchema,
  formatStockQuantity,
  formatStockWithUnit,
  previewOnHandAfterDelta,
  wouldBeNegative,
  wouldViolateReserved,
  type AdjustInventoryFormValues,
  type AdminInventoryItemDto,
} from "@/features/admin-inventory";
import { formatSellingUnit } from "@/lib/format";

type InventoryAdjustDialogProps = {
  item: AdminInventoryItemDto;
  open: boolean;
  busy?: boolean;
  formError?: string | null;
  onSubmit: (values: AdjustInventoryFormValues) => void;
  onCancel: () => void;
};

/**
 * Delta adjustment dialog — quantityDelta adds/removes from OnHand (not absolute set).
 */
export function InventoryAdjustDialog({
  item,
  open,
  busy = false,
  formError = null,
  onSubmit,
  onCancel,
}: InventoryAdjustDialogProps) {
  const form = useForm<AdjustInventoryFormValues>({
    resolver: zodResolver(adjustInventoryFormSchema),
    defaultValues: {
      quantityDelta: undefined as unknown as number,
      reason: "",
    },
  });

  useEffect(() => {
    if (open) {
      form.reset({
        quantityDelta: undefined as unknown as number,
        reason: "",
      });
    }
  }, [open, item.variantId, form]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-foreground/40 p-4 sm:items-center"
      role="presentation"
      onClick={() => {
        if (!busy) onCancel();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="inventory-adjust-title"
        className="w-full max-w-md rounded-md border border-border bg-card p-5 text-foreground shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <form
          className="space-y-4"
          onSubmit={form.handleSubmit((values) => onSubmit(values))}
          noValidate
        >
          <div className="space-y-1">
            <h2
              id="inventory-adjust-title"
              className="text-base font-semibold"
            >
              تعديل المخزون الفعلي
            </h2>
            <p className="text-sm leading-6 text-muted-foreground">
              أدخل كمية للإضافة (+) أو للخصم (−). هذه ليست قيمة مطلقة للمخزون.
            </p>
          </div>

          <div className="rounded-md border border-border bg-muted/30 px-3 py-2 text-sm leading-6">
            <p>
              <span className="text-muted-foreground">المنتج: </span>
              {item.productName}
            </p>
            <p>
              <span className="text-muted-foreground">الخيار: </span>
              {item.variantName}
            </p>
            <p>
              <span className="text-muted-foreground">SKU: </span>
              <span className="font-mono text-xs">{item.sku}</span>
            </p>
            <p>
              <span className="text-muted-foreground">المخزون الفعلي الحالي: </span>
              {formatStockWithUnit(item.onHand, item.sellingUnit)}
            </p>
            <p>
              <span className="text-muted-foreground">المحجوز: </span>
              {formatStockWithUnit(item.reserved, item.sellingUnit)}
            </p>
          </div>

          <Controller
            name="quantityDelta"
            control={form.control}
            render={({ field, fieldState }) => {
              const delta =
                typeof field.value === "number" && Number.isFinite(field.value)
                  ? field.value
                  : null;
              const preview =
                delta !== null
                  ? previewOnHandAfterDelta(item.onHand, delta)
                  : null;
              const reservedConflict =
                delta !== null &&
                wouldViolateReserved(item.onHand, item.reserved, delta);
              const negativeConflict =
                delta !== null && wouldBeNegative(item.onHand, delta);

              return (
                <>
                  <div className="space-y-1.5">
                    <label
                      htmlFor="quantityDelta"
                      className="block text-sm font-medium"
                    >
                      كمية التعديل ({formatSellingUnit(item.sellingUnit)})
                    </label>
                    <input
                      id="quantityDelta"
                      type="number"
                      step="any"
                      inputMode="decimal"
                      disabled={busy}
                      className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
                      aria-invalid={fieldState.error ? true : undefined}
                      aria-describedby="quantityDelta-hint"
                      name={field.name}
                      ref={field.ref}
                      onBlur={field.onBlur}
                      value={
                        typeof field.value === "number" &&
                        Number.isFinite(field.value)
                          ? field.value
                          : ""
                      }
                      onChange={(e) => {
                        const raw = e.target.value;
                        if (raw === "" || raw === "-") {
                          field.onChange(undefined);
                          return;
                        }
                        field.onChange(e.target.valueAsNumber);
                      }}
                    />
                    <p
                      id="quantityDelta-hint"
                      className="text-xs leading-5 text-muted-foreground"
                    >
                      مثال: 10 لإضافة عشرة، أو −3 لخصم ثلاثة.
                    </p>
                    {fieldState.error ? (
                      <p className="text-sm text-destructive" role="alert">
                        {fieldState.error.message}
                      </p>
                    ) : null}
                  </div>

                  {preview !== null && delta !== null ? (
                    <p className="text-sm leading-6">
                      <span className="text-muted-foreground">
                        النتيجة المتوقعة:{" "}
                      </span>
                      <span className="font-medium">
                        {formatStockQuantity(item.onHand)}{" "}
                        {delta > 0 ? "+" : "−"}{" "}
                        {formatStockQuantity(Math.abs(delta))} ={" "}
                        {formatStockWithUnit(preview, item.sellingUnit)}
                      </span>
                    </p>
                  ) : null}

                  {negativeConflict ? (
                    <p className="text-sm text-destructive" role="alert">
                      النتيجة ستكون سالبة — الخادم سيرفض هذا التعديل.
                    </p>
                  ) : null}
                  {reservedConflict && !negativeConflict ? (
                    <p className="text-sm text-destructive" role="alert">
                      النتيجة أقل من المحجوز (
                      {formatStockQuantity(item.reserved)}) — الخادم سيرفض هذا
                      التعديل.
                    </p>
                  ) : null}

                  <div className="space-y-1.5">
                    <label
                      htmlFor="adjustReason"
                      className="block text-sm font-medium"
                    >
                      سبب التعديل
                    </label>
                    <textarea
                      id="adjustReason"
                      rows={3}
                      disabled={busy}
                      className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm disabled:opacity-60"
                      aria-invalid={
                        form.formState.errors.reason ? true : undefined
                      }
                      {...form.register("reason")}
                    />
                    {form.formState.errors.reason ? (
                      <p className="text-sm text-destructive" role="alert">
                        {form.formState.errors.reason.message}
                      </p>
                    ) : null}
                  </div>

                  {formError ? (
                    <p className="text-sm text-destructive" role="alert">
                      {formError}
                    </p>
                  ) : null}

                  <div className="flex flex-wrap justify-end gap-2 pt-1">
                    <button
                      type="button"
                      className="rounded-md border border-border px-3 py-2 text-sm hover:bg-muted disabled:opacity-60"
                      onClick={onCancel}
                      disabled={busy}
                    >
                      إلغاء
                    </button>
                    <button
                      type="submit"
                      className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
                      disabled={busy || reservedConflict || negativeConflict}
                    >
                      {busy ? "جاري التنفيذ…" : "تأكيد التعديل"}
                    </button>
                  </div>
                </>
              );
            }}
          />
        </form>
      </div>
    </div>
  );
}
