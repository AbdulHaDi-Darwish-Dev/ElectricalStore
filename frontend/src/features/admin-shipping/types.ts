/** Matches DeliveryZone.NameMaxLength. */
export const ZONE_NAME_MAX_LENGTH = 200;

/** Matches DeliveryZone.FeeScale (SYP decimal). */
export const ZONE_FEE_SCALE = 2;

/** Admin DTO — DeliveryZoneDto. */
export type AdminDeliveryZoneDto = {
  id: string;
  name: string;
  fee: number;
  isActive: boolean;
};

/** POST /admin/shipping/zones */
export type CreateAdminDeliveryZoneRequest = {
  name: string;
  fee: number;
  isActive: boolean;
};

/** PUT /admin/shipping/zones/{id} — name + fee only (lifecycle separate). */
export type UpdateAdminDeliveryZoneRequest = {
  name: string;
  fee: number;
};
