"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@/lib/api";
import {
  getAuthErrorMessage,
  MFA_UNAVAILABLE_MESSAGE,
  postLogin,
  resolveSafeReturnTo,
  useAuthActions,
} from "@/lib/auth";
import {
  EMAIL_VERIFICATION_RESEND_GENERIC,
  getCustomerProfileErrorMessage,
  resendCustomerEmailVerification,
} from "@/features/account-profile";
import {
  loginFormSchema,
  type LoginFormValues,
} from "@/features/auth";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { loginWithTokens } = useAuthActions();
  const [formError, setFormError] = useState<string | null>(null);
  const [mfaNotice, setMfaNotice] = useState(false);
  const [emailNotConfirmed, setEmailNotConfirmed] = useState(false);
  const [resendMsg, setResendMsg] = useState<string | null>(null);
  const [resendBusy, setResendBusy] = useState(false);

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: { emailOrUserName: "", password: "" },
    mode: "onBlur",
  });

  async function onSubmit(values: LoginFormValues) {
    setFormError(null);
    setMfaNotice(false);
    setEmailNotConfirmed(false);
    setResendMsg(null);
    try {
      const result = await postLogin(values.emailOrUserName, values.password);
      if (result.kind === "mfaRequired") {
        setMfaNotice(true);
        return;
      }
      await loginWithTokens(result);
      const returnTo = resolveSafeReturnTo(searchParams.get("returnTo"));
      router.replace(returnTo);
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.code === "Authentication.EmailNotConfirmed") {
          setEmailNotConfirmed(true);
          setFormError(getAuthErrorMessage(error.code, error.status));
          return;
        }
        setFormError(getAuthErrorMessage(error.code, error.status));
        return;
      }
      setFormError(getAuthErrorMessage(undefined));
    }
  }

  async function onResend() {
    const email = form.getValues("emailOrUserName").trim();
    if (!email.includes("@")) {
      setResendMsg("أدخل البريد الإلكتروني لإعادة إرسال رسالة التأكيد.");
      return;
    }
    setResendBusy(true);
    setResendMsg(null);
    try {
      await resendCustomerEmailVerification({ email });
      setResendMsg(EMAIL_VERIFICATION_RESEND_GENERIC);
    } catch (error) {
      if (error instanceof ApiError) {
        setResendMsg(getCustomerProfileErrorMessage(error.code, error.status));
      } else {
        setResendMsg(getCustomerProfileErrorMessage(undefined));
      }
    } finally {
      setResendBusy(false);
    }
  }

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-5"
      noValidate
    >
      <div className="space-y-2">
        <label htmlFor="emailOrUserName" className="text-sm font-medium">
          البريد أو اسم المستخدم
        </label>
        <input
          id="emailOrUserName"
          autoComplete="username"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("emailOrUserName")}
        />
        {form.formState.errors.emailOrUserName ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.emailOrUserName.message}
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
          autoComplete="current-password"
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          {...form.register("password")}
        />
        {form.formState.errors.password ? (
          <p className="text-sm text-destructive" role="alert">
            {form.formState.errors.password.message}
          </p>
        ) : null}
      </div>

      {formError ? (
        <p
          className="rounded-md border border-border bg-muted/40 px-3 py-2 text-sm text-foreground"
          role="alert"
        >
          {formError}
        </p>
      ) : null}

      {emailNotConfirmed ? (
        <div className="space-y-3 rounded-md border border-border px-3 py-3 text-sm">
          <p>البريد الإلكتروني غير مؤكد. يرجى تأكيد بريدك قبل تسجيل الدخول.</p>
          <button
            type="button"
            disabled={resendBusy}
            onClick={() => void onResend()}
            className="w-full rounded-md border border-border px-3 py-2 font-medium hover:bg-muted/40 disabled:opacity-60"
          >
            {resendBusy ? "جاري الإرسال…" : "إعادة إرسال رسالة التأكيد"}
          </button>
          {resendMsg ? (
            <p className="text-muted-foreground" role="status">
              {resendMsg}
            </p>
          ) : null}
        </div>
      ) : null}

      {mfaNotice ? (
        <p
          className="rounded-md border border-border bg-muted/40 px-3 py-2 text-sm text-foreground"
          role="status"
        >
          {MFA_UNAVAILABLE_MESSAGE}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={form.formState.isSubmitting}
        className="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
      >
        {form.formState.isSubmitting ? "جاري الدخول…" : "تسجيل الدخول"}
      </button>

      <p className="text-center text-sm text-muted-foreground">
        ليس لديك حساب؟{" "}
        <Link href="/register" className="text-primary hover:underline">
          إنشاء حساب
        </Link>
      </p>
    </form>
  );
}
