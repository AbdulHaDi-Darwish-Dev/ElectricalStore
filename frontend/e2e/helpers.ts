/**
 * Shared E2E helpers. Never hardcode production credentials.
 * Supply via env (see e2e/README.md).
 */

export const e2eEnv = {
  baseUrl: process.env.E2E_BASE_URL ?? "http://localhost:3100",
  apiUrl: process.env.E2E_API_URL ?? "http://localhost:5180",
  customerEmail: process.env.E2E_CUSTOMER_EMAIL ?? "",
  customerPassword: process.env.E2E_CUSTOMER_PASSWORD ?? "",
  adminEmail: process.env.E2E_ADMIN_EMAIL ?? "",
  adminPassword: process.env.E2E_ADMIN_PASSWORD ?? "",
  /** Account with some Manage but without shell/read as needed for 403 cases */
  limitedEmail: process.env.E2E_LIMITED_EMAIL ?? "",
  limitedPassword: process.env.E2E_LIMITED_PASSWORD ?? "",
};

export function hasCustomerCreds(): boolean {
  return Boolean(e2eEnv.customerEmail && e2eEnv.customerPassword);
}

export function hasAdminCreds(): boolean {
  return Boolean(e2eEnv.adminEmail && e2eEnv.adminPassword);
}

export function hasLimitedCreds(): boolean {
  return Boolean(e2eEnv.limitedEmail && e2eEnv.limitedPassword);
}

export async function apiHealthy(): Promise<boolean> {
  try {
    const res = await fetch(`${e2eEnv.apiUrl}/health`, {
      signal: AbortSignal.timeout(5000),
    });
    return res.ok;
  } catch {
    return false;
  }
}

export async function frontendHealthy(): Promise<boolean> {
  try {
    const res = await fetch(e2eEnv.baseUrl, {
      signal: AbortSignal.timeout(5000),
    });
    return res.ok || res.status === 404 || res.status === 307 || res.status === 308;
  } catch {
    return false;
  }
}

/** Local stack ready for browser smoke (API + frontend). */
export async function localStackReady(): Promise<boolean> {
  return (await apiHealthy()) && (await frontendHealthy());
}
