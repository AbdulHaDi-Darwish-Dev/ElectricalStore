import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  adminSettingsKeys,
  getOrderingSettings,
  updateOrderingSettings,
  getSettingsErrorMessage,
  toUpdateOrderingSettingsRequest,
  toOrderingFormValues,
  isNoMinimumMerchandise,
  orderingSettingsFormSchema,
} from "@/features/admin-settings";
import { AppPermission, canAccessSettings } from "@/features/admin";

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    authenticatedFetch: vi.fn(),
  };
});

import { authenticatedFetch } from "@/lib/auth";

const mockedFetch = vi.mocked(authenticatedFetch);

describe("admin settings permissions", () => {
  it("requires Settings.Manage", () => {
    expect(AppPermission.settings.manage).toBe("Settings.Manage");
    expect(canAccessSettings(["Settings.Manage"])).toBe(true);
    expect(canAccessSettings(["Orders.Manage"])).toBe(false);
    expect(canAccessSettings(["Admin"])).toBe(false);
  });
});

describe("admin settings query keys", () => {
  it("uses stable ordering settings key", () => {
    expect(adminSettingsKeys.all()).toEqual(["admin", "settings"]);
    expect(adminSettingsKeys.ordering()).toEqual([
      "admin",
      "settings",
      "ordering",
    ]);
  });
});

describe("admin settings API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("gets via GET /admin/settings/ordering", async () => {
    mockedFetch.mockResolvedValueOnce({ minimumMerchandiseSubtotal: 0 });
    await getOrderingSettings();
    expect(mockedFetch).toHaveBeenCalledWith("/admin/settings/ordering", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("updates via PUT with exact DTO", async () => {
    mockedFetch.mockResolvedValueOnce({ minimumMerchandiseSubtotal: 10000 });
    const body = toUpdateOrderingSettingsRequest({
      minimumMerchandiseSubtotal: 10000,
    });
    expect(body).toEqual({ minimumMerchandiseSubtotal: 10000 });
    await updateOrderingSettings(body);
    expect(mockedFetch).toHaveBeenCalledWith("/admin/settings/ordering", {
      method: "PUT",
      body,
    });
  });
});

describe("admin settings validation / zero semantics", () => {
  it("allows zero and rejects negative", () => {
    expect(isNoMinimumMerchandise(0)).toBe(true);
    expect(isNoMinimumMerchandise(100)).toBe(false);
    expect(
      orderingSettingsFormSchema.safeParse({
        minimumMerchandiseSubtotal: 0,
      }).success,
    ).toBe(true);
    expect(
      orderingSettingsFormSchema.safeParse({
        minimumMerchandiseSubtotal: -1,
      }).success,
    ).toBe(false);
    expect(
      toOrderingFormValues({ minimumMerchandiseSubtotal: 2500 }),
    ).toEqual({ minimumMerchandiseSubtotal: 2500 });
  });
});

describe("admin settings error mapping", () => {
  it("maps InvalidMinimumOrderAmount and auth statuses", () => {
    expect(
      getSettingsErrorMessage("Ordering.InvalidMinimumOrderAmount"),
    ).toContain("سالباً");
    expect(getSettingsErrorMessage(undefined, 403)).toContain("صلاحية");
    expect(getSettingsErrorMessage(undefined, 401)).toContain("الجلسة");
  });
});

describe("admin settings invalidation root", () => {
  it("exposes all() for post-save invalidation", () => {
    expect(adminSettingsKeys.all()).toEqual(["admin", "settings"]);
  });
});
