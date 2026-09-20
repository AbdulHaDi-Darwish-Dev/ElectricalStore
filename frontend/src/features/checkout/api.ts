import { apiFetch } from "@/lib/api";
import type { CheckoutPreviewDto, CheckoutPreviewRequest } from "./types";

/**
 * POST /checkout/preview — non-persisting pricing preview.
 * Never cached. Does not place an order.
 */
export function previewCheckout(
  request: CheckoutPreviewRequest,
): Promise<CheckoutPreviewDto> {
  return apiFetch<CheckoutPreviewDto>("/checkout/preview", {
    method: "POST",
    body: request,
    cache: "no-store",
  });
}
