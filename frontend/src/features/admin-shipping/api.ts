import { authenticatedFetch } from "@/lib/auth";
import type {
  AdminDeliveryZoneDto,
  CreateAdminDeliveryZoneRequest,
  UpdateAdminDeliveryZoneRequest,
} from "./types";

const BASE = "/admin/shipping/zones";

export async function listAdminShippingZones(
  signal?: AbortSignal,
): Promise<AdminDeliveryZoneDto[]> {
  return authenticatedFetch<AdminDeliveryZoneDto[]>(BASE, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function getAdminShippingZone(
  id: string,
  signal?: AbortSignal,
): Promise<AdminDeliveryZoneDto> {
  return authenticatedFetch<AdminDeliveryZoneDto>(`${BASE}/${id}`, {
    method: "GET",
    signal,
    cache: "no-store",
  });
}

export async function createAdminShippingZone(
  body: CreateAdminDeliveryZoneRequest,
): Promise<AdminDeliveryZoneDto> {
  return authenticatedFetch<AdminDeliveryZoneDto>(BASE, {
    method: "POST",
    body,
  });
}

export async function updateAdminShippingZone(
  id: string,
  body: UpdateAdminDeliveryZoneRequest,
): Promise<AdminDeliveryZoneDto> {
  return authenticatedFetch<AdminDeliveryZoneDto>(`${BASE}/${id}`, {
    method: "PUT",
    body,
  });
}

export async function activateAdminShippingZone(
  id: string,
): Promise<AdminDeliveryZoneDto> {
  return authenticatedFetch<AdminDeliveryZoneDto>(`${BASE}/${id}/activate`, {
    method: "POST",
  });
}

export async function deactivateAdminShippingZone(
  id: string,
): Promise<AdminDeliveryZoneDto> {
  return authenticatedFetch<AdminDeliveryZoneDto>(`${BASE}/${id}/deactivate`, {
    method: "POST",
  });
}
