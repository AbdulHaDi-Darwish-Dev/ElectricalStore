import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  adminProductKeys,
  createAdminProduct,
  listAdminProducts,
  updateAdminProduct,
  activateAdminProduct,
  deactivateAdminProduct,
  addAdminProductVariant,
  updateAdminProductVariant,
  activateAdminProductVariant,
  deactivateAdminProductVariant,
  addAdminProductImage,
  deleteAdminProductImage,
  setPrimaryAdminProductImage,
  reorderAdminProductImages,
  getProductErrorMessage,
  toCreateProductRequest,
  toUpdateProductRequest,
  toUpdateVariantRequest,
  validateProductImageFile,
  getProductReadiness,
  PRODUCT_MAX_IMAGES,
  PRODUCT_IMAGE_MAX_SIZE_BYTES,
  type AdminProductDto,
} from "@/features/admin-products";
import { AppPermission, canAccessProducts } from "@/features/admin";

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    authenticatedFetch: vi.fn(),
  };
});

import { authenticatedFetch } from "@/lib/auth";

const mockedFetch = vi.mocked(authenticatedFetch);

const sampleProduct: AdminProductDto = {
  id: "p1",
  name: "كابل",
  description: null,
  categoryId: "c1",
  categoryName: "كابلات",
  categoryIsActive: true,
  isActive: true,
  variants: [
    {
      id: "v1",
      name: "قياسي",
      sku: "SKU-1",
      price: 100,
      sellingUnit: "Piece",
      quantityIncrement: 1,
      isActive: true,
    },
  ],
  images: [
    {
      id: "i1",
      url: "https://example.com/a.jpg",
      isPrimary: true,
      sortOrder: 0,
    },
  ],
};

describe("admin product permissions", () => {
  it("requires Products.Manage only", () => {
    expect(AppPermission.products.manage).toBe("Products.Manage");
    expect(canAccessProducts(["Products.Manage"])).toBe(true);
    expect(canAccessProducts(["Admin"])).toBe(false);
    expect(canAccessProducts(["Inventory.Adjust"])).toBe(false);
  });
});

describe("admin product query keys", () => {
  it("uses stable list and detail keys", () => {
    expect(adminProductKeys.all()).toEqual(["admin", "products"]);
    expect(adminProductKeys.list()).toEqual([
      "admin",
      "products",
      "list",
      null,
      null,
      null,
    ]);
    expect(adminProductKeys.detail("abc")).toEqual([
      "admin",
      "products",
      "detail",
      "abc",
    ]);
  });
});

describe("admin product API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
  });

  it("lists via GET /admin/products", async () => {
    mockedFetch.mockResolvedValueOnce([]);
    await listAdminProducts();
    expect(mockedFetch).toHaveBeenCalledWith("/admin/products", {
      method: "GET",
      signal: undefined,
      cache: "no-store",
    });
  });

  it("creates via POST with variants required in body", async () => {
    mockedFetch.mockResolvedValueOnce(sampleProduct);
    await createAdminProduct({
      name: "كابل",
      categoryId: "c1",
      isActive: true,
      variants: [
        {
          name: "قياسي",
          sku: "SKU-1",
          price: 100,
          sellingUnit: "Piece",
          quantityIncrement: 1,
          isActive: true,
        },
      ],
    });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/products", {
      method: "POST",
      body: expect.objectContaining({
        name: "كابل",
        variants: expect.any(Array),
      }),
    });
  });

  it("updates basics via PUT without isActive", async () => {
    mockedFetch.mockResolvedValueOnce(sampleProduct);
    await updateAdminProduct("p1", {
      name: "محدث",
      description: null,
      categoryId: "c1",
    });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/products/p1", {
      method: "PUT",
      body: { name: "محدث", description: null, categoryId: "c1" },
    });
  });

  it("activates and deactivates product via POST", async () => {
    mockedFetch.mockResolvedValue(sampleProduct);
    await deactivateAdminProduct("p1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/products/p1/deactivate", {
      method: "POST",
    });
    await activateAdminProduct("p1");
    expect(mockedFetch).toHaveBeenCalledWith("/admin/products/p1/activate", {
      method: "POST",
    });
  });
});

describe("admin variant API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
    mockedFetch.mockResolvedValue(sampleProduct);
  });

  it("adds variant via POST", async () => {
    await addAdminProductVariant("p1", {
      name: "متر",
      sku: "SKU-M",
      price: 50,
      sellingUnit: "Meter",
      quantityIncrement: 0.5,
      isActive: true,
    });
    expect(mockedFetch).toHaveBeenCalledWith("/admin/products/p1/variants", {
      method: "POST",
      body: expect.objectContaining({ sellingUnit: "Meter" }),
    });
  });

  it("updates variant via PUT without isActive", async () => {
    await updateAdminProductVariant("p1", "v1", {
      name: "قياسي",
      sku: "SKU-1",
      price: 120,
      sellingUnit: "Piece",
      quantityIncrement: 1,
    });
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/products/p1/variants/v1",
      {
        method: "PUT",
        body: expect.not.objectContaining({ isActive: expect.anything() }),
      },
    );
  });

  it("activates and deactivates variants via POST", async () => {
    await activateAdminProductVariant("p1", "v1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/products/p1/variants/v1/activate",
      { method: "POST" },
    );
    await deactivateAdminProductVariant("p1", "v1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/products/p1/variants/v1/deactivate",
      { method: "POST" },
    );
  });
});

