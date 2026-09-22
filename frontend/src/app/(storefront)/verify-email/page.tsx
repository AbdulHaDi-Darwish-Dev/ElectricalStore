"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { ApiError } from "@/lib/api";
import {
  confirmCustomerEmail,
  getCustomerProfileErrorMessage,
  resendCustomerEmailVerification,
  EMAIL_VERIFICATION_RESEND_GENERIC,
  type ConfirmEmailResponse,
} from "@/features/account-profile";

type Status =
  | { kind: "loading" }
  | { kind: "success"; alreadyConfirmed: boolean }
  | { kind: "error"; message: string; expired: boolean };

/** Deduplicate confirm calls across React Strict Mode remounts (token is single-use). */
const confirmInFlight = new Map<string, Promise<ConfirmEmailResponse>>();

function confirmEmailOnce(
  challengeId: string,
  token: string,
): Promise<ConfirmEmailResponse> {
  const key = `${challengeId}\0${token}`;
  const existing = confirmInFlight.get(key);
  if (existing) return existing;

  const request = confirmCustomerEmail({ challengeId, token }).catch(
    (error: unknown) => {
      confirmInFlight.delete(key);
      throw error;
    },
  );
  confirmInFlight.set(key, request);
  return request;
}

function VerifyContent() {
  const searchParams = useSearchParams();
  const challengeId = searchParams.get("challengeId")?.trim() ?? "";
  const token = searchParams.get("token")?.trim() ?? "";
  const [status, setStatus] = useState<Status>({ kind: "loading" });
  const [resendEmail, setResendEmail] = useState("");
  const [resendMsg, setResendMsg] = useState<string | null>(null);
  const [resendBusy, setResendBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      if (!challengeId || !token) {
        if (!cancelled) {
          setStatus({
            kind: "error",
            message: "رابط التأكيد غير مكتمل أو غير صالح.",
            expired: false,
          });
        }
        return;
      }

      try {
        const result = await confirmEmailOnce(challengeId, token);
        if (!cancelled) {
          setStatus({
            kind: "success",
            alreadyConfirmed: Boolean(result.alreadyConfirmed),
          });
        }
      } catch (error) {
        if (cancelled) return;
        if (error instanceof ApiError) {
          setStatus({
            kind: "error",
            message: getCustomerProfileErrorMessage(error.code, error.status),
            expired: error.code === "Verification.Expired",
          });
          return;
        }
        setStatus({
          kind: "error",
          message: getCustomerProfileErrorMessage(undefined),
          expired: false,
        });
      }
    }

    void run();
    return () => {
      cancelled = true;
    };
  }, [challengeId, token]);

  async function onResend() {
    const email = resendEmail.trim();
    if (!email) return;
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

  if (status.kind === "loading") {
    return (
      <p className="px-4 py-16 text-center text-sm text-muted-foreground">
        جاري تأكيد البريد الإلكتروني…
      </p>
    );
  }

  if (status.kind === "success") {
    return (
      <div className="mx-auto max-w-md space-y-5 px-4 py-16 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">
          {status.alreadyConfirmed
            ? "تم تأكيد بريدك الإلكتروني بالفعل"
            : "تم تأكيد بريدك الإلكتروني بنجاح"}
        </h1>
        <Link
          href="/login"
          className="inline-flex rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95"
        >
          تسجيل الدخول
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md space-y-5 px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold tracking-tight">تعذر التأكيد</h1>
      <p className="text-sm text-destructive" role="alert">
        {status.message}
      </p>
      <div className="space-y-3 text-start">
        <label htmlFor="resendEmail" className="text-sm font-medium">
          إعادة إرسال رسالة التأكيد
        </label>
        <input
          id="resendEmail"
          type="email"
          dir="ltr"
          value={resendEmail}
          onChange={(e) => setResendEmail(e.target.value)}
          className="w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm outline-none ring-primary focus:ring-2"
          placeholder="email@example.com"
        />
        <button
          type="button"
          disabled={resendBusy || !resendEmail.trim()}
          onClick={() => void onResend()}
          className="w-full rounded-md border border-border px-4 py-2.5 text-sm font-medium hover:bg-muted/40 disabled:opacity-60"
        >
          {resendBusy ? "جاري الإرسال…" : "إعادة الإرسال"}
        </button>
        {resendMsg ? (
          <p className="text-sm text-muted-foreground" role="status">
            {resendMsg}
          </p>
        ) : null}
      </div>
      <Link href="/login" className="text-sm text-primary hover:underline">
        العودة لتسجيل الدخول
      </Link>
    </div>
  );
}

export default function VerifyEmailPage() {
  return (
    <Suspense
      fallback={
        <p className="px-4 py-16 text-center text-sm text-muted-foreground">
          جاري التحميل…
        </p>
      }
    >
      <VerifyContent />
    </Suspense>
  );
}
