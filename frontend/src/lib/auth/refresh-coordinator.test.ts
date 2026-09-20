import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  coordinatedRefresh,
  hasRefreshInFlight,
} from "./auth-api";

describe("coordinatedRefresh single-flight", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        await new Promise((r) => setTimeout(r, 30));
        return new Response(
          JSON.stringify({
            kind: "authenticated",
            userId: "u1",
            accessToken: "a",
            accessTokenExpiresAtUtc: "2099-01-01T00:00:00Z",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shares one underlying refresh Promise for concurrent callers", async () => {
    const p1 = coordinatedRefresh();
    expect(hasRefreshInFlight()).toBe(true);
    const p2 = coordinatedRefresh();
    const [a, b] = await Promise.all([p1, p2]);
    expect(a?.accessToken).toBe("a");
    expect(b?.accessToken).toBe("a");
    expect(fetch).toHaveBeenCalledTimes(1);
    expect(hasRefreshInFlight()).toBe(false);
  });
});
