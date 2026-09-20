import { z } from "zod";
import { INVENTORY_REASON_MAX_LENGTH } from "./helpers";

/**
 * Adjustment form — backend uses QuantityDelta (add/remove), not absolute OnHand.
 */
export const adjustInventoryFormSchema = z.object({
  quantityDelta: z
    .number("أدخل كمية التعديل.")
    .refine((v) => Number.isFinite(v), { message: "أدخل كمية التعديل." })
    .refine((v) => v !== 0, {
      message: "قيمة التعديل لا يمكن أن تكون صفراً.",
    }),
  reason: z
    .string()
    .trim()
    .min(1, "سبب التعديل مطلوب.")
    .max(
      INVENTORY_REASON_MAX_LENGTH,
      `سبب التعديل يجب ألا يتجاوز ${INVENTORY_REASON_MAX_LENGTH} حرفاً.`,
    ),
});

export type AdjustInventoryFormValues = z.infer<
  typeof adjustInventoryFormSchema
>;
