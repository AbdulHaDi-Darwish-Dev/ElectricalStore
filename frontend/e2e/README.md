# Frontend E2E (Playwright)

Local/smoke only — **never** point at production.

## Prerequisites

1. API on `http://localhost:5180` (`GET /health` → ok)
2. Frontend on `http://localhost:3100`
3. Optional seeded users for auth/admin specs

```powershell
# from repo root
.\dev.ps1
```

## Environment

| Variable | Purpose |
|----------|---------|
| `E2E_BASE_URL` | Frontend (default `http://localhost:3100`) |
| `E2E_API_URL` | API (default `http://localhost:5180`) |
| `E2E_CUSTOMER_EMAIL` / `E2E_CUSTOMER_PASSWORD` | Customer login specs |
| `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` | Admin shell smoke |
| `E2E_LIMITED_EMAIL` / `E2E_LIMITED_PASSWORD` | Permission-denied cases |

## Credentials (optional for auth/admin specs)

Set in the shell (never commit):

```powershell
$env:E2E_CUSTOMER_EMAIL="..."
$env:E2E_CUSTOMER_PASSWORD="..."
$env:E2E_ADMIN_EMAIL="..."
$env:E2E_ADMIN_PASSWORD="..."
$env:E2E_LIMITED_EMAIL="..."
$env:E2E_LIMITED_PASSWORD="..."
```

Without them, credential-dependent tests **skip**; anonymous storefront/auth-failure/admin-redirect still run when the stack is up.

## Commands

```bash
cd frontend
npx playwright install chromium   # once
npm run test:e2e                  # all smoke
npm run test:e2e -- e2e/storefront.smoke.spec.ts
```

Specs that need credentials **skip** when env vars are empty.
Specs that need API **skip** when `/health` is unreachable.

## Coverage (smoke)

- Anonymous storefront / catalog shells
- Login success & failure (when customer creds set)
- Protected `/account` and `/admin` redirects
- Admin module navigation (when admin creds set)
- Cart add path (best-effort against live catalog)
- Guest checkout UI reachability (full place-order needs stock + zones)

## Cleanup

No dedicated DB reset in MVP — use disposable local DB / re-seed via Development AppSeed.
