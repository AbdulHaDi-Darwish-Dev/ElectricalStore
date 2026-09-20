import { z } from "zod";
import type { SellingUnit } from "@/features/catalog";
import {
  PRODUCT_DESCRIPTION_MAX_LENGTH,
  PRODUCT_NAME_MAX_LENGTH,
  PRODUCT_SKU_MAX_LENGTH,
  PRODUCT_VARIANT_NAME_MAX_LENGTH,
} from "./types";

const sellingUnitSchema = z.enum(["Piece", "Meter"]);

const productVariantCoreSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "اسم الخيار مطلوب.")
    .max(
      PRODUCT_VARIANT_NAME_MAX_LENGTH,
      `اسم الخيار يجب ألا يتجاوز ${PRODUCT_VARIANT_NAME_MAX_LENGTH} حرفاً.`,
    ),
  sku: z
    .string()
    .trim()
    .min(1, "رمز SKU مطلوب.")
    .max(
      PRODUCT_SKU_MAX_LENGTH,
      `رمز SKU يجب ألا يتجاوز ${PRODUCT_SKU_MAX_LENGTH} حرفاً.`,
    ),
  price: z.number().positive("السعر يجب أن يكون أكبر من صفر."),
  sellingUnit: sellingUnitSchema,
  quantityIncrement: z
    .number()
    .positive("خطوة الكمية يجب أن تكون أكبر من صفر."),
});

function withPieceIncrementRule<T extends z.ZodType>(schema: T) {
  return schema.superRefine((value, ctx) => {
    const v = value as {
      sellingUnit: SellingUnit;
      quantityIncrement: number;
    };
    if (v.sellingUnit === "Piece" && v.quantityIncrement !== 1) {
      ctx.addIssue({
        code: "custom",
        path: ["quantityIncrement"],
        message: "وحدة القطعة تتطلب خطوة كمية تساوي 1.",
      });
    }
  });
}

export const productVariantFormSchema = withPieceIncrementRule(
  productVariantCoreSchema.extend({
    isActive: z.boolean(),
  }),
);

export type ProductVariantFormValues = z.infer<typeof productVariantFormSchema>;

/** Edit variant — no isActive (use activate/deactivate endpoints). */
export const productVariantEditFormSchema = withPieceIncrementRule(
  productVariantCoreSchema,
);

export type ProductVariantEditFormValues = z.infer<
  typeof productVariantEditFormSchema
>;

export const productBasicsFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "اسم المنتج مطلوب.")
    .max(
      PRODUCT_NAME_MAX_LENGTH,
      `الاسم يجب ألا يتجاوز ${PRODUCT_NAME_MAX_LENGTH} حرفاً.`,
    ),
  description: z
    .string()
    .max(
      PRODUCT_DESCRIPTION_MAX_LENGTH,
      `الوصف يجب ألا يتجاوز ${PRODUCT_DESCRIPTION_MAX_LENGTH} حرفاً.`,
    ),
  categoryId: z.string().uuid("يجب اختيار تصنيف صالح."),
  isActive: z.boolean(),
});

export type ProductBasicsFormValues = z.infer<typeof productBasicsFormSchema>;

export const productCreateFormSchema = productBasicsFormSchema.extend({
  variants: z
    .array(productVariantFormSchema)
    .min(1, "يجب إضافة خيار واحد على الأقل."),
});

export type ProductCreateFormValues = z.infer<typeof productCreateFormSchema>;

export function emptyVariantFormValues(
  overrides?: Partial<ProductVariantFormValues>,
): ProductVariantFormValues {
  return {
    name: "",
    sku: "",
    price: 0,
    sellingUnit: "Piece" as SellingUnit,
    quantityIncrement: 1,
    isActive: true,
    ...overrides,
  };
}

export function emptyProductCreateFormValues(): ProductCreateFormValues {
  return {
    name: "",
    description: "",
    categoryId: "",
    isActive: true,
    variants: [emptyVariantFormValues()],
  };
}

export function emptyProductBasicsFormValues(
  overrides?: Partial<ProductBasicsFormValues>,
): ProductBasicsFormValues {
  return {
    name: "",
    description: "",
    categoryId: "",
    isActive: true,
    ...overrides,
  };
}
