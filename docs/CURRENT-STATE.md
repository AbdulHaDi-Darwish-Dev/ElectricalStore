# Current state

> **Purpose:** Living operational status.  
> **Authority:** Verified status only — re-run tests before changing baselines.  
> **Update when:** After every non-trivial milestone.

## Status

| Item | Value |
|------|--------|
| Template origin | `dotnet new permixa-app` |
| Framework | .NET 8 + Permixa `0.1.0-preview.2` |
| Business features | **Categories** + **Product/Variant** + **Media** + **Inventory** + **Shipping** + **Checkout Preview** + **Orders** MVP + **Access Management** (thin `/admin/access` adapters over Permixa) |
| Permissions | App: `Categories.Manage`, `Products.Manage`, `Inventory.Read`, `Inventory.Adjust`, `Shipping.Manage`, `Orders.Read`, `Orders.Manage`, `Settings.Manage` (AppSeed → Owner). IAM Dashboard uses Permixa `Iam.*` (Users/Roles/Permissions/RolePermissions/UserRoles/UserPermissionOverrides/Audit) — do not duplicate as `Access.*` |
| Reference feature | SampleNotes **removed** (forward migration `DropSampleNotes`) |
| Authz cache | In-memory by default; Redis when generated with `--redis` |
| Email confirmation | Off by default; `RequireConfirmedEmail=true` with `--resend` |
| App migrations | … + `ProtectGuestIdempotencyToken` + **`DropSampleNotes`** (`__AppMigrationsHistory`, count **11**) — **no new business IAM tables** |
| Image provider | Cloudinary via `IImageStorage` (Infrastructure only); storage key is provider-neutral (`ImageStorageKey` / `StorageKey`) |
| Upload policy | Central `Media:MaxImageSizeMb` (default 5); JPEG/PNG/WebP; affects NEW uploads only |
| Windows Dev DB | `DESKTOP-30CDIBP\MSSQLSERVER22` / `ElectricalStore.Db` (Trusted Connection) |
| Dev setup | Idempotent `scripts/init-dev-secrets.ps1` (JWT/Owner secrets preserved on rerun) |
| Swagger JWT | Bearer security scheme + Authorize button (OpenAPI metadata only) |
| Swagger tags | `Authentication`, `Account`, `Health`, `Catalog - …`, `Shipping`, `Checkout`, `Orders`, `Back Office - Categories/Products/Inventory/Shipping/Orders/Settings/Access Management`; not API versions |
| Client JSON errors | `ClientRequestExceptionHandler` → HTTP 400 `InvalidRequest` (before Permixa 500 catch-all) |
| Money precision | Variant `Price` / DeliveryZone `Fee` / Order money = `decimal(18,2)` (SYP MVP); qty = `decimal(18,3)` |
| Inventory | Per `ProductVariant`; `OnHand`/`Reserved` persisted; `Available` derived; missing row = zero stock; SQL `rowversion` concurrency; admin adjust + audit |
| Shipping | Aleppo fixed-fee `DeliveryZone`; public active list for Checkout |
| Cart | **Frontend-only** (localStorage/state). Backend does **not** persist Carts/CartItems. No GuestCartToken. |
| Checkout | `POST /checkout/preview` — non-persisting; re-resolves catalog/price/stock/shipping; never trusts client money fields |
| Orders | Place Order persists Order+Items snapshots; **Pending does not reserve**; **Admin Confirm reserves atomically**; OutForDelivery dispatches; COD Unpaid/Paid separate from status |
| Minimum order | Persisted `OrderingSettings` (admin Back Office); snapshotted as `AppliedMinimumOrderAmount` |
| Place Order | Requires `Idempotency-Key`; guest replay via Data-Protection–protected payload (24h expiry); Order stores hash only |
| Access Management | `/admin/access/*` thin Api adapters → Permixa use cases only. RoleLevel: **lower int = higher authority** (bootstrap Owner = **1**). Precedence: UserDeny > UserAllow > Role > DefaultDeny |
| Data Protection | Keys persisted to `DataProtection:KeysPath` or `{BaseDirectory}/dp-keys` (required for guest idempotency Unprotect across restarts) |
| Operational logging | Built-in `ILogger` + JSON console (Production); request middleware; Order/Inventory Information logs; no bodies/secrets/PII |
| Postman contract | `docs/postman/ElectricalStore.postman_collection.json` + Local environment (frontend-ready) |
| CORS | Config-driven `Cors:AllowedOrigins` (Dev: `http://localhost:3000`); methods GET/POST/PUT/DELETE/OPTIONS; headers Authorization, Content-Type, Idempotency-Key, X-Order-Token; expose Retry-After; **no** AllowAnyOrigin / **no** AllowCredentials |
| Verified tests | **162/162** Release (Domain 48, Application 45, Infrastructure 17, Integration 52) |

