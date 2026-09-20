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
- Inventory adjustments live under **F7.3** (not inside product forms)
- Public catalog ISR may lag Admin changes by up to **60 seconds**

## Inventory (F7.3)

Feature-owned under `src/features/admin-inventory/` + `src/components/admin-inventory/`.

### Stock semantics (per ProductVariant)

| Field | Arabic | Notes |
|-------|--------|--------|
| OnHand | المخزون الفعلي | Admin-adjustable via **delta** (`quantityDelta`) |
| Reserved | المحجوز | Order lifecycle only — **read-only** in Admin Inventory |
| Available | المتاح للبيع | Derived `OnHand − Reserved` — **read-only**; server DTO is source of truth |

### Permissions

- `Inventory.Read` — list / get (page gate)
- `Inventory.Adjust` — `POST …/adjust` only (does **not** imply Read on the API)
- Do **not** use `Products.Manage` for inventory authorization
- Read-only users see stock; adjust actions are **hidden** (not merely disabled)

### Adjustment ownership

- Request: `{ quantityDelta, reason }` — **add/remove**, not absolute OnHand set
- Reason required (max 500)
- OnHand may be 0; cannot go negative; cannot fall below Reserved
- Optimistic concurrency via SQL `rowversion` → `Inventory.ConcurrencyConflict` (409); refetch and retry
- Adjustment history endpoint exists (`GET …/adjustments`) but **no Admin history UI** in F7.3

### Public / Checkout interaction

- Backend inventory is authoritative immediately after adjust
- Public catalog stock UI may lag up to **~60s ISR**
- Checkout Preview and Place Order always revalidate against the backend

## Shipping (F7.4)

Feature-owned under `src/features/admin-shipping/` + `src/components/admin-shipping/`.

### Zone model

- Named fixed-fee delivery zones (Aleppo MVP) — **no maps/geo**
- Fields: `name`, `fee` (SYP, decimal scale 2), `isActive`
- **Fee = 0** is allowed (free shipping for that zone)
- **Minimum merchandise subtotal** is **global** `OrderingSettings` (`Settings.Manage`) — **not** per zone (deferred to Settings)

### Lifecycle

- Create: `{ name, fee, isActive }`
- Update: `{ name, fee }` only
- Activate / deactivate: separate POSTs (no hard delete)
- Inactive zones omitted from public `GET /shipping/zones`

### Permission

- `Shipping.Manage` for all admin shipping routes

### Public / Checkout

- Public zone list uses `revalidate: 60` (~60s lag possible)
- Checkout Preview / Place Order remain backend-authoritative for fee + totals
- Historical orders keep snapshotted zone name + fee

## Orders (F7.5)

Feature-owned under `src/features/admin-orders/` + `src/components/admin-orders/`.

### Permissions

- `Orders.Read` — list + detail (page gate)
- `Orders.Manage` — confirm / prepare / out-for-delivery / deliver / mark-paid / cancel
- Manage does **not** imply Read on the API

### State machine (explicit actions — no free status dropdown)

```
PendingConfirmation → confirm → Confirmed (reserves inventory)
PendingConfirmation → cancel → Cancelled
Confirmed → prepare → Preparing
Confirmed → cancel → Cancelled (releases reservation)
Preparing → out-for-delivery → OutForDelivery (dispatch OnHand+Reserved)
Preparing → cancel → Cancelled (releases reservation)
OutForDelivery → deliver → Delivered (no inventory mutation)
OutForDelivery|Delivered + Unpaid → mark-paid → Paid (COD; not reversible in UI)
Delivered / Cancelled — terminal for fulfillment
```

### Payment

- COD only (`CashOnDelivery`)
- `Unpaid` / `Paid`; mark-paid only when OutForDelivery or Delivered
- Deliver does **not** auto-mark Paid

### Filters

- `status`, `paymentStatus`, `search` (order number / name / phone), optional date range params in API
- **No server pagination** — list returns all matching rows

### Inventory invalidation

Successful confirm / out-for-delivery / cancel invalidate Admin Inventory query cache in the same session.

### Deferred

- Staff `PUT …/items` (PendingConfirmation reprice) — endpoint exists; not in F7.5 UI
- Invoice/print/notifications — not in contract

## Settings (F7.6)

Feature-owned under `src/features/admin-settings/` + `src/components/admin-settings/`.

### Ownership

Only business settings exposed by backend:

| Group | Endpoint | Fields |
|-------|----------|--------|
| Ordering | `GET/PUT /admin/settings/ordering` | `minimumMerchandiseSubtotal` |

- Permission: `Settings.Manage` (read + update — no separate Settings.Read)
- DB singleton; updates take effect immediately on next Checkout Preview / Place Order
- **Zero = no minimum** merchandise subtotal (excludes shipping)
- Negative rejected → `Ordering.InvalidMinimumOrderAmount`
- No rowversion concurrency (last-write-wins)
- Not shipping fees, not env/secrets, not branding CMS

### Arabic field

- الحد الأدنى لقيمة المنتجات — مجموع المنتجات قبل الشحن

## Phase boundaries

- F7: shell, nav, gates
- F7.1: Categories
- F7.2: Products (+ variants + media)
- F7.3: Inventory (list + delta adjust)
- F7.4: Shipping zones
- F7.5: Admin Orders lifecycle
- F7.6: Settings (OrderingSettings)
- Later: IAM

## Production gate

Local Admin uses F6 auth. Production Admin go-live remains blocked until trusted ingress + ForwardedHeaders Production trust are configured (`docs/TRUSTED-CLIENT-IP.md`).
