# Deferred scope

> **Purpose:** Explicit non-goals / later work.  
> **Authority:** What we are intentionally not doing yet.  
> **Update when:** Scope decisions change.

## Deferred

- Deployment / CD workflows
- Cursor-specific `.cursor/rules` (use `AGENTS.md`)
- Persisted backend Cart / GuestCartToken / Carts+CartItems tables (Cart is **frontend-only**; Checkout/Orders persist Orders only)
- Automated background cleanup of expired `OrderPlacementIdempotencies` (ExpiresAtUtc exists; purge is ops/deferred)
- Payment gateway / online payments (MVP is Cash on Delivery only; `PaymentStatus` separate from `OrderStatus`)
- Failed-delivery **return-to-store** inventory workflow (after OutForDelivery, do not auto-increment stock)
- Customer saved-address book / Customer Address aggregate
- Redis / distributed locks for Order confirmation (use SQL `rowversion` only)
- Event bus / saga / microservices for Ordering
- Inventory multi-warehouse, stock transfers, reservation HTTP APIs beyond Order lifecycle
- Distance/weight-based shipping, external carriers, free-shipping rule engines
- Category **Delete**, hierarchy/parent-child, SEO/slug, drag-and-drop ordering, Category pagination
- Product discounts, tax, multi-currency infrastructure, brand, SEO/slug, dynamic attributes, Product delete, pagination framework
- **Variant images**; rich media library / DAM / generic media-management platform; image transformations/CDN policy UI; malware scanning; automatic recompression of existing assets when `MaxImageSizeMb` changes
- Unified Permixa `AddPermixa()` facade (framework-side)
- MFA HTTP productization (when `--resend`); admin force-password-reset / session revoke adapters remain deferred (customer self-service forgot/reset **and** change-email are done)
- Multi-tenancy
- API versioning (do not introduce until a real backward-compatibility requirement exists)
- Access Management HTTP adapters not wired in this freeze (Permixa use cases exist): admin user create/lock/disable/enable, admin email change, force password reset, session list/revoke; permission catalog create/update-description beyond seed
- Dedicated Permixa effective-permission **provenance** DTO (host composes source from public overrides + effective names today)
- Host-side security audit subsystem (use Permixa `Iam.Audit.Read` / `GetIamAuditLogs` instead)
- Azure Blob / certificate-based Data Protection key ring for multi-instance production (file-system keys are the MVP default)
