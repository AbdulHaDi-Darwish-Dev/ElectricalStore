# Release readiness

> **Purpose:** Production launch audit, topology proposal, environment matrix, and operational checklist.  
> **Authority:** Verified against repo state during Hardening & Release Readiness.  
> **Does not** authorize enabling Production Auth until trusted ingress is configured.

## Executive status

**Hardening baseline LOCKED** (code + production compose/nginx/trust chain in-repo).

**Not yet a successful production launch** until operators complete host deployment and the **HIGH launch-verification** items below.

In-repo: topology, KnownProxies `10.80.0.10`/`10.80.0.20`, Login BFF fresh XFF, DP persistence, CORS deny-by-default, security headers (CSP deferred), restore drill script, Playwright smoke, frontend CI. See [PRODUCTION.md](./PRODUCTION.md).

### HIGH launch-verification (open — do not mark complete)

These do **not** block locking the hardening baseline; they **must** remain open until actually executed on a real launch path:

1. Credentialled Admin E2E (local test admin — not production Owner secrets)
2. Limited-permission Admin E2E including **403 ≠ logout**
3. `nginx -t` against the **actual rendered** production config in a running nginx container
4. Real VPS / domain / TLS deployment smoke

---

## 1. Verified capability matrix (repo facts)

| Area | Status | Evidence |
|------|--------|----------|
| Frontend (Next 16, ports 3100) | Implemented | `frontend/` |
| ASP.NET API (Kestrel 5180 local / 8080 prod) | Implemented | `src/ElectricalStore.Api` |
| SQL Server | Dev via compose or local instance; prod private | `docker-compose.yml` / `docker-compose.prod.yml` |
| Authentication (Permixa) | Local ready; Prod trust chain **approved** | F6 + `docs/TRUSTED-CLIENT-IP.md` + `PRODUCTION.md` |
| Authorization (permission codes) | Implemented | App + `Iam.*` |
| Checkout / Orders / Inventory | Implemented | Domain + API + Admin UI |
| Media (Cloudinary) | Implemented; secrets via config | `Media` / `Cloudinary` sections |
| Admin + IAM UI | Locked F7–F7.7 | `5b473f4` |
| CORS | Deny-by-default; same-origin `/aspnet` | `Cors:AllowedOrigins: []` + prod compose |
| ForwardedHeaders | Prod compose: Enabled + KnownProxies nginx/Next only | `docker-compose.prod.yml`; appsettings fail-safe off until env set |
| Reverse proxy / TLS / full Compose | **In-repo** | `deploy/nginx`, Dockerfiles, `docker-compose.prod.yml` |
| Logging | Console JSON in Production appsettings | `appsettings.Production.json` |
| Health | Liveness only `{ status: "ok" }` | `GET /health` |
| Backups | Documented + local restore drill PASSED | `docs/BACKUP-RESTORE.md`, `scripts/backup-restore-drill.ps1` |
| CI | Backend + frontend jobs | `.github/workflows/ci.yml` |
| E2E (Playwright) | Smoke suite; credentialled admin/limited **open** | `frontend/e2e/` |
| Unit/integration tests | Frontend Vitest + .NET test projects | `docs/TESTING-STRATEGY.md` |

---

## 2. Production Auth / trusted client IP (unchanged fail-safe)

Verified:

- `appsettings.json`: `ForwardedHeaders:Enabled=false`, `KnownProxies=[]`, `KnownNetworks=[]`
- `appsettings.Production.json`: does **not** enable ForwardedHeaders (inherits fail-safe)
- `ForwardedHeadersHostExtensions`: Enabled=true with empty trust lists → middleware **not** registered
- Login rate limit: sliding window **20 / minute**, partition **RemoteIp** (`PermixaServiceCollectionExtensions`)
- Local Next login BFF always sends `127.0.0.1` (not browser XFF)

**Do not enable ForwardedHeaders in Production until KnownProxies/Networks match a real ingress hop.**

Full rules: [TRUSTED-CLIENT-IP.md](./TRUSTED-CLIENT-IP.md).

---

## 3. Recommended minimal production topology

Repo does **not** define production ingress. Recommended shape (operators must implement outside or later in-repo):

