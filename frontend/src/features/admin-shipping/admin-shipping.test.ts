import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  adminShippingKeys,
  listAdminShippingZones,
  getAdminShippingZone,
  createAdminShippingZone,
  updateAdminShippingZone,
  activateAdminShippingZone,
  deactivateAdminShippingZone,
  getShippingErrorMessage,
  toCreateShippingZoneRequest,
  toUpdateShippingZoneRequest,
  shippingZoneFormSchema,
  isFreeShippingFee,
  ZONE_NAME_MAX_LENGTH,
} from "@/features/admin-shipping";
import { AppPermission, canAccessShipping } from "@/features/admin";
import { formatPrice } from "@/lib/format";

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    authenticatedFetch: vi.fn(),
  };
});

import { authenticatedFetch } from "@/lib/auth";

const mockedFetch = vi.mocked(authenticatedFetch);

const sampleZone = {
  id: "z1",
  name: "حلب المركز",
  fee: 5000,
  isActive: true,
};

describe("admin shipping permissions", () => {
  it("requires Shipping.Manage only", () => {
    expect(AppPermission.shipping.manage).toBe("Shipping.Manage");
    expect(canAccessShipping(["Shipping.Manage"])).toBe(true);
    expect(canAccessShipping(["Inventory.Read"])).toBe(false);
    expect(canAccessShipping(["Admin"])).toBe(false);
  });
});

describe("admin shipping query keys", () => {
  it("uses stable zone list and detail keys", () => {
    expect(adminShippingKeys.all()).toEqual(["admin", "shipping"]);
    expect(adminShippingKeys.zones()).toEqual(["admin", "shipping", "zones"]);
    expect(adminShippingKeys.list()).toEqual([
      "admin",
      "shipping",
      "zones",
      "list",
    ]);
    expect(adminShippingKeys.detail("z1")).toEqual([
      "admin",
      "shipping",
      "zones",
      "detail",
      "z1",
    ]);
  });
});

describe("admin shipping API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("lists via GET /admin/shipping/zones", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    await listAdminShippingZones();
    expect(mockedFetch).toHaveBeenCalledWith("/admin/shipping/zones", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("gets via GET /admin/shipping/zones/{id}", async () => {
    mockedFetch.mockResolvedValueOnce(sampleZone);
    await getAdminShippingZone("z1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/shipping/zones/z1", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("creates with name, fee, isActive", async () => {
    mockedFetch.mockResolvedValueOnce(sampleZone);
    const body = toCreateShippingZoneRequest({
      name: "  حلب المركز  ",
      fee: 5000,
      isActive: true,
    });
    expect(body).toEqual({
      name: "حلب المركز",
      fee: 5000,
      isActive: true,
    });
    await createAdminShippingZone(body);
    expect(mockedFetch).toHaveBeenCalledWith("/admin/shipping/zones", {
      method: "POST",
      body,
    });
  });

  it("updates name and fee only (no isActive / no delete)", async () => {
    mockedFetch.mockResolvedValueOnce(sampleZone);
    const body = toUpdateShippingZoneRequest({
      name: "حلب",
      fee: 0,
      isActive: false,
    });
    expect(body).toEqual({ name: "حلب", fee: 0 });
    expect(body).not.toHaveProperty("isActive");
    await updateAdminShippingZone("z1", body);
    expect(mockedFetch).toHaveBeenCalledWith("/admin/shipping/zones/z1", {
      method: "PUT",
      body,
    });
  });

  it("activates and deactivates via lifecycle POSTs", async () => {
    mockedFetch.mockResolvedValueOnce(sampleZone);
    await activateAdminShippingZone("z1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/shipping/zones/z1/activate",
      { method: "POST" },
    );

    mockedFetch.mockResolvedValueOnce({ ...sampleZone, isActive: false });
    await deactivateAdminShippingZone("z1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/shipping/zones/z1/deactivate",
      { method: "POST" },
    );
  });
});

describe("admin shipping fee / form", () => {
  it("allows zero fee and rejects negative", () => {
    expect(isFreeShippingFee(0)).toBe(true);
    expect(isFreeShippingFee(100)).toBe(false);
    expect(formatPrice(0)).toBeTruthy();
    expect(
      shippingZoneFormSchema.safeParse({
        name: "A",
        fee: 0,
        isActive: true,
      }).success,
    ).toBe(true);
    expect(
      shippingZoneFormSchema.safeParse({
        name: "A",
        fee: -1,
        isActive: true,
      }).success,
    ).toBe(false);
    expect(
      shippingZoneFormSchema.safeParse({
        name: "x".repeat(ZONE_NAME_MAX_LENGTH + 1),
        fee: 1,
        isActive: true,
      }).success,
    ).toBe(false);
  });
});

describe("admin shipping error mapping", () => {
  it("maps Shipping.* codes", () => {
    expect(getShippingErrorMessage("Shipping.NotFound")).toContain("غير موجودة");
    expect(getShippingErrorMessage("Shipping.NameRequired")).toContain("مطلوب");
    expect(getShippingErrorMessage("Shipping.NameAlreadyExists")).toContain(
      "نفس الاسم",
    );
    expect(getShippingErrorMessage("Shipping.NegativeFee")).toContain("سالبة");
    expect(getShippingErrorMessage(undefined, 403)).toContain("صلاحية");
    expect(getShippingErrorMessage(undefined, 401)).toContain("الجلسة");
  });
});

describe("admin shipping invalidation root", () => {
  it("exposes all() prefix for mutations", () => {
    expect(adminShippingKeys.all()).toEqual(["admin", "shipping"]);
    expect(adminShippingKeys.list()[0]).toBe("admin");
    expect(adminShippingKeys.list()[1]).toBe("shipping");
  });
});
