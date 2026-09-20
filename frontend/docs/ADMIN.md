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
- Never persist Admin caches to localStorage
- `403` → Access Denied UX; do not refresh-loop

## Categories (F7.1) — reference CRUD pattern

Feature-owned under `src/features/admin-categories/` + `src/components/admin-categories/`.

- Permission: `Categories.Manage`
- No category hard-delete — activate/deactivate only
- Public catalog ISR may lag Admin changes by up to **60 seconds**

## Products (F7.2)

Feature-owned under `src/features/admin-products/` + `src/components/admin-products/`.

- Permission: `Products.Manage` (not Inventory)
- Create **requires ≥1 variant** in the same POST (`CreateProductRequest.Variants`)
- After create → manage screen for additional variants + images
- Variants: add / update / activate / deactivate (no variant delete)
- Media: POST images (max 4), DELETE, set primary, reorder (up/down)
- Update product basics: name/description/categoryId only — lifecycle via activate/deactivate
- Readiness checklist is informational (active + category active + image + active variant)
- **Inventory adjustments are deferred to F7.3**
- Public catalog ISR may lag Admin changes by up to **60 seconds**

## Phase boundaries

- F7: shell, nav, gates
- F7.1: Categories
- F7.2: Products (+ variants + media)
- Later: Inventory / Orders / Shipping / Settings / IAM

## Production gate

Local Admin uses F6 auth. Production Admin go-live remains blocked until trusted ingress + ForwardedHeaders Production trust are configured (`docs/TRUSTED-CLIENT-IP.md`).
