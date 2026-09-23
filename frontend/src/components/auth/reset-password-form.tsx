"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@/lib/api";
import {
  getCustomerProfileErrorMessage,
  PASSWORD_POLICY_HINT,
  resetCustomerPassword,
  resetPasswordFormSchema,
  type ResetPasswordFormValues,
} from "@/features/account-profile";

type ViewState =
  | { kind: "form" }
  | { kind: "success" }
  | { kind: "invalid"; message: string; offerNewLink: boolean };

function classifyResetError(code: string | undefined, status?: number): ViewState {
  const message = getCustomerProfileErrorMessage(code, status);
  if (
    code === "Customer.PasswordResetLinkExpired" ||
    code === "Verification.Expired" ||
    code === "Customer.PasswordResetLinkUsed" ||
    code === "Verification.AlreadyConsumed" ||
    code === "Verification.Invalidated" ||
    code === "Customer.PasswordResetLinkInvalid" ||
    code === "Verification.InvalidToken" ||
    code === "Verification.ChallengeNotFound" ||
    code === "Customer.PasswordResetFailed"
  ) {
    return { kind: "invalid", message, offerNewLink: true };
  }
  return { kind: "invalid", message, offerNewLink: true };
}

export function ResetPasswordForm() {
  const searchParams = useSearchParams();
  const challengeId = searchParams.get("challengeId")?.trim() ?? "";
  const token = searchParams.get("token")?.trim() ?? "";
  const linkMalformed = !challengeId || !token;

  const [view, setView] = useState<ViewState>(
    linkMalformed
      ? {
          kind: "invalid",
          message: "رابط إعادة التعيين غير مكتمل أو غير صالح.",
          offerNewLink: true,
        }
      : { kind: "form" },
  );
  const [formError, setFormError] = useState<string | null>(null);

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordFormSchema),
    defaultValues: { newPassword: "", confirmNewPassword: "" },
    mode: "onBlur",
  });

  async function onSubmit(values: ResetPasswordFormValues) {
    setFormError(null);
    try {
      await resetCustomerPassword({
        challengeId,
        token,
        newPassword: values.newPassword,
      });
      setView({ kind: "success" });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.code === "Customer.PasswordPolicyFailed" || error.status === 400) {
          const mapped = getCustomerProfileErrorMessage(error.code, error.status);
          if (
            error.code === "Customer.PasswordPolicyFailed" ||
            error.code === "Authentication.InvalidPassword" ||
            error.code?.startsWith("Identity.Password")
          ) {
            setFormError(mapped);
            return;
          }
          setView(classifyResetError(error.code, error.status));
          return;
        }
        setView(classifyResetError(error.code, error.status));
        return;
      }
      setView({
        kind: "invalid",
        message: getCustomerProfileErrorMessage(undefined),
        offerNewLink: true,
      });
    }
  }

  if (view.kind === "success") {
    return (
      <div className="space-y-5 text-center">
        <h2 className="text-xl font-semibold tracking-tight">
          تم تغيير كلمة المرور بنجاح
        </h2>
        <p className="text-sm text-muted-foreground">
          يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.
        </p>
        <Link
          href="/login"
          className="inline-flex rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95"
        >
          تسجيل الدخول
        </Link>
      </div>
    );
  }

  if (view.kind === "invalid") {
    return (
      <div className="space-y-5 text-center">
        <h2 className="text-xl font-semibold tracking-tight">تعذر إعادة التعيين</h2>
        <p className="text-sm text-destructive" role="alert">
          {view.message}
        </p>
        {view.offerNewLink ? (
          <Link
            href="/forgot-password"
            className="inline-flex rounded-md border border-border px-4 py-2.5 text-sm font-medium hover:bg-muted/40"
          >
            طلب رابط جديد
          </Link>
        ) : null}
        <div>
          <Link href="/login" className="text-sm text-primary hover:underline">
            العودة لتسجيل الدخول
          </Link>
        </div>
      </div>
    );
  }

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-5"
      noValidate
    >
      <p className="text-xs text-muted-foreground">{PASSWORD_POLICY_HINT}</p>

      <div className="space-y-2">
        <label htmlFor="newPassword" className="text-sm font-medium">
          كلمة المرور الجديدة
        </label>
        <input
          id="newPassword"
          type="password"
          autoComplete="new-password"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("newPassword")}
        />
        {form.formState.errors.newPassword ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.newPassword.message}
          </p>
        ) : null}
      </div>

      <div className="space-y-2">
        <label htmlFor="confirmNewPassword" className="text-sm font-medium">
          تأكيد كلمة المرور الجديدة
        </label>
        <input
          id="confirmNewPassword"
          type="password"
          autoComplete="new-password"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("confirmNewPassword")}
        />
        {form.formState.errors.confirmNewPassword ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.confirmNewPassword.message}
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
        {form.formState.isSubmitting ? "جاري الحفظ…" : "تعيين كلمة المرور"}
      </button>
    </form>
  );
}