## Next work (backend sequence)

1. ~~Inventory~~ **done**
2. ~~Shipping / Delivery Zones~~ **done**
3. ~~Persisted backend Cart~~ **rejected** — cart is frontend-only
4. ~~Checkout / Place Order~~ **done**
5. ~~Customer Orders~~ **done**
6. ~~Back Office Orders (admin confirm reserves inventory)~~ **done**
7. ~~Permixa Access Management readiness for Dashboard~~ **done**
8. ~~SampleNotes cleanup~~ **done** (`DropSampleNotes`)
9. ~~Operational logging~~ **done**
10. ~~CORS for Next.js Dev origin~~ **done** (`Cors:AllowedOrigins`)
11. Frontend foundation (Storefront + Back Office) against frozen API
12. ~~Frontend F1 foundation scaffold~~ **done** (`frontend/` — Arabic/RTL, brand-agnostic shells, API/Query foundation; no business features yet)
13. ~~Frontend F2 public API contract layer~~ **done** (catalog / shipping / checkout preview DTOs + modules; Vitest 4.1.11)

Then: Auth phase + catalog Storefront UI (F3).

## Access Management (Dashboard IAM)

- Permixa remains IAM source of truth (Identity users/roles, permissions, role grants, user overrides, effective resolution, RoleLevel, authn/authz). **No ElectricalStore IAM tables/entities.**
- Host surface: `/admin/access/*` under Swagger tag `Back Office - Access Management`, Bearer + `RequirePermission(Iam.*)`.
- RoleLevel controls **who can manage whom**; permissions control **what**. `CanManage` when `actorLevel < targetLevel` (lower int = higher authority). Bootstrap **Owner RoleLevel = 1**.
- Create role uses `ReferenceRoleId` + `RolePlacement` (`Above`/`Below`/`SameLevel`) — not free-form RoleLevel assignment.
- `GET /admin/access/roles` unions Permixa manageable roles with actor's own roles (`GetMyRoles`) so Owner appears as a create reference.
- `GET /admin/access/users` returns hierarchically manageable users only (Owner typically excluded; self IAM detail → `CannotManageSelf`).
- Effective permission `source` is composed from public `UserIamDetails` overrides + effective names + permission catalog (UserDeny / UserAllow / Role / DefaultDeny) — not a separate Permixa provenance DTO.
- IAM audit: `GET /admin/access/audit` → Permixa `GetIamAuditLogsUseCase` (`Iam.Audit.Read`).
- Deferred host work: admin user create/lock/disable/email/force-reset/session revoke HTTP adapters (Permixa use cases exist; not wired in this slice).


## Minimum order / settings

- Persisted singleton `OrderingSettings.MinimumMerchandiseSubtotal` (admin `GET/PUT /admin/settings/ordering`, permission `Settings.Manage`).
- Preview/Place use CURRENT setting; Place snapshots into `AppliedMinimumOrderAmount`; Pending modify keeps the snapshot.
- Appsettings `Ordering:*` is **not** authoritative.

