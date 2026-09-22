import { z } from "zod";

export const loginFormSchema = z.object({
  emailOrUserName: z
    .string()
    .trim()
    .min(1, "البريد أو اسم المستخدم مطلوب"),
  password: z.string().min(1, "كلمة المرور مطلوبة"),
});

export type LoginFormValues = z.infer<typeof loginFormSchema>;

/** @deprecated Prefer customerRegisterFormSchema from account-profile (FullName UX). */
export const registerFormSchema = z
  .object({
    userName: z
      .string()
      .trim()
      .min(3, "اسم المستخدم قصير جداً")
      .max(64, "اسم المستخدم طويل جداً"),
    email: z
      .string()
      .trim()
      .email("البريد الإلكتروني غير صالح")
      .max(256, "البريد طويل جداً"),
    password: z
      .string()
      .min(8, "كلمة المرور يجب ألا تقل عن 8 أحرف")
      .max(128, "كلمة المرور طويلة جداً"),
    confirmPassword: z.string().min(1, "تأكيد كلمة المرور مطلوب"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "كلمتا المرور غير متطابقتين",
    path: ["confirmPassword"],
  });

export type RegisterFormValues = z.infer<typeof registerFormSchema>;
