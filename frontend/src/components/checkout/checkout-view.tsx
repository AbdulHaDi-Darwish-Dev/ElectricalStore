"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState, type ReactNode } from "react";
import { ApiError } from "@/lib/api";
import { formatPrice, formatSellingUnit } from "@/lib/format";
import { useCartStore, type CartItem } from "@/features/cart";
import { getDeliveryZones } from "@/features/shipping";
import {
  buildCheckoutPreviewQueryKey,
  buildCheckoutPreviewRequest,
  checkoutCustomerSchema,
  clearIdempotencyAttempt,
  hasPriceChanged,
  previewCheckout,
  resolveIdempotencyKey,
  type CheckoutCustomerFormValues,
} from "@/features/checkout";
import {
  getOrderingErrorMessage,
  placeOrder,
  type PlaceOrderRequest,
} from "@/features/orders";
import { useAuthStore } from "@/lib/auth";
import { EmptyState } from "@/components/storefront/empty-state";

export function CheckoutView() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const hasHydrated = useCartStore((state) => state.hasHydrated);
  const items = useCartStore((state) => state.items);
  const clearCart = useCartStore((state) => state.clearCart);
  const accessToken = useAuthStore((state) => state.accessToken);
  const authStatus = useAuthStore((state) => state.status);

  const [businessError, setBusinessError] = useState<string | null>(null);
  const [forceNewIdempotency, setForceNewIdempotency] = useState(false);

  const form = useForm<CheckoutCustomerFormValues>({
    resolver: zodResolver(checkoutCustomerSchema),
    defaultValues: {
      customerName: "",
      phone: "",
      addressText: "",
      customerNote: "",
      deliveryZoneId: "",
    },
    mode: "onBlur",
  });

  const deliveryZoneId = useWatch({
    control: form.control,
    name: "deliveryZoneId",
  });

  const zonesQuery = useQuery({
    queryKey: ["delivery-zones"],
    queryFn: getDeliveryZones,
    staleTime: 60_000,
    enabled: hasHydrated && items.length > 0,
  });

  const previewEnabled =
    hasHydrated && items.length > 0 && Boolean(deliveryZoneId);

  const previewQuery = useQuery({
    queryKey: buildCheckoutPreviewQueryKey(items, deliveryZoneId),
    queryFn: () =>
      previewCheckout(buildCheckoutPreviewRequest(items, deliveryZoneId)),
    enabled: previewEnabled,
    staleTime: 0,
    gcTime: 30_000,
    refetchOnWindowFocus: true,
    retry: 1,
  });

  const placeMutation = useMutation({
    mutationFn: async (payload: PlaceOrderRequest) => {
      const key = await resolveIdempotencyKey(payload, {
        forceNew: forceNewIdempotency,
      });
      setForceNewIdempotency(false);
      const token = authStatus === "authenticated" ? accessToken : null;
      return placeOrder(payload, key, token);
    },
    onSuccess: (order) => {
      clearIdempotencyAttempt();
      clearCart();
      void queryClient.removeQueries({ queryKey: ["checkout-preview"] });
      if (authStatus === "authenticated") {
        router.replace(`/account/orders/${order.id}`);
      } else {
        router.replace(`/orders/${order.id}/confirmation`);
      }
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        if (error.code === "Ordering.IdempotencyReplayUnavailable") {
          clearIdempotencyAttempt();
          setForceNewIdempotency(true);
        }
        setBusinessError(getOrderingErrorMessage(error.code));
        return;
      }
      setBusinessError(
        "تعذر تأكيد إرسال الطلب. إذا كنت غير متأكد من النتيجة، أعد المحاولة بنفس البيانات.",
      );
    },
  });

  const cartByVariant = useMemo(() => {
    const map = new Map<string, CartItem>();
    for (const item of items) {
      map.set(item.variantId, item);
    }
    return map;
  }, [items]);

  if (!hasHydrated) {
    return (
      <div className="space-y-4" aria-busy="true">
        <div className="h-8 w-56 animate-pulse rounded bg-muted" />
        <div className="h-40 animate-pulse rounded-md bg-muted" />
        <div className="h-64 animate-pulse rounded-md bg-muted" />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <EmptyState
        title="لا يمكن إتمام الطلب"
        description="سلة التسوق فارغة. أضف منتجات قبل الانتقال إلى الدفع."
        actionHref="/products"
        actionLabel="تصفح المنتجات"
      />
    );
  }

  const preview = previewQuery.data;
  const canSubmit =
    Boolean(preview?.meetsMinimumOrder) &&
    !previewQuery.isFetching &&
    !previewQuery.isError &&
    !placeMutation.isPending;

  async function onSubmit(values: CheckoutCustomerFormValues) {
    setBusinessError(null);
    if (!preview?.meetsMinimumOrder) {
      setBusinessError("لم يُستوفَ الحد الأدنى لمجموع البضاعة.");
      return;
    }

    const payload: PlaceOrderRequest = {
      items: items.map((item) => ({
        variantId: item.variantId,
        quantity: item.quantity,
      })),
      deliveryZoneId: values.deliveryZoneId,
      customerName: values.customerName.trim(),
      phone: values.phone.trim(),
      addressText: values.addressText.trim(),
      customerNote: values.customerNote?.trim()
        ? values.customerNote.trim()
        : null,
    };

    placeMutation.mutate(payload);
  }

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="grid gap-8 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,0.9fr)] lg:items-start"
      noValidate
    >
      <div className="space-y-8">
        <section className="space-y-4 rounded-md border border-border bg-card p-5 sm:p-6">
          <header className="space-y-1">
            <h2 className="text-lg font-medium text-foreground">
              بيانات المستلم
            </h2>
            <p className="text-sm text-muted-foreground">
              الدفع عند الاستلام. لا يلزم إنشاء حساب.
            </p>
          </header>

          <div className="space-y-4">
            <Field
              id="customerName"
              label="الاسم"
              error={form.formState.errors.customerName?.message}
            >
              <input
                id="customerName"
                autoComplete="name"
                className={inputClass}
                {...form.register("customerName")}
              />
            </Field>

            <Field
              id="phone"
              label="رقم الهاتف"
              error={form.formState.errors.phone?.message}
            >
              <input
                id="phone"
                type="tel"
                autoComplete="tel"
                inputMode="tel"
                className={inputClass}
                {...form.register("phone")}
              />
            </Field>

            <Field
              id="addressText"
              label="عنوان التوصيل"
              error={form.formState.errors.addressText?.message}
            >
              <textarea
                id="addressText"
                rows={4}
                autoComplete="street-address"
                className={`${inputClass} min-h-28 resize-y`}
                {...form.register("addressText")}
              />
            </Field>

            <Field
              id="customerNote"
              label="ملاحظة (اختياري)"
              error={form.formState.errors.customerNote?.message}
            >
              <textarea
                id="customerNote"
                rows={2}
                className={`${inputClass} resize-y`}
                {...form.register("customerNote")}
              />
            </Field>
          </div>
        </section>

        <section className="space-y-4 rounded-md border border-border bg-card p-5 sm:p-6">
          <header className="space-y-1">
            <h2 className="text-lg font-medium text-foreground">
              منطقة التوصيل
            </h2>
            <p className="text-sm text-muted-foreground">
              اختر المنطقة لحساب الشحن والإجمالي عبر الخادم.
            </p>
          </header>

          {zonesQuery.isLoading ? (
            <div className="h-11 animate-pulse rounded-md bg-muted" />
          ) : zonesQuery.isError ? (
            <p className="text-sm text-destructive" role="alert">
              تعذر تحميل مناطق التوصيل. أعد تحميل الصفحة.
            </p>
          ) : (
            <Field
              id="deliveryZoneId"
              label="المنطقة"
              error={form.formState.errors.deliveryZoneId?.message}
            >
              <select
                id="deliveryZoneId"
                className={inputClass}
                {...form.register("deliveryZoneId")}
              >
                <option value="">اختر منطقة التوصيل</option>
                {(zonesQuery.data ?? []).map((zone) => (
                  <option key={zone.id} value={zone.id}>
                    {zone.name} — {formatPrice(zone.fee)}
                  </option>
                ))}
              </select>
            </Field>
          )}
        </section>
      </div>

      <aside className="space-y-4 lg:sticky lg:top-24">
        <section className="rounded-md border border-border bg-card p-5 sm:p-6">
          <h2 className="text-lg font-medium text-foreground">ملخص الطلب</h2>

          {!deliveryZoneId ? (
            <p className="mt-4 text-sm text-muted-foreground">
              اختر منطقة التوصيل لعرض الأسعار النهائية من الخادم.
            </p>
          ) : previewQuery.isLoading ? (
            <div className="mt-4 space-y-3" aria-busy="true">
              <div className="h-16 animate-pulse rounded bg-muted" />
              <div className="h-16 animate-pulse rounded bg-muted" />
            </div>
          ) : previewQuery.isError ? (
            <div className="mt-4 space-y-3" role="alert">
              <p className="text-sm text-destructive">
                {previewQuery.error instanceof ApiError
                  ? getOrderingErrorMessage(previewQuery.error.code)
                  : "تعذر تحميل ملخص الطلب."}
              </p>
              <div className="flex flex-wrap gap-3">
                <button
                  type="button"
                  onClick={() => void previewQuery.refetch()}
                  className="rounded-md border border-border px-4 py-2 text-sm"
                >
                  إعادة المحاولة
                </button>
                <Link href="/cart" className="text-sm text-primary hover:underline">
                  العودة إلى السلة
                </Link>
              </div>
            </div>
          ) : preview ? (
            <div className="mt-4 space-y-4">
              <ul className="space-y-3">
                {preview.items.map((line) => {
                  const cartLine = cartByVariant.get(line.variantId);
                  const priceChanged =
                    cartLine &&
                    hasPriceChanged(cartLine.lastKnownUnitPrice, line.unitPrice);
                  return (
                    <li
                      key={line.variantId}
                      className="flex gap-3 border-b border-border/70 pb-3 last:border-0"
                    >
                      <div className="relative h-16 w-16 shrink-0 overflow-hidden rounded-md bg-muted">
                        {line.primaryImageUrl ? (
                          <Image
                            src={line.primaryImageUrl}
                            alt={line.productName}
                            fill
                            sizes="64px"
                            className="object-cover"
                          />
                        ) : null}
                      </div>
                      <div className="min-w-0 flex-1 space-y-1">
                        <p className="text-sm font-medium text-foreground">
                          {line.productName}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {line.variantName} · {line.quantity}{" "}
                          {formatSellingUnit(line.sellingUnit)}
                        </p>
                        <p className="text-sm tabular-nums text-foreground">
                          {formatPrice(line.lineTotal)}
                        </p>
                        {priceChanged ? (
                          <p className="text-xs text-accent-foreground">
                            تم تحديث سعر هذا المنتج وفق السعر الحالي.
                          </p>
                        ) : null}
                      </div>
                    </li>
                  );
                })}
              </ul>

              <dl className="space-y-2 text-sm">
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">مجموع البضاعة</dt>
                  <dd className="tabular-nums font-medium">
                    {formatPrice(preview.merchandiseSubtotal)}
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">
                    الشحن ({preview.deliveryZoneName})
                  </dt>
                  <dd className="tabular-nums font-medium">
                    {formatPrice(preview.shippingFee)}
                  </dd>
                </div>
                <div className="flex justify-between gap-3 border-t border-border pt-2 text-base">
                  <dt className="font-medium">الإجمالي</dt>
                  <dd className="tabular-nums font-semibold">
                    {formatPrice(preview.total)}
                  </dd>
                </div>
              </dl>

              {!preview.meetsMinimumOrder ? (
                <p
                  className="rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground"
                  role="status"
                >
                  الحد الأدنى لمجموع البضاعة هو{" "}
                  <span className="font-medium text-foreground">
                    {formatPrice(preview.appliedMinimumOrderAmount)}
                  </span>
                  . أضف منتجات أخرى لإتمام الطلب.
                </p>
              ) : null}
            </div>
          ) : null}

          {businessError ? (
            <p className="mt-4 text-sm text-destructive" role="alert">
              {businessError}
            </p>
          ) : null}

          <button
            type="submit"
            disabled={!canSubmit}
            className="mt-6 flex h-12 w-full items-center justify-center rounded-md bg-primary text-sm font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:bg-muted disabled:text-muted-foreground"
          >
            {placeMutation.isPending ? "جاري إرسال الطلب…" : "تأكيد الطلب"}
          </button>

          <p className="mt-3 text-xs leading-5 text-muted-foreground">
            الأسعار والتوفر يُعاد التحقق منهما على الخادم عند التأكيد. الدفع عند
            الاستلام فقط.
          </p>

          <Link
            href="/cart"
            className="mt-4 inline-flex text-sm text-primary hover:underline"
          >
            العودة إلى السلة
          </Link>
        </section>
      </aside>
    </form>
  );
}

const inputClass =
  "w-full rounded-md border border-border bg-background px-3 py-2.5 text-sm text-foreground outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring";

function Field({
  id,
  label,
  error,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="text-sm font-medium text-foreground">
        {label}
      </label>
      {children}
      {error ? (
        <p className="text-xs text-destructive" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