```
Internet
  → Reverse proxy (TLS termination)     [PUBLIC :443]
       → Next.js frontend               [PRIVATE, e.g. 3100]
            → ASP.NET API               [PRIVATE, e.g. 5180]  (BFF + optional)
       → ASP.NET API (optional direct)  [PRIVATE] for browser CORS calls
  → SQL Server                          [PRIVATE :1433, not published]

Client IP trust:
  Proxy overwrites/sanitises X-Forwarded-For to the real client
  ASP.NET KnownProxies = proxy IP (or KnownNetworks = private bridge CIDR)
  ForwardLimit = 1
  Next Auth BFF must forward the *trusted* connecting IP ASP.NET expects
    (not raw browser headers)
```

| Concern | Recommendation |
|---------|----------------|
| TLS | Terminate at reverse proxy; cookies `Secure` when `NODE_ENV=production` |
| Public ports | 443 only (and 80→443 redirect) |
| Private | Next, API, SQL |
| Browser → API | Prefer same-origin via Next BFF/proxy for auth; if direct CORS, set exact HTTPS origin only |
| CORS | `Cors__AllowedOrigins__0=https://shop.example.com` — no wildcards |
| Data Protection keys | Persist `DataProtection:KeysPath` on durable volume (multi-instance needs shared ring — deferred Azure/cert) |

**Until this exists: Production Auth/Admin = BLOCKER.**

---

## 4. Environment & secrets matrix

### Backend (Production — env / secret store, never commit)

| Key | Notes |
|-----|--------|
| `ConnectionStrings__Default` | SQL auth or managed identity; strong password |
| `Permixa__Jwt__PrivateKeyPem` / `PublicKeyPem` | RSA; never commit PEMs |
| `Permixa__Jwt__Issuer` / `Audience` | Stable production values |
| `Permixa__Bootstrap__*` | **Disabled** in Production |
| `Permixa__AppSeed__Enabled` | **false** |
| `Cloudinary__CloudName` / `ApiKey` / `ApiSecret` | Server-only |
| `Cors__AllowedOrigins__0` | Exact `https://…` frontend origin |
| `ForwardedHeaders__Enabled` | `true` only after Known* set |
| `ForwardedHeaders__KnownProxies__0` | Immediate proxy IP |
| `DataProtection__KeysPath` | Durable path |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| Logging | JSON console already in Production appsettings |

### Frontend

| Key | Notes |
|-----|--------|
| `API_BASE_URL` | Internal/server URL to ASP.NET (BFF) |
| `NEXT_PUBLIC_API_BASE_URL` | Only if browser calls API directly; must match CORS |
| `NEXT_PUBLIC_SITE_URL` | Public HTTPS origin (metadata/sitemap) |
| `SITE_URL` | Optional server same-origin override |
| `NODE_ENV` | `production` → Secure cookies |

### Secrets scan (Hardening pass)

| Finding | Severity |
|---------|----------|
| `.gitignore` excludes `.env*`, `*.pem`, `secrets.json`, `dp-keys/` | OK |
| `.env.example` / `frontend/.env.example` — placeholders only | OK |
| Postman env passwords empty placeholders | OK |
| `appsettings.Development.json` commits machine-specific SQL `Trusted_Connection` string (no SQL password) | LOW–MEDIUM (dev machine name leakage; move to user-secrets) |
| `docker-compose.yml` default SA password is **dev-only** placeholder via env | OK if not used in prod |
| No committed JWT PEMs / Cloudinary secrets found in tracked files | OK |

---

## 5. Guest order / session / idempotency (verified)

| Control | Status |
|---------|--------|
| Access token | Memory (Zustand); not localStorage |
| Refresh cookie | HttpOnly, path `/api/auth`, SameSite=Lax, Secure in production |
| Guest order cookie | HttpOnly, order-scoped path, SameSite=Lax, Secure in production; stripped from browser DTOs |
| Idempotency | Frontend fingerprint: same key only if payload hash matches; **backend** replays by key without payload hash — frontend mitigation required and present |
| Login rate limit | 20/min RemoteIp |

---

## 6. Database / migrations

- Dev: auto-migrate + AppSeed only when `IsDevelopment()` (`DevelopmentInitializationExtensions`)
- Production: **must not** rely on startup migrate; apply Permixa then App migrations in release process (`docs/DEVELOPMENT-GUIDE.md`)
- Backup before migrate; see [BACKUP-RESTORE.md](./BACKUP-RESTORE.md)

