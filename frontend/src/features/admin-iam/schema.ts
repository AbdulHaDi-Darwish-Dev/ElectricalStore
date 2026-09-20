import { z } from "zod";

export const createRoleFormSchema = z.object({
  name: z.string().trim().min(1, "اسم الدور مطلوب.").max(256),
  referenceRoleId: z.string().uuid("اختر دوراً مرجعياً."),
  placement: z.enum(["Above", "Below", "SameLevel"]),
});

export type CreateRoleFormValues = z.infer<typeof createRoleFormSchema>;

export const renameRoleFormSchema = z.object({
  name: z.string().trim().min(1, "اسم الدور مطلوب.").max(256),
});

export type RenameRoleFormValues = z.infer<typeof renameRoleFormSchema>;
