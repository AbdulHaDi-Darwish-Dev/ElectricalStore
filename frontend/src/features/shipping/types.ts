/**
 * Public shipping DTOs — GET /shipping/zones.
 * Inactive zones are omitted by the API; no isActive on the public shape.
 */

export type PublicDeliveryZoneDto = {
  id: string;
  name: string;
  fee: number;
};
