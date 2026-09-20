import { z } from "zod";

export const orderingSettingsFormSchema = z.object({
  minimumMerchandiseSubtotal: z
    .number("أدخل الحد الأدنى لقيمة المنتجات.")
    .refine((v) => Number.isFinite(v), {
      message: "أدخل الحد الأدنى لقيمة المنتجات.",
    })
    .refine((v) => v >= 0, {
      message: "الحد الأدنى لقيمة المنتجات لا يمكن أن يكون سالباً.",
    }),
});

export type OrderingSettingsFormValues = z.infer<
  typeof orderingSettingsFormSchema
>;
