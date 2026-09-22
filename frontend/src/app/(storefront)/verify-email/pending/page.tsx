"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { ApiError } from "@/lib/api";
import {
  EMAIL_VERIFICATION_RESEND_GENERIC,
  getCustomerProfileErrorMessage,
  resendCustomerEmailVerification,
} from "@/features/account-profile";

function PendingContent() {
  const searchParams = useSearchParams();
  const email = searchParams.get("email")?.trim() ?? "";
  const sentFlag = searchParams.get("sent");
  const initiallySent = sentFlag !== "0";

  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onResend() {
    if (!email) {
      setError("البريد الإلكتروني غير متوفر لإعادة الإرسال.");
      return;
    }
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await resendCustomerEmailVerification({ email });
      setMessage(EMAIL_VERIFICATION_RESEND_GENERIC);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(getCustomerProfileErrorMessage(err.code, err.status));
      } else {
        setError(getCustomerProfileErrorMessage(undefined));
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-md space-y-5 px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold tracking-tight">تم إنشاء حسابك</h1>
      {initiallySent ? (
        <p className="text-sm leading-7 text-muted-foreground">
          أرسلنا رسالة تأكيد إلى{" "}
          <span dir="ltr" className="font-medium text-foreground">
            {email || "بريدك الإلكتروني"}
          </span>
          . تحقق من صندوق الوارد ومجلد الرسائل غير المرغوب فيها.
        </p>
      ) : (
        <p className="text-sm leading-7 text-muted-foreground">
          تم إنشاء الحساب، لكن تعذر إرسال رسالة التأكيد. حاول إعادة الإرسال.
        </p>
      )}
      <p className="text-xs leading-6 text-muted-foreground">
        رابط التأكيد صالح لمدة ساعة واحدة.
      </p>

      {message ? (
        <p className="text-sm text-foreground" role="status">
          {message}
        </p>
      ) : null}
      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}

      <div className="flex flex-col gap-3">
        <button
          type="button"
          disabled={busy || !email}
          onClick={() => void onResend()}
          className="rounded-md border border-border px-4 py-2.5 text-sm font-medium hover:bg-muted/40 disabled:opacity-60"
        >
          {busy ? "جاري الإرسال…" : "إعادة إرسال رسالة التأكيد"}
        </button>
        <Link
          href="/login"
          className="rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:opacity-95"
        >
          الانتقال لتسجيل الدخول
        </Link>
      </div>
    </div>
  );
}

export default function VerifyEmailPendingPage() {
  return (
    <Suspense
      fallback={
        <p className="px-4 py-16 text-center text-sm text-muted-foreground">
          جاري التحميل…
        </p>
      }
    >
      <PendingContent />
    </Suspense>
  );
}
