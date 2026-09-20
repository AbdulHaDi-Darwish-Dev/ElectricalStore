import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  adminOrderKeys,
  listAdminOrders,
  getAdminOrder,
  confirmAdminOrder,
  prepareAdminOrder,
  outForDeliveryAdminOrder,
  deliverAdminOrder,
  markPaidAdminOrder,
  cancelAdminOrder,
  getAdminOrderErrorMessage,
  getAvailableAdminOrderActions,
  actionAffectsInventory,
  cancelReleasesReservation,
  formatOrderStatus,
  formatPaymentStatus,
  formatOrderQuantity,
  cancelOrderFormSchema,
} from "@/features/admin-orders";
import { AppPermission, canAccessOrders } from "@/features/admin";
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

describe("admin orders permissions", () => {
  it("splits Orders.Read and Orders.Manage", () => {
    expect(AppPermission.orders.read).toBe("Orders.Read");
    expect(AppPermission.orders.manage).toBe("Orders.Manage");
    expect(canAccessOrders(["Orders.Read"])).toBe(true);
    expect(canAccessOrders(["Orders.Manage"])).toBe(true);
    expect(canAccessOrders(["Shipping.Manage"])).toBe(false);
    expect(hasPermission(["Orders.Read"], AppPermission.orders.manage)).toBe(
      false,
    );
    expect(hasPermission(["Orders.Manage"], AppPermission.orders.read)).toBe(
      false,
    );
  });
});

describe("admin orders query keys", () => {
  it("uses stable list/detail keys with filters", () => {
    expect(adminOrderKeys.all()).toEqual(["admin", "orders"]);
    expect(adminOrderKeys.list()).toEqual([
      "admin",
      "orders",
      "list",
      null,
      null,
      null,
      null,
      null,
    ]);
    expect(
      adminOrderKeys.list({
        status: "PendingConfirmation",
        paymentStatus: "Unpaid",
        search: "ORD",
      }),
    ).toEqual([
      "admin",
      "orders",
      "list",
      "PendingConfirmation",
      "Unpaid",
      "ORD",
      null,
      null,
    ]);
    expect(adminOrderKeys.detail("o1")).toEqual([
      "admin",
      "orders",
      "detail",
      "o1",
    ]);
  });
});

describe("admin orders API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("lists with status/paymentStatus/search query params", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    await listAdminOrders({
      status: "Confirmed",
      paymentStatus: "Paid",
      search: "أحمد",
    });
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/orders?status=Confirmed&paymentStatus=Paid&search=%D8%A3%D8%AD%D9%85%D8%AF",
      { method: "GET", signal: undefined, cache: "no-store" },
    );
  });

  it("gets detail via GET /admin/orders/{id}", async () => {
    mockedFetch.mockResolvedValueOnce({ id: "o1" });
    await getAdminOrder("o1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/orders/o1", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("posts each lifecycle transition endpoint", async () => {
    mockedFetch.mockResolvedValue({ id: "o1" });
    await confirmAdminOrder("o1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/orders/o1/confirm", {
      method: "POST",
    });
    await prepareAdminOrder("o1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/orders/o1/prepare", {
      method: "POST",
    });
    await outForDeliveryAdminOrder("o1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/orders/o1/out-for-delivery",
      { method: "POST" },
    );
    await deliverAdminOrder("o1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/orders/o1/deliver", {
      method: "POST",
    });
    await markPaidAdminOrder("o1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/orders/o1/mark-paid", {
      method: "POST",
    });
  });

  it("cancels with required reason body", async () => {
    mockedFetch.mockResolvedValueOnce({ id: "o1" });
    await cancelAdminOrder("o1", { reason: "طلب العميل" });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/orders/o1/cancel", {
      method: "POST",
      body: { reason: "طلب العميل" },
    });
  });
});

describe("admin orders state machine helpers", () => {
  it("exposes only backend-allowed actions", () => {
    expect(
      getAvailableAdminOrderActions("PendingConfirmation", "Unpaid"),
    ).toEqual(["confirm", "cancel"]);
    expect(getAvailableAdminOrderActions("Confirmed", "Unpaid")).toEqual([
      "prepare",
      "cancel",
    ]);
    expect(getAvailableAdminOrderActions("Preparing", "Unpaid")).toEqual([
      "outForDelivery",
      "cancel",
    ]);
    expect(getAvailableAdminOrderActions("OutForDelivery", "Unpaid")).toEqual([
      "deliver",
      "markPaid",
    ]);
    expect(getAvailableAdminOrderActions("OutForDelivery", "Paid")).toEqual([
      "deliver",
    ]);
    expect(getAvailableAdminOrderActions("Delivered", "Unpaid")).toEqual([
      "markPaid",
    ]);
    expect(getAvailableAdminOrderActions("Delivered", "Paid")).toEqual([]);
    expect(getAvailableAdminOrderActions("Cancelled", "Unpaid")).toEqual([]);
  });

  it("flags inventory-affecting actions and cancel release", () => {
    expect(actionAffectsInventory("confirm")).toBe(true);
    expect(actionAffectsInventory("outForDelivery")).toBe(true);
    expect(actionAffectsInventory("cancel")).toBe(true);
    expect(actionAffectsInventory("prepare")).toBe(false);
    expect(actionAffectsInventory("deliver")).toBe(false);
    expect(actionAffectsInventory("markPaid")).toBe(false);
    expect(cancelReleasesReservation("Confirmed")).toBe(true);
    expect(cancelReleasesReservation("PendingConfirmation")).toBe(false);
  });
});

describe("admin orders presentation", () => {
  it("maps Arabic labels and formats quantities", () => {
    expect(formatOrderStatus("PendingConfirmation")).toContain("تأكيد");
    expect(formatPaymentStatus("Unpaid")).toBe("غير مدفوع");
    expect(formatOrderQuantity(12.5)).toBe(
      new Intl.NumberFormat("ar-SY", { maximumFractionDigits: 3 }).format(12.5),
    );
  });
});

describe("admin orders cancel schema", () => {
  it("requires non-empty reason", () => {
    expect(cancelOrderFormSchema.safeParse({ reason: "  " }).success).toBe(
      false,
    );
    expect(
      cancelOrderFormSchema.safeParse({ reason: "نقص مخزون" }).success,
    ).toBe(true);
  });
});

describe("admin orders error mapping", () => {
  it("maps ordering and concurrency codes", () => {
    expect(
      getAdminOrderErrorMessage("Ordering.ConfirmationStockConflict"),
    ).toContain("المخزون");
    expect(getAdminOrderErrorMessage("Ordering.ConcurrencyConflict")).toContain(
      "عملية أخرى",
    );
    expect(getAdminOrderErrorMessage("Ordering.InvalidTransition")).toContain(
      "غير مسموحة",
    );
    expect(getAdminOrderErrorMessage("Ordering.AlreadyPaid")).toContain(
      "مدفوع",
    );
    expect(getAdminOrderErrorMessage(undefined, 403)).toContain("صلاحية");
  });
});

describe("admin orders invalidation root", () => {
  it("exposes all() for list/detail invalidation", () => {
    expect(adminOrderKeys.all()).toEqual(["admin", "orders"]);
  });
});
