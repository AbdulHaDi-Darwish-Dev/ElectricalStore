import { authenticatedFetch } from "@/lib/auth";
import type {
  OrderingSettingsDto,
  UpdateOrderingSettingsRequest,
} from "./types";

const ORDERING_BASE = "/admin/settings/ordering";

export async function getOrderingSettings(
  signal?: AbortSignal,
): Promise<OrderingSettingsDto> {
  return authenticatedFetch<OrderingSettingsDto>(ORDERING_BASE, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function updateOrderingSettings(
  body: UpdateOrderingSettingsRequest,
): Promise<OrderingSettingsDto> {
  return authenticatedFetch<OrderingSettingsDto>(ORDERING_BASE, {
    method: "PUT",
    body,
  });
}
