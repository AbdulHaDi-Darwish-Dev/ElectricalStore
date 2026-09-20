"use client";

import { useEffect, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  selectAuthReady,
  selectIsAuthenticated,
  useAuthStore,
} from "@/lib/auth";

type AuthGateProps = {
  children: ReactNode;
};

/**
 * Waits for auth bootstrap; redirects anonymous users to login with safe returnTo.
 */
export function AuthGate({ children }: AuthGateProps) {
  const router = useRouter();
  const pathname = usePathname();
  const ready = useAuthStore(selectAuthReady);
  const authenticated = useAuthStore(selectIsAuthenticated);

  useEffect(() => {
    if (!ready) return;
    if (!authenticated) {
      const returnTo = encodeURIComponent(pathname || "/account");
      router.replace(`/login?returnTo=${returnTo}`);
    }
  }, [ready, authenticated, pathname, router]);

  if (!ready) {
    return (
      <div className="mx-auto w-full max-w-3xl space-y-4 px-4 py-10 sm:px-6" aria-busy="true">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <div className="h-32 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (!authenticated) {
    return (
      <div className="mx-auto w-full max-w-3xl px-4 py-10 text-sm text-muted-foreground sm:px-6">
        جاري التحويل لتسجيل الدخول…
      </div>
    );
  }

  return <>{children}</>;
}
