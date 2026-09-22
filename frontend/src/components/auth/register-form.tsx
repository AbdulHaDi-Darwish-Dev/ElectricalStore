"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@/lib/api";
import {
  PASSWORD_POLICY_HINT,
  customerRegisterFormSchema,
  getCustomerProfileErrorMessage,
  registerCustomer,
  type CustomerRegisterFormValues,
} from "@/features/account-profile";

export function RegisterForm() {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const form = useForm<CustomerRegisterFormValues>({
    resolver: zodResolver(customerRegisterFormSchema),
    defaultValues: {
      fullName: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
    mode: "onBlur",
  });

  async function onSubmit(values: CustomerRegisterFormValues) {
    setFormError(null);
    try {
      await registerCustomer({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
      });
      setDone(true);
      router.replace("/login");
    } catch (error) {
      if (error instanceof ApiError) {
        setFormError(getCustomerProfileErrorMessage(error.code, error.status));
        return;
      }
      setFormError(getCustomerProfileErrorMessage(undefined));
    }
  }

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-5"
      noValidate
    >
      <div className="space-y-2">
        <label htmlFor="fullName" className="text-sm font-medium">
          الاسم الكامل
        </label>
        <input
          id="fullName"
          autoComplete="name"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("fullName")}
        />
        {form.formState.errors.fullName ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.fullName.message}
          </p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label htmlFor="email" className="text-sm font-medium">
          البريد الإلكتروني
        </label>
        <input
          id="email"
          type="email"
          autoComplete="email"
          dir="ltr"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("email")}
        />
        {form.formState.errors.email ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.email.message}
          </p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label htmlFor="password" className="text-sm font-medium">
          كلمة المرور
        </label>
        <input
          id="password"
          type="password"
          autoComplete="new-password"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("password")}
        />
        <p className="text-xs text-muted-foreground">{PASSWORD_POLICY_HINT}</p>
        {form.formState.errors.password ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.password.message}
          </p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label htmlFor="confirmPassword" className="text-sm font-medium">
          تأكيد كلمة المرور
        </label>
        <input
          id="confirmPassword"
          type="password"
          autoComplete="new-password"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("confirmPassword")}
        />
        {form.formState.errors.confirmPassword ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.confirmPassword.message}
          </p>
        ) : null}
      </div>

      {formError ? (
        <p className="text-sm text-destructive" role="alert">
          {formError}
        </p>
      ) : null}
      {done ? (
        <p className="text-sm text-muted-foreground" role="status">
          تم إنشاء الحساب. يمكنك تسجيل الدخول الآن.
        </p>
      ) : null}

      <button
        type="submit"
        disabled={form.formState.isSubmitting}
        className="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
      >
        {form.formState.isSubmitting ? "جاري إنشاء الحساب…" : "إنشاء حساب"}
      </button>

      <p className="text-center text-sm text-muted-foreground">
        لديك حساب؟{" "}
        <Link href="/login" className="text-primary hover:underline">
          تسجيل الدخول
        </Link>
      </p>
    </form>
  );
}
