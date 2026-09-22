# Frontend E2E (Playwright)

Local/smoke only — **never** point at production.

## Prerequisites

1. API on `http://localhost:5180` (`GET /health` → ok)
2. Frontend on `http://localhost:3100`
3. Local E2E fixtures + accounts (Development only)

```powershell
# from repo root
.\dev.ps1
.\scripts\prepare-local-e2e.ps1
.\stop-dev.ps1
.\dev.ps1
```

`prepare-local-e2e.ps1` writes gitignored `frontend/.env.e2e.local` and LocalDevFixtures user-secrets (passwords not printed). Restart the API so `LocalDevAccountFixtureSeeder` / catalog seed run. Playwright loads `.env.e2e.local` via `playwright.config.ts`.

No Owner password is required for this path.

## Environment

| Variable | Purpose |
|----------|---------|
| `E2E_BASE_URL` | Frontend (default `http://localhost:3100`) |
| `E2E_API_URL` | API (default `http://localhost:5180`) |
| `E2E_CUSTOMER_EMAIL` / `E2E_CUSTOMER_PASSWORD` | Customer login specs |
| `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` | Admin shell / fulfillment |
| `E2E_LIMITED_EMAIL` / `E2E_LIMITED_PASSWORD` | Permission-boundary cases |

Alternatively set the same vars in the shell before tests (never commit).

Without credentials, credential-dependent tests **skip**; anonymous storefront/auth-failure/admin-redirect still run when the stack is up.

## Commands

```bash
cd frontend
npm run test:e2e:install   # once (Chromium)
npm run test:e2e
```

## Coverage (smoke)

- Anonymous storefront / catalog
- Customer login / logout / account protection
- **Authenticated checkout** → account orders list + detail
- Admin shell + module routes
- Limited user: Read vs Manage, 403 ≠ logout
- Cart add + quantity persistence on **E2E Product**
- Guest checkout place + confirmation
- Admin order fulfillment with inventory asserts (Confirm / OutForDelivery / Deliver)
- Confirm → Cancel with reservation release asserts
- Ordering settings update + checkout minimum + restore
- IAM role/override lifecycle on E2E accounts (cleanup)
- Cloudinary product media upload/reorder/delete (requires local Cloudinary secrets)

Catalog fixtures (when `LocalDevFixtures:Enabled`): `E2E Category`, `E2E Product` / `E2E-STD-001`, `E2E Shipping Zone`.

## Cleanup

No dedicated DB reset in MVP — fixtures are idempotent (`E2E-*` names). See `docs/DEVELOPMENT-GUIDE.md`.
