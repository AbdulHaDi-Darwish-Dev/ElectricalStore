import { apiFetch } from "@/lib/api";
import type { PublicDeliveryZoneDto } from "./types";

const SHIPPING_REVALIDATE_SECONDS = 60;

/**
 * GET /shipping/zones — active delivery zones for checkout.
 */
export function getDeliveryZones(): Promise<PublicDeliveryZoneDto[]> {
  return apiFetch<PublicDeliveryZoneDto[]>("/shipping/zones", {
    next: { revalidate: SHIPPING_REVALIDATE_SECONDS },
  });
}