---

## 7. Pagination / scalability

| Endpoint | Paging | Launch class |
|----------|--------|--------------|
| Admin Orders list | **None** | Acceptable MVP if order volume low; fix before scale |
| Admin Products / Categories / Inventory / Shipping | Unpaginated lists | MVP OK for small catalogs |
| IAM Users / Audit | Paged | OK |
| Public catalog | Existing query patterns + ISR | OK |

Do **not** fake client-only pagination over huge payloads.

---

## 8. Deferred IAM host adapters (not release-blocking for COD store)

Sessions, user lock/email, permission create/update — Permixa use cases exist; host HTTP adapters not wired (`docs/DEFERRED-SCOPE.md`). Store can launch without them if Owner uses seeded permissions carefully.

---

## 9. Observability / health

| Item | Status |
|------|--------|
| `/health` | Process liveness only — no DB check (safe; add separate ready probe later if needed) |
| Request logging | Host middleware; skips health path |
| Production logs | JSON console formatter |
| Correlation / metrics / uptime | Minimal — recommend external uptime on `/health` + log ship from stdout |
| Security headers (HSTS, CSP, …) | **Not** implemented in-repo — proxy task |

---

## 10. Release blockers vs follow-ups

See tables in Hardening report / section below checklist.

### Code/infra blockers (resolved in hardening baseline)

1. ~~Define reverse-proxy + TLS topology~~ — `deploy/nginx` + `docker-compose.prod.yml`
2. ~~Configure ForwardedHeaders KnownProxies + verify RemoteIp~~ — KnownProxies `10.80.0.10` / `10.80.0.20` only; proofs in `ForwardedHeadersApiTests`
3. ~~Production CORS model~~ — deny-by-default; same-origin `/aspnet`
4. ~~Secrets outside git~~ — `deploy/env.production.example`; `.env.production` gitignored
5. ~~Explicit migration + backup/restore drill~~ — docs + `scripts/backup-restore-drill.ps1` (drill PASSED locally)

### Operator / host (required before declaring launch successful)

1. Fill `.env.production` + mount TLS certs on the target host  
2. Apply Permixa then App migrations on private SQL  
3. Bring up compose; verify health + RemoteIp with two real clients  
4. Complete **HIGH launch-verification** items listed in Executive status  

### Non-blocking follow-ups (engineering judgment)

- Admin Orders pagination  
- Full IAM session/lock HTTP adapters  
- DB-backed health ready probe  
- CSP (deferred until safely designed)  
- Playwright in default CI (requires live stack)  

---

## 11. Launch checklist

### Infrastructure
- [ ] VPS/host chosen; SQL not publicly exposed  
- [ ] Reverse proxy + TLS certificates  
- [ ] Private networking Next ↔ API ↔ SQL  
- [ ] KnownProxies/Networks documented and applied  
- [ ] ForwardedHeaders enabled only after RemoteIp verified  

### Security
- [ ] CORS exact HTTPS origin  
- [ ] No Production Auth over HTTP  
- [ ] Cookies Secure in production  
- [ ] Secrets in vault/env — not git  
- [ ] Bootstrap/AppSeed disabled  
- [ ] Rate limit RemoteIp = real client  

### Database
- [ ] Backup taken  
- [ ] Permixa migrations applied  
- [ ] App migrations applied  
- [ ] Restore drill documented/tested  

### Frontend / Backend
- [ ] `NEXT_PUBLIC_SITE_URL` = production HTTPS  
- [ ] `API_BASE_URL` internal  
- [ ] API `ASPNETCORE_ENVIRONMENT=Production`  
- [ ] Cloudinary production cloud  
- [ ] DataProtection keys persisted  

### Smoke tests
- [ ] Anonymous catalog  
- [ ] Guest checkout + confirmation reload  
- [ ] Login / refresh / logout  
- [ ] Account orders  
- [ ] Admin module smoke (Categories…IAM) with least-privilege accounts  
- [ ] 403 does not logout  
- [ ] `/health` monitored  

### Ops
- [ ] Log retention / rotation  
- [ ] Uptime check on `/health`  
- [ ] Incident contact + rollback plan  

---

## 12. E2E

See `frontend/e2e/README.md`. Requires local API + frontend; not production.
