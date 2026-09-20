# Trusted client IP / Forwarded headers

> **Purpose:** Operational rules for correcting `HttpContext.Connection.RemoteIpAddress` before Permixa login rate limiting.  
> **Authority:** Host infrastructure only (Permixa RemoteIp partition semantics unchanged).

## Why this exists

Login rate limiting is partitioned by `RemoteIpAddress` (~20 requests / minute).

When Browser → Next.js Route Handler → ASP.NET, the TCP peer ASP.NET sees is the **Next process**, not the browser. Without a trusted ForwardedHeaders boundary, all storefront users can share one login bucket.

## Repository topology (current facts)

This repository does **not** define production ingress:

| Artifact | Finding |
|----------|---------|
| `docker-compose.yml` | SQL Server only; API service is a **commented logging example** |
| Dockerfiles / nginx / reverse-proxy configs | **None** |
| Hostinger / VPS / CD deploy docs | **None** (`docs/DEFERRED-SCOPE.md`: Deployment / CD deferred) |
| `PROJECT-OVERVIEW.md` | Explicitly **not** a deployment/IaC starter |

**Local Development (documented):**

- API: `http://localhost:5180` (Kestrel)
- Frontend: `http://localhost:3100`
- Browser may call the API directly (`NEXT_PUBLIC_API_BASE_URL`) under scoped CORS
- Auth BFF: Browser → Next → `API_BASE_URL` (server-side)
- Launcher: `.\dev.ps1` / `.\stop-dev.ps1` — dedicated ports; one API owner at a time (launcher **or** Visual Studio)

**Production topology:** **not specified in-repo.** Operators must choose and document one before enabling production ForwardedHeaders.

## Security principle

`X-Forwarded-For` from the public Internet is **attacker-controlled** unless an immediate hop is trusted and sanitizes headers.

Do **not**:

- Clear `KnownProxies` / `KnownNetworks` to trust everyone
- Enable ForwardedHeaders with an empty trust list
- Blindly copy client-supplied `X-Forwarded-For` into outbound Next→API calls without a controlled ingress

## Host configuration

Section: `ForwardedHeaders`

| Key | Meaning |
|-----|---------|
| `Enabled` | When `false`, middleware is not registered (fail-safe default in `appsettings.json`) |
| `KnownProxies` | Exact IP strings of the immediate trusted hop |
| `KnownNetworks` | CIDR strings (`10.0.0.0/8`) for the immediate trusted hop |

**Headers applied (when enabled):** `X-Forwarded-For`, `X-Forwarded-Proto`  
**ForwardLimit:** `1` (single sanitised client IP from the trusted hop)

**Fail-safe:** `Enabled=true` with empty KnownProxies **and** empty KnownNetworks → middleware is **not** registered (logged as warning).

### Development

`appsettings.Development.json` enables trust for loopback only:

```json
"ForwardedHeaders": {
  "Enabled": true,
  "KnownProxies": [ "127.0.0.1", "::1" ],
  "KnownNetworks": []
}
```

Local Next on the same machine can forward a client IP; ASP.NET accepts it only from loopback.

### Authentication application vs production go-live

| | Status |
|--|--------|
| Authentication **application code** (Next BFF, cookies, account UI) | Implemented for **local** development/testing |
| **Production Auth deployment** | **BLOCKED** until trusted ingress + KnownProxies/Networks + RemoteIp verification |

Local login BFF does **not** read browser `X-Forwarded-For`, `X-Real-IP`, or `Forwarded`.
It always forwards the explicit loopback identity `127.0.0.1` (Development KnownProxies).
That is a **local development identity only** — shared Login bucket for one developer; it does **not** prove production per-client partitioning.

### Production

Committed Production baseline keeps `Enabled=false` (inherits `appsettings.json`).

Operators must set environment / secrets, for example:

```bash
ForwardedHeaders__Enabled=true
ForwardedHeaders__KnownProxies__0=<next-or-proxy-ip>
# and/or
ForwardedHeaders__KnownNetworks__0=<docker-bridge-cidr>
```

Without those values, RemoteIp remains the direct TCP peer (safe against spoofing; **unsafe** for shared Next-IP rate-limit buckets if login is proxied through Next).

## Middleware order

In `Program.cs`:

1. `UseElectricalStoreForwardedHeaders()` — correct RemoteIp first  
2. `UseExceptionHandler`  
3. `UseCors`  
4. `UseElectricalStoreRequestLogging`  
5. `UseAuthentication`  
6. `UseRateLimiter` ← Permixa Login RemoteIp partition uses corrected IP  
7. `UseAuthorization`

## Intended trust chain (when deployment exists)

Conceptually:

```
Browser
  → trusted ingress (reverse proxy) that overwrites/sanitises client IP
    → Next.js (reads that trusted client IP)
      → ASP.NET (trusts only Next/proxy IP via KnownProxies/Networks)
        → Connection.RemoteIpAddress = real client
        → Login rate limiter partitions per client
```

**Next forwarding (local Auth BFF):** Login sets `X-Forwarded-For: 127.0.0.1` only — never from request headers. Production client-IP forwarding remains unimplemented until trusted ingress + KnownProxies/Networks exist.

## Direct API exposure

Today the storefront is designed for **direct browser → API** catalog/checkout calls (CORS). That implies the API may remain reachable from browsers in production unless operators place it behind the same origin/proxy later.

ForwardedHeaders does **not** widen CORS. Spoofed `X-Forwarded-For` from a public browser is ignored when the TCP peer is not in KnownProxies/Networks.

## Misconfiguration outcomes

| Situation | Result |
|-----------|--------|
| Disabled / empty trust | RemoteIp = TCP peer; no spoof; Next-proxied logins share Next’s IP bucket |
| Enabled + correct KnownProxies + sanitized XFF | RemoteIp = client; per-client login partitions |
| Enabled + wrong/missing trust | Same as disabled for untrusted peers; forwarded headers ignored |

## Tests

`ForwardedHeadersApiTests` cover trusted rewrite, untrusted spoof rejection, disabled behaviour, distinct/same client IPs, and Login 429 partition separation for two forwarded clients behind one trusted proxy.
