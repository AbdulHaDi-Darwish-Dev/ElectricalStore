import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api/client", () => ({
  apiFetch: vi.fn(),
}));

vi.mock("./auth-api", async () => {
  const actual = await vi.importActual<typeof import("./auth-api")>("./auth-api");
  return {
    ...actual,
    coordinatedRefresh: vi.fn(),
  };
});

import { ApiError } from "@/lib/api";
import { apiFetch } from "@/lib/api/client";
import { coordinatedRefresh } from "./auth-api";
import { authenticatedFetch } from "./authenticated-fetch";
import { useAuthStore } from "./session-store";

describe("authenticatedFetch", () => {
  beforeEach(() => {
    useAuthStore.setState({
      status: "authenticated",
      accessToken: "old-token",
      accessTokenExpiresAtUtc: "2099-01-01T00:00:00Z",
      userId: "u1",
      permissions: [],
    });
    vi.mocked(apiFetch).mockReset();
    vi.mocked(coordinatedRefresh).mockReset();
  });

  afterEach(() => {
    useAuthStore.getState().clearSession();
  });

  it("retries once after a single coordinated refresh on 401", async () => {
    vi.mocked(apiFetch)
      .mockRejectedValueOnce(new ApiError(401, { code: "Unauthorized" }))
      .mockResolvedValueOnce({ ok: true });
    vi.mocked(coordinatedRefresh).mockResolvedValue({
      userId: "u1",
      accessToken: "new-token",
      accessTokenExpiresAtUtc: "2099-01-01T00:00:00Z",
    });

    const result = await authenticatedFetch<{ ok: boolean }>("/orders");
    expect(result).toEqual({ ok: true });
    expect(coordinatedRefresh).toHaveBeenCalledTimes(1);
    expect(apiFetch).toHaveBeenCalledTimes(2);
    expect(vi.mocked(apiFetch).mock.calls[1][1]?.accessToken).toBe("new-token");
  });

  it("does not refresh on 403", async () => {
    vi.mocked(apiFetch).mockRejectedValueOnce(
      new ApiError(403, { code: "Forbidden" }),
    );

    await expect(authenticatedFetch("/admin")).rejects.toMatchObject({
      status: 403,
    });
    expect(coordinatedRefresh).not.toHaveBeenCalled();
    expect(apiFetch).toHaveBeenCalledTimes(1);
  });

  it("clears session when refresh fails after 401", async () => {
    vi.mocked(apiFetch).mockRejectedValueOnce(new ApiError(401));
    vi.mocked(coordinatedRefresh).mockResolvedValue(null);

    await expect(authenticatedFetch("/orders")).rejects.toMatchObject({
      status: 401,
    });
    expect(useAuthStore.getState().status).toBe("anonymous");
    expect(useAuthStore.getState().accessToken).toBeNull();
  });
});
