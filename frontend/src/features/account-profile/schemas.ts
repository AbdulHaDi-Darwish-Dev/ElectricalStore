import { z } from "zod";

/** Mirrors Permixa Identity password options used by the host (length 8+, complexity). */
export const PASSWORD_POLICY_HINT =
  "كلمة المرور: ٨ أحرف على الأقل، وتتضمن حرفاً كبيراً وصغيراً ورقماً ورمزاً خاصاً.";

const fullNameSchema = z
  .string()
  .trim()
  .min(1, "الاسم الكامل مطلوب")
  .max(200, "الاسم الكامل طويل جداً");

const passwordSchema = z
  .string()
  .min(8, "كلمة المرور يجب ألا تقل عن 8 أحرف")
  .max(128, "كلمة المرور طويلة جداً");

export const customerRegisterFormSchema = z
  .object({
    fullName: fullNameSchema,
    email: z
      .string()
      .trim()
      .email("البريد الإلكتروني غير صالح")
      .max(256, "البريد طويل جداً"),
    password: passwordSchema,
    confirmPassword: z.string().min(1, "تأكيد كلمة المرور مطلوب"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "كلمتا المرور غير متطابقتين",
    path: ["confirmPassword"],
  });

export type CustomerRegisterFormValues = z.infer<
  typeof customerRegisterFormSchema
>;

export const updateProfileFormSchema = z.object({
  fullName: fullNameSchema,
});

export type UpdateProfileFormValues = z.infer<typeof updateProfileFormSchema>;

export const changePasswordFormSchema = z
  .object({
    currentPassword: z.string().min(1, "كلمة المرور الحالية مطلوبة"),
    newPassword: passwordSchema,
    confirmNewPassword: z.string().min(1, "تأكيد كلمة المرور الجديدة مطلوب"),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "كلمتا المرور غير متطابقتين",
    path: ["confirmNewPassword"],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordFormSchema>;

export const forgotPasswordFormSchema = z.object({
  email: z
    .string()
    .trim()
    .email("البريد الإلكتروني غير صالح")
    .max(256, "البريد طويل جداً"),
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordFormSchema>;

export const resetPasswordFormSchema = z
  .object({
    newPassword: passwordSchema,
    confirmNewPassword: z.string().min(1, "تأكيد كلمة المرور الجديدة مطلوب"),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "كلمتا المرور غير متطابقتين",
    path: ["confirmNewPassword"],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordFormSchema>;

export const changeEmailFormSchema = z.object({
  newEmail: z
    .string()
    .trim()
    .email("البريد الإلكتروني غير صالح")
    .max(256, "البريد طويل جداً"),
  currentPassword: z.string().min(1, "كلمة المرور الحالية مطلوبة"),
});

export type ChangeEmailFormValues = z.infer<typeof changeEmailFormSchema>;
