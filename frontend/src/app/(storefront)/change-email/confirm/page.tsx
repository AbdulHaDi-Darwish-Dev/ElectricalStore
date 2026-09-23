"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { ApiError } from "@/lib/api";
import { useAuthActions } from "@/lib/auth";
import {
  confirmCustomerEmailChange,
  getCustomerProfileErrorMessage,
  type ConfirmEmailChangeResponse,
} from "@/features/account-profile";

type Status =
  | { kind: "loading" }
  | { kind: "success"; email?: string | null }
  | { kind: "error"; message: string };

const confirmInFlight = new Map<string, Promise<ConfirmEmailChangeResponse>>();

function confirmOnce(
  challengeId: string,
  token: string,
): Promise<ConfirmEmailChangeResponse> {
  const key = `${challengeId}\0${token}`;
  const existing = confirmInFlight.get(key);
  if (existing) return existing;
  const request = confirmCustomerEmailChange({ challengeId, token }).catch(
    (error: unknown) => {
      confirmInFlight.delete(key);
      throw error;
    },
  );
  confirmInFlight.set(key, request);
  return request;
}

function ConfirmContent() {
  const searchParams = useSearchParams();
  const challengeId = searchParams.get("challengeId")?.trim() ?? "";
  const token = searchParams.get("token")?.trim() ?? "";
  const { logout } = useAuthActions();
  const [status, setStatus] = useState<Status>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;

    async function run() {
      if (!challengeId || !token) {
        if (!cancelled) {
          setStatus({
            kind: "error",
            message: "رابط تأكيد تغيير البريد غير مكتمل أو غير صالح.",
          });
        }
        return;
      }

      try {
        const result = await confirmOnce(challengeId, token);
        await logout().catch(() => undefined);
        if (!cancelled) {
          setStatus({ kind: "success", email: result.email });
        }
      } catch (error) {
        if (cancelled) return;
        if (error instanceof ApiError) {
          setStatus({
            kind: "error",
            message: getCustomerProfileErrorMessage(error.code, error.status),
          });
          return;
        }
        setStatus({
          kind: "error",
          message: getCustomerProfileErrorMessage(undefined),
        });
      }
    }

    void run();
    return () => {
      cancelled = true;
    };
  }, [challengeId, token, logout]);

  if (status.kind === "loading") {
    return (
      <p className="px-4 py-16 text-center text-sm text-muted-foreground">
        جاري تأكيد تغيير البريد الإلكتروني…
      </p>
    );
  }

  if (status.kind === "success") {
    return (
      <div className="mx-auto max-w-md space-y-5 px-4 py-16 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">
          تم تغيير بريدك الإلكتروني بنجاح
        </h1>
        {status.email ? (
          <p className="text-sm text-muted-foreground" dir="ltr">
            {status.email}
          </p>
        ) : null}
        <p className="text-sm text-muted-foreground">
          يرجى تسجيل الدخول باستخدام البريد الجديد.
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

  return (
    <div className="mx-auto max-w-md space-y-5 px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold tracking-tight">
        تعذر تأكيد تغيير البريد
      </h1>
      <p className="text-sm text-destructive" role="alert">
        {status.message}
      </p>
      <Link
        href="/account"
        className="inline-flex rounded-md border border-border px-4 py-2.5 text-sm font-medium hover:bg-muted/40"
      >
        العودة للحساب
      </Link>
      <div>
        <Link href="/login" className="text-sm text-primary hover:underline">
          تسجيل الدخول
        </Link>
      </div>
    </div>
  );
}

export default function ChangeEmailConfirmPage() {
  return (
    <Suspense
      fallback={
        <p className="px-4 py-16 text-center text-sm text-muted-foreground">
          جاري التحميل…
        </p>
      }
    >
      <ConfirmContent />
    </Suspense>
  );
}
