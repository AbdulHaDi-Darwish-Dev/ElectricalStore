"use client";

import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@/lib/api";
import {
  forgotPasswordFormSchema,
  getCustomerProfileErrorMessage,
  PASSWORD_RESET_REQUEST_GENERIC,
  requestCustomerPasswordReset,
  type ForgotPasswordFormValues,
} from "@/features/account-profile";

export function ForgotPasswordForm() {
  const [done, setDone] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const form = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordFormSchema),
    defaultValues: { email: "" },
    mode: "onBlur",
  });

  async function onSubmit(values: ForgotPasswordFormValues) {
    setFormError(null);
    try {
      await requestCustomerPasswordReset({ email: values.email.trim() });
      setDone(true);
    } catch (error) {
      if (error instanceof ApiError) {
        // Anti-enumeration: treat most outcomes as generic success except hard rate-limit/server.
        if (error.status === 429 || (error.status !== undefined && error.status >= 500)) {
          setFormError(getCustomerProfileErrorMessage(error.code, error.status));
          return;
        }
      }
      setDone(true);
    }
  }

  if (done) {
    return (
      <div className="space-y-5 text-center">
        <p className="text-sm text-foreground" role="status">
          {PASSWORD_RESET_REQUEST_GENERIC}
        </p>
        <Link href="/login" className="text-sm text-primary hover:underline">
          العودة لتسجيل الدخول
        </Link>
      </div>
    );
  }

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-5"
      noValidate
    >
      <div className="space-y-2">
        <label htmlFor="email" className="text-sm font-medium">
          البريد الإلكتروني
        </label>
        <input
          id="email"
          type="email"
          dir="ltr"
          autoComplete="email"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("email")}
        />
        {form.formState.errors.email ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.email.message}
          </p>
        ) : null}
      </div>

      {formError ? (
        <p
          className="rounded-md border border-border bg-muted/40 px-3 py-2 text-sm"
          role="alert"
        >
          {formError}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={form.formState.isSubmitting}
        className="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
      >
        {form.formState.isSubmitting ? "جاري الإرسال…" : "إرسال رابط إعادة التعيين"}
      </button>

      <p className="text-center text-sm text-muted-foreground">
        <Link href="/login" className="text-primary hover:underline">
          العودة لتسجيل الدخول
        </Link>
      </p>
    </form>
  );
}
