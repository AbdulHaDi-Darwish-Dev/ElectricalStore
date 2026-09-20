# Production deployment

> **Purpose:** Operational production topology for ElectricalStore.  
> **Authority:** Deployment / ingress / trust boundary.  
> **Local Dev** remains `dev.ps1` + `docker-compose.yml` (SQL only) — do not replace.

## Final topology

```
Internet clients
      │
      ▼
┌─────────────────────────────┐
│  Nginx :443 (TLS terminate) │  PUBLIC only
│  STORE_HOST                 │
│  headers overwritten:       │
│   X-Forwarded-For=$remote_addr
│   X-Forwarded-Proto=$scheme
│   X-ElectricalStore-Client-Ip=$remote_addr
└────────────┬────────────────┘
             │ private net 10.80.0.0/24
     ┌───────┴────────┐
     ▼                ▼
┌──────────┐   ┌─────────────────────┐
│ frontend │   │ api :8080           │
│ Next :3000│   │ ASP.NET            │
│ 10.80.0.20│   │ ForwardedHeaders   │
└────┬─────┘   │ KnownProxies=      │
     │         │  10.80.0.10 (nginx)│
     │ BFF     │  10.80.0.20 (Next) │
     └────────►│ DataProtection vol │
               │ 10.80.0.30         │
               └──────────┬──────────┘
                          ▼
                 ┌────────────────┐
                 │ sqlserver      │  NOT published
                 │ 10.80.0.40     │
                 └────────────────┘
```

### Public / private ports

| Port | Service | Exposure |
|------|---------|----------|
| 80 / 443 | Nginx | **Public** |
| 3000 | Next.js | Private Docker only |
| 8080 | ASP.NET | Private Docker only |
| 1433 | SQL Server | Private Docker only |

### Routing

| Path | Target |
|------|--------|
| `/` | Next.js (pages + `/api/auth` + `/api/guest-orders` BFF) |
| `/aspnet/` | ASP.NET (prefix stripped) — **same origin** as the store |

Browser catalog/checkout uses `NEXT_PUBLIC_API_BASE_URL=https://$STORE_HOST/aspnet` → same origin → **CORS usually unused**. Keep `Cors:AllowedOrigins` deny-by-default unless a separate API host is introduced.

Server-side BFF uses `API_BASE_URL=http://api:8080` (private), never the public hostname.

## Trusted client IP

### Login BFF (exact)

1. Client TCP → Nginx `$remote_addr`
2. Nginx **overwrites** to Next: `X-ElectricalStore-Client-Ip`, `X-Forwarded-For`, `X-Forwarded-Proto`
3. Next (`AUTH_TRUST_PROXY=true`) reads **only** `X-ElectricalStore-Client-Ip` (never browser XFF)
4. Next → ASP.NET private call sets a **fresh** `X-Forwarded-For: <trusted IP>` (`frontend/src/app/api/auth/login/route.ts`)
5. ASP.NET TCP peer = Next `10.80.0.20` (KnownProxy)
6. `ForwardedHeadersMiddleware` rewrites `Connection.RemoteIpAddress` from that XFF (`ForwardLimit=1`)
7. Login rate limiter partitions on RemoteIp

**There is no ASP.NET middleware for `X-ElectricalStore-Client-Ip`.** That header is Next-only.

### Direct `/aspnet/*`

1. Client → Nginx → ASP.NET
2. TCP peer = Nginx `10.80.0.10` (KnownProxy)
3. Nginx-overwritten XFF → RemoteIp = client

### KnownProxies (narrow — not the whole subnet)

| IP | Role |
|----|------|
| `10.80.0.10` | Nginx |
| `10.80.0.20` | Next.js Login BFF |

SQL `10.80.0.40` is **not** trusted. Do **not** use `KnownNetworks=10.80.0.0/24`.

Integration tests: `ForwardedHeadersApiTests` (dual proxies, custom-header ignored, spoof ignored, login 429 partition).

## Quick start (operator)

```bash
cp deploy/env.production.example .env.production
# fill secrets; place TLS files in deploy/certs/{fullchain.pem,privkey.pem}

docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
```

Validate Nginx config inside the container:

```bash
docker compose -f docker-compose.prod.yml --env-file .env.production exec nginx nginx -t
```

## Migrations (explicit — no startup migrate in Production)

Order: **Permixa IAM first**, then App.

```bash
# From a workstation/CI with connection to private SQL (or one-off migrate container)
dotnet ef database update \
  --project <Permixa migrations project or package guidance> \
  --connection "$ConnectionStrings__Default"

dotnet ef database update \
  --project src/ElectricalStore.Infrastructure/ElectricalStore.Infrastructure.csproj \
  --startup-project src/ElectricalStore.Api/ElectricalStore.Api.csproj \
  --context AppDbContext \
  --connection "$ConnectionStrings__Default"
```

Release sequence: backup → migrate Permixa → migrate App → deploy compose → `/health` + smoke.

## Data Protection

- Env: `DataProtection__KeysPath=/var/lib/electricalstore/dp-keys`
- Volume: `es_dp_keys`
- Production **refuses** empty KeysPath (no ephemeral fallback)

Keys at rest are filesystem-protected by host volume permissions only (MVP tradeoff — document if multi-host).

## Secrets

See `deploy/env.production.example`. Never commit `.env.production` or PEMs.

## TLS

Mount certs at `deploy/certs/`. Compatible with Certbot files (`fullchain.pem`, `privkey.pem`). HTTP serves ACME challenge path then redirects to HTTPS.

## Security headers (Nginx HTTPS)

- HSTS
- X-Content-Type-Options: nosniff
- Referrer-Policy: strict-origin-when-cross-origin
- X-Frame-Options: SAMEORIGIN
- CSP: **deferred** (Next.js)

## Failure modes

| Missing | Behavior |
|---------|----------|
| ForwardedHeaders Enabled + empty Known* (non-Production) | Middleware **not** registered (fail-safe) |
| ForwardedHeaders Enabled + empty Known* (Production) | **Startup throw** |
| Production empty DataProtection:KeysPath | **Startup throw** |
| Missing JWT PEMs / connection string | **Startup throw** |
| AUTH_TRUST_PROXY without Nginx client header | Login BFF **503** |
| Empty CORS + separate API host | Browser CORS fails (by design) |

## HIGH launch-verification (open)

Do **not** mark these complete until executed:

1. Credentialled Admin E2E (local test admin accounts only)
2. Limited-permission Admin E2E including 403 ≠ logout
3. `nginx -t` on the actual rendered production config
4. Real VPS / domain / TLS deployment smoke

## Related

- [TRUSTED-CLIENT-IP.md](./TRUSTED-CLIENT-IP.md)
- [BACKUP-RESTORE.md](./BACKUP-RESTORE.md)
- [RELEASE-READINESS.md](./RELEASE-READINESS.md)
