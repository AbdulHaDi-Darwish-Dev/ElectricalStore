import { describe, expect, it } from "vitest";
import {
  AUTH_REFRESH_COOKIE_NAME,
  AUTH_REFRESH_COOKIE_PATH,
  authRefreshCookieOptions,
  clearAuthRefreshCookieOptions,
  maxAgeSecondsFromExpiresAtUtc,
} from "@/lib/auth/refresh-cookie";
import {
  LOCAL_DEV_LOGIN_CLIENT_IP,
  resolveForwardableClientIp,
  resolveLocalDevLoginClientIp,
} from "@/lib/auth/client-ip";
import { isSafeReturnTo, resolveSafeReturnTo } from "@/lib/auth/return-to";
import { hasAnyPermission, hasPermission } from "@/lib/auth/permissions";
import {
  isAuthenticationResult,
  isMfaLoginChallenge,
  toBrowserAuthTokens,
} from "@/lib/auth/types";
import { PRIVATE_NO_STORE_HEADERS } from "@/lib/security/same-origin";

describe("auth refresh cookie", () => {
  it("uses technical cookie name and narrow auth path", () => {
    expect(AUTH_REFRESH_COOKIE_NAME).toBe("electricalstore.auth.refresh");
    expect(AUTH_REFRESH_COOKIE_PATH).toBe("/api/auth");
  });

  it("is HttpOnly, SameSite=Lax, Secure only in production", () => {
    const expires = new Date(Date.now() + 3_600_000).toISOString();
    const dev = authRefreshCookieOptions({
      expiresAtUtc: expires,
      nodeEnv: "development",
    });
    expect(dev.httpOnly).toBe(true);
    expect(dev.sameSite).toBe("lax");
    expect(dev.secure).toBe(false);
    expect(dev.path).toBe("/api/auth");
    expect(dev.maxAge).toBeGreaterThan(3500);

    const prod = authRefreshCookieOptions({
      expiresAtUtc: expires,
      nodeEnv: "production",
    });
    expect(prod.secure).toBe(true);
  });

  it("derives Max-Age from refreshTokenExpiresAtUtc", () => {
    const now = Date.UTC(2026, 0, 1, 12, 0, 0);
    const expires = new Date(now + 120_000).toISOString();
    expect(maxAgeSecondsFromExpiresAtUtc(expires, now)).toBe(120);
    expect(maxAgeSecondsFromExpiresAtUtc("not-a-date", now)).toBe(0);
  });

  it("clears cookie with maxAge 0 on same path", () => {
    const cleared = clearAuthRefreshCookieOptions({ nodeEnv: "production" });
    expect(cleared.maxAge).toBe(0);
    expect(cleared.path).toBe("/api/auth");
    expect(cleared.httpOnly).toBe(true);
    expect(cleared.secure).toBe(true);
  });
});

describe("login response stripping", () => {
  it("strips refreshToken from browser-visible tokens", () => {
    const auth = {
      userId: "u1",
      accessToken: "access",
      accessTokenExpiresAtUtc: "2026-01-01T00:00:00Z",
      refreshToken: "refresh-secret",
      refreshTokenExpiresAtUtc: "2026-01-08T00:00:00Z",
    };
    expect(isAuthenticationResult(auth)).toBe(true);
    const browser = toBrowserAuthTokens(auth);
    expect(browser).toEqual({
      userId: "u1",
      accessToken: "access",
      accessTokenExpiresAtUtc: "2026-01-01T00:00:00Z",
    });
    expect(browser).not.toHaveProperty("refreshToken");
  });

  it("detects MFA branch without treating it as authentication", () => {
    const mfa = { mfaProof: "proof", expiresAtUtc: "2026-01-01T00:00:00Z" };
    expect(isMfaLoginChallenge(mfa)).toBe(true);
    expect(isAuthenticationResult(mfa)).toBe(false);
  });
});

describe("client IP forwarding (local — no trusted ingress)", () => {
  it("ignores spoofed x-real-ip and always uses loopback", () => {
    const request = new Request("http://localhost:3000/api/auth/login", {
      headers: {
        "x-real-ip": "203.0.113.10",
      },
    });
    expect(resolveForwardableClientIp(request)).toBe(LOCAL_DEV_LOGIN_CLIENT_IP);
    expect(resolveLocalDevLoginClientIp()).toBe("127.0.0.1");
  });

  it("ignores spoofed x-forwarded-for", () => {
    const request = new Request("http://localhost:3000/api/auth/login", {
      headers: {
        "x-forwarded-for": "203.0.113.20",
      },
    });
    expect(resolveForwardableClientIp(request)).toBe("127.0.0.1");
  });

  it("ignores Forwarded and combined spoof headers", () => {
    const request = new Request("http://localhost:3000/api/auth/login", {
      headers: {
        "x-real-ip": "203.0.113.10",
        "x-forwarded-for": "203.0.113.20",
        forwarded: "for=203.0.113.30",
      },
    });
    expect(resolveForwardableClientIp(request)).toBe("127.0.0.1");
  });

  it("uses the approved local loopback development identity without headers", () => {
    expect(resolveLocalDevLoginClientIp()).toBe(LOCAL_DEV_LOGIN_CLIENT_IP);
  });

  it("does not fall back to trusting request headers (production forwarding unimplemented)", () => {
    // Helper has no production header path — spoofed headers never become identity.
    const request = new Request("https://store.example/api/auth/login", {
      headers: {
        "x-real-ip": "198.51.100.1",
        "x-forwarded-for": "198.51.100.2",
        forwarded: "for=198.51.100.3",
      },
    });
    expect(resolveForwardableClientIp(request)).toBe(LOCAL_DEV_LOGIN_CLIENT_IP);
  });
});

describe("returnTo security", () => {
  it("accepts safe relative paths", () => {
    expect(isSafeReturnTo("/account")).toBe(true);
    expect(isSafeReturnTo("/account/orders")).toBe(true);
    expect(resolveSafeReturnTo("/account/orders")).toBe("/account/orders");
  });

  it("rejects external, protocol-relative, and javascript targets", () => {
    expect(isSafeReturnTo("https://evil.example")).toBe(false);
    expect(isSafeReturnTo("//evil.example")).toBe(false);
    expect(isSafeReturnTo("/\\evil.example")).toBe(false);
    expect(isSafeReturnTo("javascript:alert(1)")).toBe(false);
    expect(isSafeReturnTo("/javascript:alert(1)")).toBe(false);
    expect(resolveSafeReturnTo("//evil")).toBe("/account");
  });
});

describe("permissions UX helpers", () => {
  it("hasPermission checks exact codes", () => {
    expect(hasPermission(["Orders.Read", "Orders.Manage"], "Orders.Read")).toBe(
      true,
    );
    expect(hasPermission(["Orders.Read"], "Orders.Manage")).toBe(false);
    expect(hasPermission(null, "Orders.Read")).toBe(false);
  });

  it("hasAnyPermission matches any code", () => {
    expect(
      hasAnyPermission(["A", "B"], ["X", "B"]),
    ).toBe(true);
    expect(hasAnyPermission(["A"], ["X", "Y"])).toBe(false);
  });
});

describe("auth private caching", () => {
  it("uses private no-store headers", () => {
    expect(PRIVATE_NO_STORE_HEADERS["Cache-Control"]).toContain("no-store");
  });
});