describe("admin media API contract", () => {
  beforeEach(() => {
    mockedFetch.mockReset();
    mockedFetch.mockResolvedValue(sampleProduct);
  });

  it("uploads image as FormData POST", async () => {
    const file = new File([new Uint8Array([1, 2, 3])], "a.jpg", {
      type: "image/jpeg",
    });
    await addAdminProductImage("p1", file);
    const call = mockedFetch.mock.calls[0];
    expect(call?.[0]).toBe("/admin/products/p1/images");
    expect(call?.[1]?.method).toBe("POST");
    expect(call?.[1]?.body).toBeInstanceOf(FormData);
  });

  it("deletes, sets primary, and reorders", async () => {
    await deleteAdminProductImage("p1", "i1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/products/p1/images/i1",
      { method: "DELETE" },
    );
    await setPrimaryAdminProductImage("p1", "i1");
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/products/p1/images/i1/primary",
      { method: "POST" },
    );
    await reorderAdminProductImages("p1", ["i1", "i2"]);
    expect(mockedFetch).toHaveBeenCalledWith(
      "/admin/products/p1/images/order",
      {
        method: "PUT",
        body: { orderedImageIds: ["i1", "i2"] },
      },
    );
  });
});

describe("product request shaping", () => {
  it("shapes create and update requests", () => {
    expect(
      toCreateProductRequest({
        name: "  منتج  ",
        description: "  ",
        categoryId: "11111111-1111-1111-1111-111111111111",
        isActive: true,
        variants: [
          {
            name: "قياسي",
            sku: "SKU",
            price: 10,
            sellingUnit: "Piece",
            quantityIncrement: 1,
            isActive: true,
          },
        ],
      }),
    ).toEqual({
      name: "منتج",
      description: null,
      categoryId: "11111111-1111-1111-1111-111111111111",
      isActive: true,
      variants: [
        expect.objectContaining({ name: "قياسي", sellingUnit: "Piece" }),
      ],
    });

    expect(
      toUpdateProductRequest({
        name: "محدث",
        description: "وصف",
        categoryId: "11111111-1111-1111-1111-111111111111",
        isActive: false,
      }),
    ).toEqual({
      name: "محدث",
      description: "وصف",
      categoryId: "11111111-1111-1111-1111-111111111111",
    });

    expect(
      toUpdateVariantRequest({
        name: "خ",
        sku: "S",
        price: 1,
        sellingUnit: "Meter",
        quantityIncrement: 0.5,
      }),
    ).not.toHaveProperty("isActive");
  });
});

describe("product image constraints", () => {
  it("rejects over max count and bad type/size", () => {
    const file = new File([new Uint8Array(10)], "a.jpg", {
      type: "image/jpeg",
    });
    expect(validateProductImageFile(file, PRODUCT_MAX_IMAGES).ok).toBe(false);
    expect(validateProductImageFile(file, 0).ok).toBe(true);

    const gif = new File([new Uint8Array(10)], "a.gif", { type: "image/gif" });
    expect(validateProductImageFile(gif, 0).ok).toBe(false);

    const huge = new File(
      [new Uint8Array(PRODUCT_IMAGE_MAX_SIZE_BYTES + 1)],
      "big.jpg",
      { type: "image/jpeg" },
    );
    expect(validateProductImageFile(huge, 0).ok).toBe(false);
  });
});

describe("product readiness helper", () => {
  it("requires active product, active category, image, and active variant", () => {
    expect(getProductReadiness(sampleProduct).likelyPublic).toBe(true);

    expect(
      getProductReadiness({
        ...sampleProduct,
        isActive: false,
      }).likelyPublic,
    ).toBe(false);

    expect(
      getProductReadiness({
        ...sampleProduct,
        categoryIsActive: false,
      }).likelyPublic,
    ).toBe(false);

    expect(
      getProductReadiness({
        ...sampleProduct,
        images: [],
      }).likelyPublic,
    ).toBe(false);

    expect(
      getProductReadiness({
        ...sampleProduct,
        variants: sampleProduct.variants.map((v) => ({
          ...v,
          isActive: false,
        })),
      }).likelyPublic,
    ).toBe(false);
  });
});

describe("product error mapping", () => {
  it("maps important Product and Media codes", () => {
    expect(getProductErrorMessage("Product.SkuAlreadyExists")).toContain("SKU");
    expect(getProductErrorMessage("Product.VariantsRequired")).toContain("خيار");
    expect(getProductErrorMessage("Product.TooManyImages")).toContain("4");
    expect(getProductErrorMessage("Product.InvalidQuantityIncrement")).toContain(
      "1",
    );
    expect(getProductErrorMessage("Media.FileTooLarge")).toContain("5");
    expect(getProductErrorMessage(undefined, 403)).toContain("صلاحية");
  });
});

describe("mutation invalidation keys", () => {
  it("all key prefixes list and detail", () => {
    const all = adminProductKeys.all();
    expect(adminProductKeys.list().slice(0, all.length)).toEqual(all);
    expect(adminProductKeys.detail("x").slice(0, all.length)).toEqual(all);
  });
});
