import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  adminCategoryKeys,
  createAdminCategory,
  getCategoryErrorMessage,
  listAdminCategories,
  normalizeOptionalDescription,
  toCreateCategoryRequest,
  toUpdateCategoryRequest,
  updateAdminCategory,
  upsertAdminCategoryImage,
  validateCategoryImageFile,
  activateAdminCategory,
  deactivateAdminCategory,
  deleteAdminCategoryImage,
  CATEGORY_IMAGE_MAX_SIZE_BYTES,
} from "@/features/admin-categories";
import { AppPermission, canAccessCategories } from "@/features/admin";

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    authenticatedFetch: vi.fn(),
  };
});

import { authenticatedFetch } from "@/lib/auth";

const mockedFetch = vi.mocked(authenticatedFetch);

describe("admin category query keys", () => {
  it("uses stable list and detail keys under admin root", () => {
    expect(adminCategoryKeys.list()).toEqual(["admin", "categories", "list"]);
    expect(adminCategoryKeys.detail("abc")).toEqual([
      "admin",
      "categories",
      "detail",
      "abc",
    ]);
    expect(adminCategoryKeys.all()).toEqual(["admin", "categories"]);
  });
});

describe("admin category permissions", () => {
  it("requires Categories.Manage only (no role names)", () => {
    expect(AppPermission.categories.manage).toBe("Categories.Manage");
    expect(canAccessCategories(["Categories.Manage"])).toBe(true);
    expect(canAccessCategories(["Admin"])).toBe(false);
    expect(canAccessCategories(["Owner"])).toBe(false);
    expect(canAccessCategories([])).toBe(false);
  });
});

describe("admin category API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("lists via GET /admin/categories with authenticatedFetch", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    await listAdminCategories();
    expect(mockedFetch).toHaveBeenCalledWith("/admin/categories", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("creates via POST JSON body", async () => {
    mockedFetch.mockResolvedValueOnce({
      id: "1",
      name: "كابلات",
      description: null,
      isActive: true,
      imageUrl: null,
      hasImage: false,
    });
    await createAdminCategory({ name: "كابلات", isActive: true });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/categories", {
      method: "POST",
      body: { name: "كابلات", isActive: true },
    });
  });

  it("updates via PUT JSON body (no isActive)", async () => {
    mockedFetch.mockResolvedValueOnce({
      id: "1",
      name: "محدث",
      description: "وصف",
      isActive: true,
      imageUrl: null,
      hasImage: false,
    });
    await updateAdminCategory("1", { name: "محدث", description: "وصف" });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/categories/1", {
      method: "PUT",
      body: { name: "محدث", description: "وصف" },
    });
  });

  it("activates and deactivates via POST (no hard delete)", async () => {
    mockedFetch.mockResolvedValue({
      id: "1",
      name: "x",
      description: null,
      isActive: false,
      imageUrl: null,
      hasImage: false,
    });
    await deactivateAdminCategory("1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/categories/1/deactivate", {
      method: "POST",
    });
    await activateAdminCategory("1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/categories/1/activate", {
      method: "POST",
    });
  });

  it("uploads image as FormData with file field and no manual Content-Type", async () => {
    mockedFetch.mockResolvedValueOnce({
      id: "1",
      name: "x",
      description: null,
      isActive: true,
      imageUrl: "https://example.com/a.jpg",
      hasImage: true,
    });
    const file = new File([new Uint8Array([0xff, 0xd8, 0xff])], "cat.jpg", {
      type: "image/jpeg",
    });
    await upsertAdminCategoryImage("1", file);
    const call = mockedFetch.mock.calls[0];
    expect(call?.[0]).toBe("/admin/categories/1/image");
    expect(call?.[1]?.method).toBe("PUT");
    expect(call?.[1]?.body).toBeInstanceOf(FormData);
    const form = call?.[1]?.body as FormData;
    expect(form.get("file")).toBeInstanceOf(File);
  });

  it("deletes image via DELETE /admin/categories/{id}/image", async () => {
    mockedFetch.mockResolvedValueOnce({
      id: "1",
      name: "x",
      description: null,
      isActive: true,
      imageUrl: null,
      hasImage: false,
    });
    await deleteAdminCategoryImage("1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/categories/1/image", {
      method: "DELETE",
    });
  });
});

describe("category form helpers", () => {
  it("normalizes blank description to null", () => {
    expect(normalizeOptionalDescription("  ")).toBeNull();
    expect(normalizeOptionalDescription("وصف")).toBe("وصف");
  });

  it("shapes create and update requests", () => {
    expect(
      toCreateCategoryRequest({
        name: "  كابلات  ",
        description: "  ",
        isActive: false,
      }),
    ).toEqual({ name: "كابلات", description: null, isActive: false });

    expect(
      toUpdateCategoryRequest({
        name: "منتجات",
        description: "وصف قصير",
      }),
    ).toEqual({ name: "منتجات", description: "وصف قصير" });
  });

  it("validates image type and size against backend MediaOptions", () => {
    const ok = new File([new Uint8Array(10)], "a.jpg", { type: "image/jpeg" });
    expect(validateCategoryImageFile(ok).ok).toBe(true);

    const badType = new File([new Uint8Array(10)], "a.gif", {
      type: "image/gif",
    });
    expect(validateCategoryImageFile(badType).ok).toBe(false);

    const huge = new File(
      [new Uint8Array(CATEGORY_IMAGE_MAX_SIZE_BYTES + 1)],
      "big.jpg",
      { type: "image/jpeg" },
    );
    expect(validateCategoryImageFile(huge).ok).toBe(false);
  });
});

describe("category error mapping", () => {
  it("maps important ProblemDetails codes to Arabic", () => {
    expect(getCategoryErrorMessage("Category.NameAlreadyExists")).toContain(
      "نفس الاسم",
    );
    expect(getCategoryErrorMessage("Category.NameRequired")).toContain("مطلوب");
    expect(getCategoryErrorMessage("Category.NotFound")).toContain("غير موجود");
    expect(getCategoryErrorMessage("Media.FileTooLarge")).toContain("5");
    expect(getCategoryErrorMessage("Media.InvalidContentType")).toContain("JPEG");
    expect(getCategoryErrorMessage(undefined, 403)).toContain("صلاحية");
  });
});

describe("mutation invalidation keys", () => {
  it("invalidating all covers list and detail prefixes", () => {
    const all = adminCategoryKeys.all();
    expect(adminCategoryKeys.list().slice(0, all.length)).toEqual(all);
    expect(adminCategoryKeys.detail("x").slice(0, all.length)).toEqual(all);
  });
});
