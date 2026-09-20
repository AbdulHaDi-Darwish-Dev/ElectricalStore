import type {
  CreateAdminDeliveryZoneRequest,
  UpdateAdminDeliveryZoneRequest,
} from "./types";
import type { ShippingZoneFormValues } from "./schema";

export function toCreateShippingZoneRequest(
  values: ShippingZoneFormValues,
): CreateAdminDeliveryZoneRequest {
  return {
    name: values.name.trim(),
    fee: values.fee,
    isActive: values.isActive,
  };
}

export function toUpdateShippingZoneRequest(
  values: ShippingZoneFormValues,
): UpdateAdminDeliveryZoneRequest {
  return {
    name: values.name.trim(),
    fee: values.fee,
  };
}

/** Zero fee is valid (free shipping for that zone). */
export function isFreeShippingFee(fee: number): boolean {
  return Number.isFinite(fee) && fee === 0;
}
