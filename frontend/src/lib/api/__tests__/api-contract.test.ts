import { afterEach, describe, expect, it, vi } from "vitest";
import { apiFetch, ApiError } from "@/lib/api";
import { buildCatalogProductsPath } from "@/features/catalog/paths";
import { previewCheckout } from "@/features/checkout/api";

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("buildCatalogProductsPath", () => {
  it("returns bare path when no params", () => {
    expect(buildCatalogProductsPath()).toBe("/catalog/products");
    expect(buildCatalogProductsPath({})).toBe("/catalog/products");
  });

  it("includes categoryId only", () => {
    expect(buildCatalogProductsPath({ categoryId: "cat-1" })).toBe(
      "/catalog/products?categoryId=cat-1",
    );
  });

  it("includes search only", () => {
    expect(buildCatalogProductsPath({ search: "كابل" })).toBe(
      `/catalog/products?search=${encodeURIComponent("كابل")}`,
    );
  });

  it("includes categoryId and search", () => {
    const path = buildCatalogProductsPath({
      categoryId: "cat-1",
      search: "wire",
    });
    expect(path).toBe("/catalog/products?categoryId=cat-1&search=wire");
  });

  it("omits empty search", () => {
    expect(buildCatalogProductsPath({ categoryId: "cat-1", search: "" })).toBe(
      "/catalog/products?categoryId=cat-1",
    );
  });
});

describe("apiFetch", () => {
  it("parses successful JSON", async () => {
    const payload = { id: "1", name: "test" };
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(payload), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      ),
    );

    await expect(apiFetch("/health")).resolves.toEqual(payload);
  });

  it("returns undefined for 204", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(null, { status: 204 })),
    );

    await expect(apiFetch("/empty")).resolves.toBeUndefined();
  });

  it("returns undefined for 205", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(null, { status: 205 })),
    );

    await expect(apiFetch("/empty")).resolves.toBeUndefined();
  });

  it("normalizes ProblemDetails preserving status and code", async () => {
    const problem = {
      status: 404,
      title: "Product.NotFound",
      detail: "Product was not found.",
      code: "Product.NotFound",
      traceId: "00-abc",
    };

    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(problem), {
          status: 404,
          headers: { "Content-Type": "application/problem+json" },
        }),
      ),
    );

    let caught: unknown;
    try {
      await apiFetch("/catalog/products/missing");
    } catch (e) {
      caught = e;
    }

    expect(caught).toBeInstanceOf(ApiError);
    const error = caught as ApiError;
    expect(error.status).toBe(404);
    expect(error.code).toBe("Product.NotFound");
    expect(error.detail).toBe("Product was not found.");
    expect(error.traceId).toBe("00-abc");
    expect(error.problem).toEqual(problem);
  });

  it("does not crash on non-JSON error bodies", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response("<html>gateway timeout</html>", {
          status: 502,
          headers: { "Content-Type": "text/html" },
        }),
      ),
    );

    let caught: unknown;
    try {
      await apiFetch("/boom");
    } catch (e) {
      caught = e;
    }

    expect(caught).toBeInstanceOf(ApiError);
    const error = caught as ApiError;
    expect(error.status).toBe(502);
    expect(error.code).toBeUndefined();
    expect(error.message).toContain("gateway timeout");
  });
});

describe("previewCheckout", () => {
  it("POSTs to /checkout/preview with the request body and no-store", async () => {
    const request = {
      items: [{ variantId: "var-1", quantity: 2 }],
      deliveryZoneId: "zone-1",
    };
    const responseBody = {
      items: [],
      deliveryZoneId: "zone-1",
      deliveryZoneName: "Test",
      shippingFee: 0,
      merchandiseSubtotal: 0,
      appliedMinimumOrderAmount: 0,
      total: 0,
      meetsMinimumOrder: true,
    };

    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(responseBody), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(previewCheckout(request)).resolves.toEqual(responseBody);

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain("/checkout/preview");
    expect(init.method).toBe("POST");
    expect(init.cache).toBe("no-store");
    expect(init.body).toBe(JSON.stringify(request));
    expect(new Headers(init.headers).get("Content-Type")).toBe("application/json");
  });
});
