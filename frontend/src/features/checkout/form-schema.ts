import { z } from "zod";

/**
 * Client UX validation only — backend remains authoritative.
 * Phone: practical length guard, no invented country regex.
 */
export const checkoutCustomerSchema = z.object({
  customerName: z
    .string()
    .trim()
    .min(1, "الاسم مطلوب")
    .max(200, "الاسم طويل جداً"),
  phone: z
    .string()
    .trim()
    .min(6, "رقم الهاتف قصير جداً")
    .max(30, "رقم الهاتف طويل جداً"),
  addressText: z
    .string()
    .trim()
    .min(1, "العنوان مطلوب")
    .max(1000, "العنوان طويل جداً"),
  customerNote: z.string().trim().max(1000, "الملاحظة طويلة جداً").optional(),
  deliveryZoneId: z.string().uuid("اختر منطقة التوصيل"),
});

export type CheckoutCustomerFormValues = z.infer<typeof checkoutCustomerSchema>;
