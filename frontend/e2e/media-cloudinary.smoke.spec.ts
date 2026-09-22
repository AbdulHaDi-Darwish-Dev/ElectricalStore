import path from "node:path";
import { test, expect } from "@playwright/test";
import {
  E2E_FIXTURE,
  e2eEnv,
  fetchAccessTokenViaLogin,
  fetchE2EProductId,
  hasAdminCreds,
  localStackReady,
} from "./helpers";

const FIXTURE_PNG = path.join(__dirname, "fixtures", "e2e-pixel.png");

type ProductImage = {
  id: string;
  url: string;
  isPrimary: boolean;
  sortOrder: number;
};

type ProductBody = {
  id: string;
  images: ProductImage[];
};

test.describe("Cloudinary real media E2E", () => {
  test.beforeEach(async () => {
    test.skip(!(await localStackReady()), "Local stack not reachable");
    test.skip(!hasAdminCreds(), "Set E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD");
  });

  test("upload / primary / reorder / delete against E2E Product", async ({
    request,
  }) => {
    const productId = await fetchE2EProductId();
    test.skip(!productId, `Product "${E2E_FIXTURE.productName}" missing`);

    const token = await fetchAccessTokenViaLogin(
      request,
      e2eEnv.adminEmail,
      e2eEnv.adminPassword,
    );
    expect(token).toBeTruthy();
    const auth = {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
    };

    const detailBefore = await request.get(
      `${e2eEnv.apiUrl}/admin/products/${productId}`,
      { headers: auth },
    );
    expect(detailBefore.ok()).toBeTruthy();
    const before = (await detailBefore.json()) as ProductBody;
    const originalPrimaryId = before.images.find((i) => i.isPrimary)?.id;
    const originalOrderedIds = [...before.images]
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((i) => i.id);

    // Cap is 4 images — keep room for two uploads.
    expect(before.images.length).toBeLessThanOrEqual(2);

    const uploadedIds: string[] = [];

    try {
      const upload1 = await request.post(
        `${e2eEnv.apiUrl}/admin/products/${productId}/images`,
        {
          headers: auth,
          multipart: {
            file: {
              name: "e2e-pixel.png",
              mimeType: "image/png",
              buffer: await readFixture(),
            },
          },
        },
      );
      const upload1Text = await upload1.text();
      expect(upload1.ok(), `upload1 HTTP ${upload1.status()}: ${upload1Text}`).toBeTruthy();
      const after1 = JSON.parse(upload1Text) as ProductBody;
      const img1 = after1.images.find((i) => !originalOrderedIds.includes(i.id));
      expect(img1, "new image after upload1").toBeTruthy();
      uploadedIds.push(img1!.id);
      expect(img1!.url).toMatch(/^https?:\/\//i);
      expect(img1!.url).not.toContain("img.test");
      expect(
        /cloudinary\.com/i.test(img1!.url),
        `expected Cloudinary URL, got ${img1!.url}`,
      ).toBeTruthy();

      const upload2 = await request.post(
        `${e2eEnv.apiUrl}/admin/products/${productId}/images`,
        {
          headers: auth,
          multipart: {
            file: {
              name: "e2e-pixel-2.png",
              mimeType: "image/png",
              buffer: await readFixture(),
            },
          },
        },
      );
      const upload2Text = await upload2.text();
      expect(upload2.ok(), upload2Text).toBeTruthy();
      const after2 = JSON.parse(upload2Text) as ProductBody;
      const img2 = after2.images.find(
        (i) => !originalOrderedIds.includes(i.id) && i.id !== img1!.id,
      );
      expect(img2).toBeTruthy();
      uploadedIds.push(img2!.id);

      const setPrimary = await request.post(
        `${e2eEnv.apiUrl}/admin/products/${productId}/images/${img2!.id}/primary`,
        { headers: auth },
      );
      expect(setPrimary.ok(), await setPrimary.text()).toBeTruthy();

      const reorder = await request.put(
        `${e2eEnv.apiUrl}/admin/products/${productId}/images/order`,
        {
          headers: {
            ...auth,
            "Content-Type": "application/json",
          },
          data: {
            orderedImageIds: [img2!.id, img1!.id, ...originalOrderedIds],
          },
        },
      );
      expect(reorder.ok(), await reorder.text()).toBeTruthy();

      const adminDetail = await request.get(
        `${e2eEnv.apiUrl}/admin/products/${productId}`,
        { headers: auth },
      );
      expect(adminDetail.ok()).toBeTruthy();
      const adminBody = (await adminDetail.json()) as ProductBody;
      expect(adminBody.images.some((i) => i.id === img1!.id)).toBeTruthy();
      expect(
        adminBody.images.some((i) => i.id === img2!.id && i.isPrimary),
      ).toBeTruthy();

      const publicDetail = await request.get(
        `${e2eEnv.apiUrl}/catalog/products/${productId}`,
        { headers: { Accept: "application/json" } },
      );
      expect(publicDetail.ok()).toBeTruthy();
      const publicBody = (await publicDetail.json()) as {
        primaryImageUrl?: string | null;
        images?: { url: string }[];
      };
      const publicUrls = [
        publicBody.primaryImageUrl,
        ...(publicBody.images?.map((i) => i.url) ?? []),
      ].filter(Boolean) as string[];
      expect(publicUrls.some((u) => u === img2!.url || u.includes("cloudinary"))).toBeTruthy();
    } finally {
      for (const id of uploadedIds) {
        await request.delete(
          `${e2eEnv.apiUrl}/admin/products/${productId}/images/${id}`,
          { headers: auth },
        );
      }

      if (originalPrimaryId) {
        await request.post(
          `${e2eEnv.apiUrl}/admin/products/${productId}/images/${originalPrimaryId}/primary`,
          { headers: auth },
        );
      }

      if (originalOrderedIds.length > 0) {
        await request.put(
          `${e2eEnv.apiUrl}/admin/products/${productId}/images/order`,
          {
            headers: {
              ...auth,
              "Content-Type": "application/json",
            },
            data: { orderedImageIds: originalOrderedIds },
          },
        );
      }
    }
  });
});

async function readFixture(): Promise<Buffer> {
  const fs = await import("node:fs/promises");
  return fs.readFile(FIXTURE_PNG);
}
