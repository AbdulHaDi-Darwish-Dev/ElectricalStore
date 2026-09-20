import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  adminInventoryKeys,
  adjustAdminInventory,
  listAdminInventory,
  getAdminInventoryByVariant,
  getInventoryErrorMessage,
  formatStockQuantity,
  availabilityLabel,
  previewOnHandAfterDelta,
  wouldViolateReserved,
  toAdjustInventoryRequest,
  adjustInventoryFormSchema,
} from "@/features/admin-inventory";
import {
  AppPermission,
  canAccessInventory,
} from "@/features/admin";
import { hasPermission } from "@/lib/auth";

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    authenticatedFetch: vi.fn(),
  };
});

import { authenticatedFetch } from "@/lib/auth";

const mockedFetch = vi.mocked(authenticatedFetch);

const sampleItem = {
  productId: "p1",
  productName: "كابل",
  variantId: "v1",
  variantName: "قياسي",
  sku: "SKU-1",
  sellingUnit: "Meter" as const,
  onHand: 12.5,
  reserved: 2,
  available: 10.5,
  isInStock: true,
};

describe("admin inventory permissions", () => {
  it("splits Inventory.Read and Inventory.Adjust", () => {
    expect(AppPermission.inventory.read).toBe("Inventory.Read");
    expect(AppPermission.inventory.adjust).toBe("Inventory.Adjust");
    expect(canAccessInventory(["Inventory.Read"])).toBe(true);
    expect(canAccessInventory(["Inventory.Adjust"])).toBe(true);
    expect(canAccessInventory(["Products.Manage"])).toBe(false);
    expect(hasPermission(["Inventory.Read"], AppPermission.inventory.adjust)).toBe(
      false,
    );
    expect(hasPermission(["Inventory.Adjust"], AppPermission.inventory.read)).toBe(
      false,
    );
  });
});

describe("admin inventory query keys", () => {
  it("uses stable list and detail keys with filters", () => {
    expect(adminInventoryKeys.all()).toEqual(["admin", "inventory"]);
    expect(adminInventoryKeys.list()).toEqual([
      "admin",
      "inventory",
      "list",
      null,
      null,
      null,
      null,
    ]);
    expect(
      adminInventoryKeys.list({
        productId: "p1",
        search: "كابل",
        inStock: true,
      }),
    ).toEqual(["admin", "inventory", "list", "p1", null, "كابل", true]);
    expect(adminInventoryKeys.detail("v1")).toEqual([
      "admin",
      "inventory",
      "detail",
      "v1",
    ]);
  });
});

describe("admin inventory API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("lists via GET /admin/inventory with supported filters", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    await listAdminInventory({
      productId: "p1",
      categoryId: "c1",
      search: "SKU",
      inStock: false,
    });
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/inventory?productId=p1&categoryId=c1&search=SKU&inStock=false",
      {
        method: "GET",
        signal: undefined,
        cache: "no-store",
      },
    );
  });

  it("gets detail via GET /admin/inventory/{variantId}", async () => {
    mockedFetch.mockResolvedValueOnce(sampleItem);
    await getAdminInventoryByVariant("v1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/inventory/v1", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("adjusts via POST delta body without Reserved/Available fields", async () => {
    mockedFetch.mockResolvedValueOnce(sampleItem);
    const body = toAdjustInventoryRequest(-1.5, "تلف");
    expect(body).toEqual({ quantityDelta: -1.5, reason: "تلف" });
    expect(body).not.toHaveProperty("onHand");
    expect(body).not.toHaveProperty("reserved");
    expect(body).not.toHaveProperty("available");

    await adjustAdminInventory("v1", body);
    expect(mockedFetch).toHaveBeenCalledWith("/admin/inventory/v1/adjust", {
      method: "POST",
      body,
    });
  });
});

describe("admin inventory quantity presentation", () => {
  it("formats Piece/Meter decimals without floating noise", () => {
    expect(formatStockQuantity(10)).toBe(
      new Intl.NumberFormat("ar-SY", { maximumFractionDigits: 3 }).format(10),
    );
    expect(formatStockQuantity(12.5)).toBe(
      new Intl.NumberFormat("ar-SY", { maximumFractionDigits: 3 }).format(12.5),
    );
    const noisy = formatStockQuantity(0.1 + 0.2);
    expect(noisy).not.toMatch(/000000/);
    expect(noisy).toBe(
      new Intl.NumberFormat("ar-SY", { maximumFractionDigits: 3 }).format(0.3),
    );
    expect(availabilityLabel(true)).toBe("متوفر");
    expect(availabilityLabel(false)).toBe("غير متوفر");
  });

  it("previews delta semantics for OnHand only", () => {
    expect(previewOnHandAfterDelta(10, 5)).toBe(15);
    expect(previewOnHandAfterDelta(10, -3)).toBe(7);
    expect(wouldViolateReserved(10, 8, -3)).toBe(true);
    expect(wouldViolateReserved(10, 8, -2)).toBe(false);
  });
});

describe("admin inventory adjustment schema", () => {
  it("rejects zero delta and empty reason", () => {
    expect(
      adjustInventoryFormSchema.safeParse({ quantityDelta: 0, reason: "x" })
        .success,
    ).toBe(false);
    expect(
      adjustInventoryFormSchema.safeParse({ quantityDelta: 5, reason: "   " })
        .success,
    ).toBe(false);
    expect(
      adjustInventoryFormSchema.safeParse({
        quantityDelta: -2.5,
        reason: "جرد",
      }).success,
    ).toBe(true);
  });
});

describe("admin inventory error mapping", () => {
  it("maps inventory codes and concurrency", () => {
    expect(getInventoryErrorMessage("Inventory.VariantNotFound")).toContain(
      "غير موجود",
    );
    expect(getInventoryErrorMessage("Inventory.ZeroAdjustment")).toContain(
      "صفراً",
    );
    expect(getInventoryErrorMessage("Inventory.OnHandBelowReserved")).toContain(
      "المحجوزة",
    );
    expect(getInventoryErrorMessage("Inventory.OnHandWouldBeNegative")).toContain(
      "سالباً",
    );
    expect(
      getInventoryErrorMessage("Inventory.ConcurrencyConflict"),
    ).toContain("عملية أخرى");
    expect(getInventoryErrorMessage(undefined, 403)).toContain("صلاحية");
    expect(getInventoryErrorMessage(undefined, 401)).toContain("الجلسة");
  });
});

describe("admin inventory invalidation key root", () => {
  it("exposes all() prefix for post-adjust invalidation", () => {
    expect(adminInventoryKeys.all()).toEqual(["admin", "inventory"]);
    expect(adminInventoryKeys.list({ search: "a" })[0]).toBe("admin");
    expect(adminInventoryKeys.list({ search: "a" })[1]).toBe("inventory");
  });
});
