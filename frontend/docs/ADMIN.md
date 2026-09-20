# Admin foundation (F7+)

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

## Query conventions

See `src/features/admin/query-conventions.ts`:

- Keys: `["admin", domain, ...]`
- Feature-owned API modules (no giant `adminApi.ts`)
- Short freshness for Inventory/Orders later; do not change global QueryClient solely for Admin
- Never persist Admin caches to localStorage
- `403` → Access Denied UX; do not refresh-loop

## Categories (F7.1) — reference CRUD pattern

Feature-owned under `src/features/admin-categories/` + `src/components/admin-categories/`.

- Permission: `Categories.Manage`
- Routes: `/admin/categories`, `/admin/categories/new`, `/admin/categories/[id]`
- Query keys: `adminCategoryKeys` (`["admin","categories",...]`)
- Mutations: create / update / activate / deactivate / image upsert / image delete
- **No category hard-delete** — backend has activate/deactivate only
- Image: separate `PUT|DELETE /admin/categories/{id}/image` (multipart `file`)
- After success: invalidate `adminCategoryKeys.all()` (list + details)
- Public catalog ISR may lag Admin changes by up to **60 seconds** (existing revalidate) — no cache-tag busting in F7.1

## Phase boundaries

- F7: shell, nav, gates, dashboard shortcuts
- F7.1: Admin Categories management (reference pattern)
- Later: Product / Inventory / Orders / Shipping / Settings / IAM CRUD

## Production gate

Local Admin uses F6 auth. Production Admin go-live remains blocked until trusted ingress + ForwardedHeaders Production trust are configured (`docs/TRUSTED-CLIENT-IP.md`).
