import { z } from "zod";
import { ZONE_NAME_MAX_LENGTH } from "./types";

export const shippingZoneFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "اسم منطقة الشحن مطلوب.")
    .max(
      ZONE_NAME_MAX_LENGTH,
      `الاسم يجب ألا يتجاوز ${ZONE_NAME_MAX_LENGTH} حرفاً.`,
    ),
  fee: z
    .number("أدخل رسوم الشحن.")
    .refine((v) => Number.isFinite(v), { message: "أدخل رسوم الشحن." })
    .refine((v) => v >= 0, {
      message: "رسوم الشحن لا يمكن أن تكون سالبة.",
    }),
  /** Create only — edit uses activate/deactivate endpoints. */
  isActive: z.boolean(),
});

export type ShippingZoneFormValues = z.infer<typeof shippingZoneFormSchema>;

export function emptyShippingZoneFormValues(
  overrides?: Partial<ShippingZoneFormValues>,
): ShippingZoneFormValues {
  return {
    name: "",
    fee: 0,
    isActive: true,
    ...overrides,
  };
}
