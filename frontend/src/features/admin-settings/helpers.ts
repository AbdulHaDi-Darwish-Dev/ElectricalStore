import type {
  OrderingSettingsDto,
  UpdateOrderingSettingsRequest,
} from "./types";
import type { OrderingSettingsFormValues } from "./schema";

export function toUpdateOrderingSettingsRequest(
  values: OrderingSettingsFormValues,
): UpdateOrderingSettingsRequest {
  return {
    minimumMerchandiseSubtotal: values.minimumMerchandiseSubtotal,
  };
}

export function toOrderingFormValues(
  dto: OrderingSettingsDto,
): OrderingSettingsFormValues {
  return {
    minimumMerchandiseSubtotal: dto.minimumMerchandiseSubtotal,
  };
}

/** Zero means no minimum merchandise requirement. */
export function isNoMinimumMerchandise(amount: number): boolean {
  return Number.isFinite(amount) && amount === 0;
}