## Place Order idempotency

- Required header `Idempotency-Key` (16–128 chars, client high-entropy).
- Unique DB row `(Scope, KeyHash)` where Scope is `u:{userId}` or `g`.
- Guest replay: `ProtectedGuestAccessToken` is ASP.NET Data Protection ciphertext (never raw). Order still stores **hash only**.
- Retention: `ExpiresAtUtc` = Created + 24h; expired rows are removed on next use of the same key. Bulk cleanup is deferred ops maintenance.
- Never log raw Idempotency-Key, raw GuestAccessToken, protected payload, or JWTs.

## MarkPaid

- Allowed only when status is `OutForDelivery` or `Delivered`. Paid is terminal. Delivered does not auto-pay.


- Admin may attach Products to an **inactive** Category (inactive = catalog visibility, not deletion).
- Public catalog requires:
  - Category: `IsActive` **and** has image
  - Product: Category publicly visible + `Product.IsActive` + ≥1 image + ≥1 active Variant
- Category has exactly 0..1 image (columns on Category). Product has 1..4 images for catalog readiness (0 allowed administratively).
- No Variant images. No generic media-management platform.
- Images managed via focused multipart endpoints (not Create Category/Product aggregates).
- No Product delete — deactivate. Variants are never physically deleted in this slice.
- SKU is globally unique (case-insensitive) via `NormalizedSku` unique index + application pre-check.
- Changing `Media:MaxImageSizeMb` does not modify existing Cloudinary assets.
- **Inventory** belongs to **ProductVariant** (not Product). Quantities are `decimal(18,3)`.
- Missing `InventoryItem` means OnHand=Reserved=Available=0; first admin adjustment creates the row. Catalog does not auto-create inventory.
- Zero stock does **not** hide catalog Products/Variants (availability ≠ visibility).
- Admin stock adjustment mutates **OnHand** only (never Reserved).
- **Shipping** MVP: Aleppo **fixed-fee Delivery Zones** only. Fee `decimal(18,2)`; zero fee allowed; names unique case-insensitively; no physical delete (Activate/Deactivate).
- Public `GET /shipping/zones` returns **active** zones only.
- **Cart is frontend-only** (localStorage/state). Backend has no `Carts`/`CartItems` tables and no guest cart token. Frontend cart lines are conceptually `{ variantId, quantity }` only.
- Backend **never trusts** frontend-supplied price, line totals, subtotal, stock, shipping fee, or order total.
- **Checkout Preview** is non-persisting and does **not** reserve stock or create Orders.
- **Place Order** revalidates everything and persists Order + OrderItem **snapshots** in one SaveChanges. Does **not** modify `Inventory.Reserved`. Multiple Pending orders may oversubscribe Available (approved).
- **Admin Confirm** (`POST /admin/orders/{id}/confirm`) is the critical all-or-nothing reservation: validate Available for all lines → Reserve all → Confirmed → single SaveChanges; Inventory `rowversion` + Order `rowversion` for races.
- **OutForDelivery** dispatches: `OnHand -= qty`, `Reserved -= qty` atomically with status transition.
- **Delivered** does not change inventory or auto-mark Paid.
- **COD**: PaymentMethod=CashOnDelivery, PaymentStatus Unpaid→Paid via explicit `mark-paid` only.
- **Failed delivery** after OutForDelivery: no automatic stock return (deferred return-to-store workflow).
- Customer may cancel/modify **only** while `PendingConfirmation`. Admin may cancel Pending/Confirmed/Preparing (releases reservation when Confirmed/Preparing). No cancel after OutForDelivery in MVP.
- Pending modification **reprices** with CURRENT catalog prices; validates against **original** `AppliedMinimumOrderAmount`.
- Guest: secure token once; hash stored; token grants access only to that Order; do not log raw token or tracking URLs.
