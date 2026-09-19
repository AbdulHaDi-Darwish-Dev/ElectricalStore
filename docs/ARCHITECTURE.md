# Architecture

> **Purpose:** Layering, dependencies, and Permixa boundary.  
> **Authority:** Structural decisions for this application.  
> **Update when:** Project graph, persistence strategy, or host integration shape changes.

## Layers

```text
ElectricalStore.Domain
        ↑
ElectricalStore.Application
        ↑
ElectricalStore.Infrastructure
        ↑
ElectricalStore.Api  ── PackageReference → Permixa.AspNetCore (+ optional providers)
```

## Persistence

| Context | Owns | Migrations |
|---------|------|------------|
| Permixa `ApplicationDbContext` | IAM / Identity | Inside Permixa NuGet packages |
| `AppDbContext` | Business data (Categories + Products/ProductVariants/ProductImages + InventoryItems/InventoryAdjustments + DeliveryZones + Orders/OrderItems/OrderModificationAudits + OrderingSettings + OrderPlacementIdempotencies) | `Infrastructure/Persistence/Migrations` (`__AppMigrationsHistory`) |

Same SQL Server database and connection string by default.

Product price and DeliveryZone fee use SQL `decimal(18,2)` (SYP MVP monetary values). Variant quantity increment and inventory quantities use `decimal(18,3)`.

**Shipping:** Aleppo MVP uses fixed-fee `DeliveryZone` rows (`Name`, `NormalizedName` unique, `Fee`, `IsActive`). No distance/weight/carrier engine. Public list returns active zones ordered by Name. Place Order snapshots zone name + fee onto the Order.

**Cart / Checkout / Orders:** Cart is **frontend-only**. `POST /checkout/preview` is non-persisting. Minimum merchandise subtotal is persisted singleton `OrderingSettings` (`Settings.Manage`). `POST /orders` requires `Idempotency-Key` (scoped unique DB row; guest replay uses ASP.NET Data Protection–protected token payload with 24h `ExpiresAtUtc`; Order stores guest token hash only). Place Order does not reserve. Admin Confirm reserves atomically. MarkPaid only when OutForDelivery or Delivered.

**Inventory:** One optional `InventoryItem` per `ProductVariant` (`ProductVariantId` PK/FK, Restrict delete). Persists `OnHand` and `Reserved`; `Available` is derived. Missing row means zero stock. SQL Server `rowversion` on `InventoryItem`. Admin `AdjustOnHand` writes an `InventoryAdjustment` audit row in the same `SaveChanges`. Domain methods `Reserve` / `Release` / `Dispatch` are used by Order Confirm / Cancel / OutForDelivery inside one `AppDbContext` / UnitOfWork transaction. Do not call `SaveChanges` inside repositories.

**Media:** Cloudinary is the MVP image provider. Domain/Application depend only on `IImageStorage` + `StoredImage` (provider-neutral `StorageKey` / `Url`). Category stores optional `ImageStorageKey`/`ImageUrl` columns (exactly one image). Product owns `ProductImage` rows with `StorageKey` (max 4; exactly one primary when any exist). Cloudinary `public_id` maps to `StorageKey` only inside `CloudinaryImageStorage`. Upload then DB persist; on DB failure after upload, compensate by deleting the new storage asset. Delete/replace: persist DB first for deletes, then best-effort provider delete (orphans possible; no saga).

Expected business/application failures for Store features use a small app-owned `Result`/`Error` model in Application, mapped to ProblemDetails in Api. Do not add a Permixa.Application dependency solely to reuse Permixa's Result type.

## Operational logging

- **Domain** has no logging dependencies.
- **Application** may use `ILogger<T>` (`Microsoft.Extensions.Logging.Abstractions`) for safe operational events (Order lifecycle, inventory adjust). Never log PII, tokens, or secrets.
- **Api** owns request completion logging (`RequestLoggingMiddleware`) and console formatter configuration (`appsettings` / `appsettings.Production.json` JSON console).
- **Infrastructure** logs Cloudinary provider failures (status/operation only).
- Correlation: ProblemDetails `traceId` + structured `TraceId` fields use `Activity.Current` / `HttpContext.TraceIdentifier`.
- Durable **audit** remains separate: `InventoryAdjustment`, `OrderModificationAudit`, Permixa IAM audit — logs do not replace them.

## Composition root

`ElectricalStore.Api/Program.cs` is the composition root: `AddAppInfrastructure` → `AddPermixaHost` → `AddApiServices`, then `InitializeDevelopmentAsync`, middleware, and `MapApiEndpoints`. Host-local helpers live under `Api/DependencyInjection` and `Api/Endpoints` — they compose existing Permixa NuGet APIs; they are not framework APIs.

## Swagger / OpenAPI tags

Swagger uses **feature-oriented tags** for discoverability. Tags organize documentation only and **do not** represent API versions.

**Customer / public-facing (when present):**

- `Catalog - Categories`
- `Catalog - Products`
- `Shipping`
- `Checkout`
- `Orders`

**Back office:**

- `Back Office - Categories`
- `Back Office - Products`
- `Back Office - Inventory`
- `Back Office - Orders`
- `Back Office - Shipping`
- `Back Office - Settings`
- `Back Office - Access Management`
- `Authentication`
- `Account`
- `Health`

## Access Management / IAM boundary

Permixa owns IAM persistence and behavior (`ApplicationDbContext`). ElectricalStore exposes thin `/admin/access` Minimal API adapters that delegate to Permixa Application use cases (`GetUsers`, roles CRUD/placement, role permissions, user roles, overrides, effective permissions, IAM audit). Do **not** add User/Role/Permission entities to `AppDbContext`.

**RoleLevel vs permissions:** RoleLevel answers who may administer whom (`CanManage` when actor level is strictly less than target — lower int = higher authority; Owner bootstrap level is **1**). Permissions answer what actions are allowed. Precedence for effective allow: **User Deny > User Allow > Role grant > Default Deny**.

Create/move roles via `ReferenceRoleId` + `RolePlacement` (Above/Below/SameLevel). Owner role is protected (`OwnerProtected`). Self hierarchy admin operations return `CannotManageSelf`.

**Cart / Checkout / Orders:** Cart is **frontend-only**. `POST /checkout/preview` is non-persisting. Minimum merchandise subtotal is persisted singleton `OrderingSettings` (`Settings.Manage`). `POST /orders` requires `Idempotency-Key` (scoped unique DB row; guest replay uses ASP.NET Data Protection–protected token payload with 24h `ExpiresAtUtc`; Order stores guest token hash only). **Data Protection keys must be persisted** (`DataProtection:KeysPath` or `{BaseDirectory}/dp-keys`) so guest idempotency Unprotect survives process restarts. Place Order does not reserve. Admin Confirm reserves atomically. MarkPaid only when OutForDelivery or Delivered. Admin order detail includes `modificationAudits` when present.

Apply `.WithTags(...)` on `MapGroup` when the whole group shares a category. Prefer concise `.WithSummary` / stable `.WithName` on endpoints.

Do **not** introduce API versioning (`/api/v1`, Asp.Versioning, multiple Swagger docs per version) until a real backward-compatibility requirement exists.
