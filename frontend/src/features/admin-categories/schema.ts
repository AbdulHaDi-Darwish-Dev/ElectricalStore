import { z } from "zod";
import {
  CATEGORY_DESCRIPTION_MAX_LENGTH,
  CATEGORY_NAME_MAX_LENGTH,
} from "./types";

export const categoryFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "اسم التصنيف مطلوب.")
    .max(
      CATEGORY_NAME_MAX_LENGTH,
      `الاسم يجب ألا يتجاوز ${CATEGORY_NAME_MAX_LENGTH} حرفاً.`,
    ),
  description: z
    .string()
    .max(
      CATEGORY_DESCRIPTION_MAX_LENGTH,
      `الوصف يجب ألا يتجاوز ${CATEGORY_DESCRIPTION_MAX_LENGTH} حرفاً.`,
    ),
  /** Create only — edit uses activate/deactivate endpoints. */
  isActive: z.boolean(),
});

export type CategoryFormValues = z.infer<typeof categoryFormSchema>;

export function emptyCategoryFormValues(
  overrides?: Partial<CategoryFormValues>,
): CategoryFormValues {
  return {
    name: "",
    description: "",
    isActive: true,
    ...overrides,
  };
}
