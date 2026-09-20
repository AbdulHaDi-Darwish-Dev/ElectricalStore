import { z } from "zod";
import { ORDER_CANCELLATION_REASON_MAX_LENGTH } from "./types";

export const cancelOrderFormSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, "سبب الإلغاء مطلوب.")
    .max(
      ORDER_CANCELLATION_REASON_MAX_LENGTH,
      `سبب الإلغاء يجب ألا يتجاوز ${ORDER_CANCELLATION_REASON_MAX_LENGTH} حرفاً.`,
    ),
});

export type CancelOrderFormValues = z.infer<typeof cancelOrderFormSchema>;
