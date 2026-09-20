# Admin foundation (F7)

> Permission-aware Admin shell. Backend remains the authorization authority.

## Access model

- Auth from F6 (`useAuthStore`, `/me` → `permissions[]`).
- No role-name checks (`Admin` / `Owner`).
- Shell eligibility: any code in `ADMIN_SHELL_PERMISSION_CODES` (app + IAM).
- There is **no** invented `Admin.Access` permission.

## Permission catalog

See `src/features/admin/permission-catalog.ts` — codes must match ASP.NET / Permixa.

## Navigation

`src/features/admin/navigation.ts` — items are **omitted** when unauthorized (not merely disabled).

## Query conventions (future features)

See `src/features/admin/query-conventions.ts`:

- Keys: `["admin", domain, ...]`
- Feature-owned API modules (no giant `adminApi.ts`)
- Short freshness for Inventory/Orders later; do not change global QueryClient solely for Admin
- Never persist Admin caches to localStorage
- `403` → Access Denied UX; do not refresh-loop

## Phase boundaries

F7: shell, nav, gates, dashboard shortcuts, placeholders only.

Later: Category/Product/Inventory/Orders/Shipping/Settings/IAM CRUD.

## Production gate

Local Admin uses F6 auth. Production Admin go-live remains blocked until trusted ingress + ForwardedHeaders Production trust are configured (`docs/TRUSTED-CLIENT-IP.md`).
