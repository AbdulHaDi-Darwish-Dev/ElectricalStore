"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@/lib/api";
import { useAuthActions, useAuthStore } from "@/lib/auth";
import {
  PASSWORD_POLICY_HINT,
  accountProfileKeys,
  changeCustomerPassword,
  changeEmailFormSchema,
  changePasswordFormSchema,
  getCustomerProfile,
  getCustomerProfileErrorMessage,
  requestCustomerEmailChange,
  updateCustomerProfile,
  updateProfileFormSchema,
  type ChangeEmailFormValues,
  type ChangePasswordFormValues,
  type UpdateProfileFormValues,
} from "@/features/account-profile";

export function AccountProfileView() {
  const queryClient = useQueryClient();
  const accessToken = useAuthStore((s) => s.accessToken);
  const { logout } = useAuthActions();
  const router = useRouter();

  const profileQuery = useQuery({
    queryKey: accountProfileKeys.detail(),
    queryFn: () => getCustomerProfile(),
    enabled: Boolean(accessToken),
    staleTime: 30_000,
  });

  const [profileMessage, setProfileMessage] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [emailChangeOpen, setEmailChangeOpen] = useState(false);
  const [emailChangeMessage, setEmailChangeMessage] = useState<string | null>(
    null,
  );
  const [emailChangeError, setEmailChangeError] = useState<string | null>(null);

  const profileForm = useForm<UpdateProfileFormValues>({
    resolver: zodResolver(updateProfileFormSchema),
    values: {
      fullName: profileQuery.data?.fullName ?? "",
    },
  });

  const passwordForm = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordFormSchema),
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      confirmNewPassword: "",
    },
  });

  const emailChangeForm = useForm<ChangeEmailFormValues>({
    resolver: zodResolver(changeEmailFormSchema),
    defaultValues: {
      newEmail: "",
      currentPassword: "",
    },
  });

  const updateMutation = useMutation({
    mutationFn: (values: UpdateProfileFormValues) =>
      updateCustomerProfile({ fullName: values.fullName }),
    onSuccess: async (data) => {
      setProfileError(null);
      setProfileMessage("تم تحديث الاسم بنجاح.");
      await queryClient.setQueryData(accountProfileKeys.detail(), data);
      await queryClient.invalidateQueries({ queryKey: accountProfileKeys.detail() });
    },
    onError: (error) => {
      setProfileMessage(null);
      if (error instanceof ApiError) {
        setProfileError(getCustomerProfileErrorMessage(error.code, error.status));
        return;
      }
      setProfileError(getCustomerProfileErrorMessage(undefined));
    },
  });

  const passwordMutation = useMutation({
    mutationFn: (values: ChangePasswordFormValues) =>
      changeCustomerPassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      }),
    onSuccess: async (result) => {
      setPasswordError(null);
      passwordForm.reset();
      if (result.reauthenticationRequired) {
        setPasswordMessage("تم تغيير كلمة المرور. يرجى تسجيل الدخول مجدداً.");
        await logout();
        router.replace("/login");
        return;
      }
      setPasswordMessage("تم تغيير كلمة المرور بنجاح.");
    },
    onError: (error) => {
      setPasswordMessage(null);
      if (error instanceof ApiError) {
        setPasswordError(getCustomerProfileErrorMessage(error.code, error.status));
        return;
      }
      setPasswordError(getCustomerProfileErrorMessage(undefined));
    },
  });

  const emailChangeMutation = useMutation({
    mutationFn: (values: ChangeEmailFormValues) =>
      requestCustomerEmailChange({
        newEmail: values.newEmail.trim(),
        currentPassword: values.currentPassword,
      }),
    onSuccess: () => {
      setEmailChangeError(null);
      emailChangeForm.reset();
      setEmailChangeMessage(
        "أرسلنا رسالة تأكيد إلى بريدك الإلكتروني الجديد. يبقى بريدك الحالي فعّالاً حتى التأكيد. تحقق من الوارد/الرسائل غير المرغوب فيها؛ ينتهي صلاحية الرابط خلال ساعة تقريباً.",
      );
    },
    onError: (error) => {
      setEmailChangeMessage(null);
      if (error instanceof ApiError) {
        setEmailChangeError(
          getCustomerProfileErrorMessage(error.code, error.status),
        );
        return;
      }
      setEmailChangeError(getCustomerProfileErrorMessage(undefined));
    },
  });

  if (profileQuery.isLoading) {
    return (
      <div className="space-y-4" aria-busy="true">
        <div className="h-8 w-48 animate-pulse rounded bg-muted" />
        <div className="h-40 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (profileQuery.isError) {
    const err = profileQuery.error;
    const message =
      err instanceof ApiError
        ? getCustomerProfileErrorMessage(err.code, err.status)
        : getCustomerProfileErrorMessage(undefined);
    return (
      <p className="text-sm text-destructive" role="alert">
        {message}
      </p>
    );
  }

  const profile = profileQuery.data;

  return (
    <div className="space-y-8">
      <section className="rounded-md border border-border bg-card p-6 space-y-4">
        <h2 className="text-lg font-medium">بيانات الحساب</h2>
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">الاسم الكامل</dt>
            <dd className="mt-1 font-medium">{profile?.fullName}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">البريد الإلكتروني</dt>
            <dd className="mt-1 font-medium" dir="ltr">
              {profile?.email}
            </dd>
            <button
              type="button"
              className="mt-2 text-sm text-primary hover:underline"
              onClick={() => {
                setEmailChangeOpen((v) => !v);
                setEmailChangeError(null);
                if (!emailChangeOpen) setEmailChangeMessage(null);
              }}
            >
              تغيير البريد الإلكتروني
            </button>
          </div>
          <div>
            <dt className="text-muted-foreground">تأكيد البريد</dt>
            <dd className="mt-1">
              {profile?.emailConfirmed ? "مؤكَّد" : "غير مؤكَّد"}
            </dd>
          </div>
        </dl>

        <form
          className="max-w-lg space-y-3 border-t border-border pt-4"
          onSubmit={profileForm.handleSubmit((v) => updateMutation.mutate(v))}
          noValidate
        >
          <h3 className="text-sm font-medium">تعديل الاسم</h3>
          <label htmlFor="fullName" className="block text-sm">
            الاسم الكامل
          </label>
          <input
            id="fullName"
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
            {...profileForm.register("fullName")}
          />
          {profileForm.formState.errors.fullName ? (
            <p className="text-sm text-destructive" role="alert">
              {profileForm.formState.errors.fullName.message}
            </p>
          ) : null}
          {profileError ? (
            <p className="text-sm text-destructive" role="alert">
              {profileError}
            </p>
          ) : null}
          {profileMessage ? (
            <p className="text-sm text-muted-foreground" role="status">
              {profileMessage}
            </p>
          ) : null}
          <button
            type="submit"
            disabled={updateMutation.isPending}
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-95 disabled:opacity-60"
          >
            {updateMutation.isPending ? "جاري الحفظ…" : "حفظ الاسم"}
          </button>
        </form>

        {emailChangeOpen ? (
          <form
            className="max-w-lg space-y-3 border-t border-border pt-4"
            onSubmit={emailChangeForm.handleSubmit((v) =>
              emailChangeMutation.mutate(v),
            )}
            noValidate
          >
            <h3 className="text-sm font-medium">تغيير البريد الإلكتروني</h3>
            <p className="text-xs text-muted-foreground">
              سنرسل رابط تأكيد إلى البريد الجديد. يبقى بريدك الحالي فعّالاً حتى
              التأكيد.
            </p>
            <div className="space-y-1.5">
              <label htmlFor="newEmail" className="text-sm font-medium">
                البريد الإلكتروني الجديد
              </label>
              <input
                id="newEmail"
                type="email"
                dir="ltr"
                autoComplete="email"
                className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                {...emailChangeForm.register("newEmail")}
              />
              {emailChangeForm.formState.errors.newEmail ? (
                <p className="text-sm text-destructive" role="alert">
                  {emailChangeForm.formState.errors.newEmail.message}
                </p>
              ) : null}
            </div>
            <div className="space-y-1.5">
              <label
                htmlFor="emailChangeCurrentPassword"
                className="text-sm font-medium"
              >
                كلمة المرور الحالية
              </label>
              <input
                id="emailChangeCurrentPassword"
                type="password"
                autoComplete="current-password"
                className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                {...emailChangeForm.register("currentPassword")}
              />
              {emailChangeForm.formState.errors.currentPassword ? (
                <p className="text-sm text-destructive" role="alert">
                  {emailChangeForm.formState.errors.currentPassword.message}
                </p>
              ) : null}
            </div>
            {emailChangeError ? (
              <p className="text-sm text-destructive" role="alert">
                {emailChangeError}
              </p>
            ) : null}
            {emailChangeMessage ? (
              <p className="text-sm text-muted-foreground" role="status">
                {emailChangeMessage}
              </p>
            ) : null}
            <button
              type="submit"
              disabled={emailChangeMutation.isPending}
              className="rounded-md border border-border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-60"
            >
              {emailChangeMutation.isPending
                ? "جاري الإرسال…"
                : "إرسال رابط التأكيد"}
            </button>
          </form>
        ) : null}
      </section>

      <section className="rounded-md border border-border bg-card p-6">
        <h2 className="text-lg font-medium">طلباتي</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          تصفّح طلباتك السابقة وتفاصيل كل طلب.
        </p>
        <Link
          href="/account/orders"
          className="mt-4 inline-flex rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-95"
        >
          عرض طلباتي
        </Link>
      </section>

      <section className="rounded-md border border-border bg-card p-6 space-y-4">
        <h2 className="text-lg font-medium">الأمان</h2>
        <p className="text-sm text-muted-foreground">{PASSWORD_POLICY_HINT}</p>
        <form
          className="max-w-lg space-y-3"
          onSubmit={passwordForm.handleSubmit((v) => passwordMutation.mutate(v))}
          noValidate
        >
          <div className="space-y-1.5">
            <label htmlFor="currentPassword" className="text-sm font-medium">
              كلمة المرور الحالية
            </label>
            <input
              id="currentPassword"
              type="password"
              autoComplete="current-password"
              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
              {...passwordForm.register("currentPassword")}
            />
          </div>
          <div className="space-y-1.5">
            <label htmlFor="newPassword" className="text-sm font-medium">
              كلمة المرور الجديدة
            </label>
            <input
              id="newPassword"
              type="password"
              autoComplete="new-password"
              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
              {...passwordForm.register("newPassword")}
            />
          </div>
          <div className="space-y-1.5">
            <label htmlFor="confirmNewPassword" className="text-sm font-medium">
              تأكيد كلمة المرور الجديدة
            </label>
            <input
              id="confirmNewPassword"
              type="password"
              autoComplete="new-password"
              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
              {...passwordForm.register("confirmNewPassword")}
            />
            {passwordForm.formState.errors.confirmNewPassword ? (
              <p className="text-sm text-destructive" role="alert">
                {passwordForm.formState.errors.confirmNewPassword.message}
              </p>
            ) : null}
          </div>
          {passwordError ? (
            <p className="text-sm text-destructive" role="alert">
              {passwordError}
            </p>
          ) : null}
          {passwordMessage ? (
            <p className="text-sm text-muted-foreground" role="status">
              {passwordMessage}
            </p>
          ) : null}
          <button
            type="submit"
            disabled={passwordMutation.isPending}
            className="rounded-md border border-border px-4 py-2 text-sm font-medium hover:bg-muted disabled:opacity-60"
          >
            {passwordMutation.isPending
              ? "جاري التغيير…"
              : "تغيير كلمة المرور"}
          </button>
        </form>
      </section>
    </div>
  );
}
