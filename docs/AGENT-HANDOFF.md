# Agent handoff

> **Purpose:** Concise entry point for AI agents and new developers.  
> **Authority:** Operational orientation — link out for depth.  
> **Update when:** Status, next work, or critical invariants change.

## Read first

1. This file  
2. [ARCHITECTURE.md](ARCHITECTURE.md)  
3. [SECURITY.md](SECURITY.md)  
4. Root [AGENTS.md](../AGENTS.md)  
5. [DEVELOPMENT-GUIDE.md](DEVELOPMENT-GUIDE.md)

## Identity

Consumer ASP.NET Core app using Permixa NuGet IAM. Clean Architecture host.

## Critical invariants

- Two DbContexts; do not merge business entities into Permixa context
- Development-only auto-migrate
- No committed secrets
- SampleNotes reference feature **removed** (forward `DropSampleNotes` migration)
- Category names are unique case-insensitively (`NormalizedName` unique index)
- Product variants have globally unique SKUs case-insensitively (`NormalizedSku` unique index); Product is the aggregate root and always has ≥1 variant
- Admin Category APIs require `Categories.Manage`; Admin Product APIs require `Products.Manage`; Admin Inventory read requires `Inventory.Read`, adjust requires `Inventory.Adjust`; Admin Shipping requires `Shipping.Manage`; Admin Order reads require `Orders.Read`, mutations require `Orders.Manage`; Ordering settings require `Settings.Manage`
- Access Management Dashboard uses Permixa `Iam.*` permissions only (not role names, not host `Access.*` duplicates). Thin `/admin/access` adapters; no host IAM tables
- RoleLevel: lower int = higher authority (Owner bootstrap = 1). Precedence: UserDeny > UserAllow > Role > DefaultDeny. Do not copy effective-permission logic into ElectricalStore
- Guest Place Order idempotency requires **persisted** ASP.NET Data Protection keys (`DataProtection:KeysPath` or `dp-keys/`); ephemeral keys break Unprotect after restart
- Operational logging: structured console (JSON in Production); request middleware; Order/Inventory Information logs; no Domain logging; never log secrets/tokens/PII
- Place Order requires high-entropy `Idempotency-Key`; guest idempotency stores Data-Protection–protected token (24h); Order stores hash only; Pending does not reserve; Confirm reserves; MarkPaid only OutForDelivery/Delivered
- Minimum order is persisted `OrderingSettings` (not appsettings)
- Public catalog exposes **catalog-ready** data only: Category active+image; Product active with ≥1 image, ≥1 active variant, and a publicly visible Category
- Inventory is per **ProductVariant**; missing inventory row = zero stock; zero stock does not hide catalog items; `Available = OnHand - Reserved` (derived); admin adjust does not mutate Reserved
- InventoryItem and Order use SQL Server `rowversion` optimistic concurrency (409 on conflict); repositories do not call `SaveChanges` — UnitOfWork coordinates Order+Inventory transactions
- Domain inventory primitives `Reserve` / `Release` / `Dispatch` are used by Order Confirm / Cancel / OutForDelivery — **not** at Place Order
- Shipping MVP is Aleppo fixed-fee `DeliveryZone`; Orders snapshot zone name + fee
- **Cart is frontend-only** — no backend Cart persistence; Checkout/Orders revalidate and persist Orders only
- Place Order does **not** reserve stock; Admin Confirm reserves atomically; OutForDelivery dispatches OnHand+Reserved
- Guest order token: store hash only; raw token once; `X-Order-Token` for guest access; never log raw token
- CORS: `Cors:AllowedOrigins` config only (Dev includes `http://localhost:3100`); no AllowAnyOrigin; no AllowCredentials; expose `Retry-After`
- Cloudinary lives only in Infrastructure behind `IImageStorage`; Domain/Application use provider-neutral `StorageKey`
- `Media:MaxImageSizeMb` (default 5) + allowed content types are central upload validation only
- No Variant images; no generic media platform in MVP
- Expected Category/Product/Media/Inventory/Shipping/Ordering failures use app-owned `Result`/`Error` → ProblemDetails
- Malformed JSON / Minimal API body binding failures (`BadHttpRequestException`) must return HTTP 400 via `ClientRequestExceptionHandler`
- Swagger tags are feature-oriented (`Catalog - …` / `Shipping` / `Checkout` / `Orders` / `Back Office - …`); not API versions
- Windows daily Dev DB is local MSSQL named instance + Trusted Connection (not Docker SQL)
- `init-dev-secrets.ps1` must stay idempotent (no silent JWT/Owner password rotation)

## Current / next

See [CURRENT-STATE.md](CURRENT-STATE.md). Frontend F1–F7.2 done locally; F7.3 Admin Inventory in review. **Production Auth/Admin go-live remains BLOCKED** until trusted ingress + KnownProxies/Networks (`docs/TRUSTED-CLIENT-IP.md`). Admin UX uses effective `permissions[]` only — never role names.
