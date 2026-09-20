import type { PlaceOrderRequest } from "@/features/orders";

export const IDEMPOTENCY_ATTEMPT_STORAGE_KEY = "electricalstore.checkout-attempt";

export type IdempotencyAttempt = {
  key: string;
  /** SHA-256 hex of normalized Place Order intent — no plaintext PII stored. */
  fingerprint: string;
};

/** Normalize Place Order payload for stable fingerprinting. */
export function normalizePlaceOrderIntent(request: PlaceOrderRequest): string {
  const items = [...request.items]
    .map((item) => ({
      variantId: item.variantId.trim().toLowerCase(),
      quantity: Number(item.quantity),
    }))
    .sort((a, b) => a.variantId.localeCompare(b.variantId));

  const normalized = {
    items,
    deliveryZoneId: request.deliveryZoneId.trim().toLowerCase(),
    customerName: request.customerName.trim(),
    phone: request.phone.trim(),
    addressText: request.addressText.trim(),
    customerNote: (request.customerNote ?? "").trim(),
  };

  return JSON.stringify(normalized);
}

export async function hashPlaceOrderFingerprint(
  request: PlaceOrderRequest,
): Promise<string> {
  const payload = normalizePlaceOrderIntent(request);
  const data = new TextEncoder().encode(payload);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return [...new Uint8Array(digest)]
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");
}

/** Generate a high-entropy Idempotency-Key (UUID, 36 chars within 16–128). */
export function createIdempotencyKey(): string {
  return crypto.randomUUID();
}

export function readIdempotencyAttempt(): IdempotencyAttempt | null {
  const storage = getSessionStorage();
  if (!storage) {
    return null;
  }
  try {
    const raw = storage.getItem(IDEMPOTENCY_ATTEMPT_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as IdempotencyAttempt;
    if (
      typeof parsed?.key !== "string" ||
      parsed.key.length < 16 ||
      typeof parsed?.fingerprint !== "string" ||
      parsed.fingerprint.length < 16
    ) {
      storage.removeItem(IDEMPOTENCY_ATTEMPT_STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    storage.removeItem(IDEMPOTENCY_ATTEMPT_STORAGE_KEY);
    return null;
  }
}

export function writeIdempotencyAttempt(attempt: IdempotencyAttempt): void {
  const storage = getSessionStorage();
  if (!storage) {
    return;
  }
  storage.setItem(IDEMPOTENCY_ATTEMPT_STORAGE_KEY, JSON.stringify(attempt));
}

export function clearIdempotencyAttempt(): void {
  const storage = getSessionStorage();
  if (!storage) {
    return;
  }
  storage.removeItem(IDEMPOTENCY_ATTEMPT_STORAGE_KEY);
}

function getSessionStorage(): Storage | null {
  try {
    if (typeof globalThis.sessionStorage === "undefined") {
      return null;
    }
    return globalThis.sessionStorage;
  } catch {
    return null;
  }
}

/**
 * Resolve key for an intended Place Order submission.
 * Same fingerprint → reuse key (retry). Different → new key.
 * forceNew: used when backend returns IdempotencyReplayUnavailable.
 */
export async function resolveIdempotencyKey(
  request: PlaceOrderRequest,
  options?: { forceNew?: boolean },
): Promise<string> {
  const fingerprint = await hashPlaceOrderFingerprint(request);
  const existing = options?.forceNew ? null : readIdempotencyAttempt();

  if (existing && existing.fingerprint === fingerprint) {
    return existing.key;
  }

  const key = createIdempotencyKey();
  writeIdempotencyAttempt({ key, fingerprint });
  return key;
}
