# Security

> **Purpose:** Host-app security expectations and secret handling.  
> **Authority:** This application's security posture (Permixa has its own model).  
> **Update when:** Auth/config/threat-model changes.

## Host responsibilities

- Protect connection strings, JWT PEMs, bootstrap passwords, Resend API keys
- Disable bootstrap after first successful Owner setup and remove OwnerPassword
- Production must not auto-migrate by default (Development-only migrate in template)
- Configure forwarded headers / TLS for your deployment — see [TRUSTED-CLIENT-IP.md](TRUSTED-CLIENT-IP.md)

## Defaults in this template

- Bootstrap `Enabled=false` in committed `appsettings.json`
- No PEM files or Owner passwords committed
- Development JWT PEMs and Owner password live in .NET User Secrets only
- Windows Development DB uses Trusted Connection (no SQL password in source)
- Login endpoint uses a sample Permixa rate-limit policy (RemoteIp). Host ForwardedHeaders is **fail-safe off** in Production until `ForwardedHeaders:Enabled` + KnownProxies/Networks are set; Development trusts loopback only. Details: [TRUSTED-CLIENT-IP.md](TRUSTED-CLIENT-IP.md)
- **Auth deployment gate:** Local Auth application code may run against Development loopback trust. **Production Auth go-live is BLOCKED** until trusted ingress sanitizes client IP and Production KnownProxies/Networks are set. Do not enable trust-all ForwardedHeaders.
- Admin Category APIs require permission `Categories.Manage`
- Admin Product APIs require permission `Products.Manage`
- Admin Inventory list/get/adjustments require `Inventory.Read`; stock adjust requires `Inventory.Adjust`
- Admin Shipping (Delivery Zone) APIs require `Shipping.Manage`
- Admin Order list/detail require `Orders.Read`; confirm/prepare/dispatch/deliver/cancel/mark-paid/modify require `Orders.Manage`
- Ordering settings require `Settings.Manage`
- Access Management (`/admin/access/*`) requires Permixa `Iam.*` permissions (Users/Roles/Permissions/RolePermissions/UserRoles/UserPermissionOverrides/Audit). Authorize by permission name, never by role name
- RoleLevel hierarchy is enforced by Permixa use cases (lower int = higher authority; Owner protected; no self hierarchy admin; no lower→higher escalation)
- User IAM DTOs must never expose password hashes, security stamps, refresh tokens, MFA/OTP secrets, or JWTs
- Place Order requires `Idempotency-Key`; guest idempotency stores Data-Protection–protected token only (not plaintext); Order stores hash only; 24h expiry on idempotency rows
- **Production:** persist ASP.NET Data Protection keys (`DataProtection:KeysPath` shared volume, or provider equivalent). Ephemeral keys make guest idempotency Unprotect fail after restart (`Ordering.IdempotencyReplayUnavailable`)
- Operational logs: never log Authorization, JWT/refresh tokens, GuestAccessToken, X-Order-Token, Idempotency-Key, protected ciphertext, passwords, Cloudinary ApiSecret, JWT PEMs, connection-string passwords, or customer phone/name/address; use OrderId/UserId/VariantId instead
- Request logging records method, route template, status, elapsed ms, TraceId only (no bodies/headers)
- Never log raw Idempotency-Key, GuestAccessToken, protected guest payload, or JWTs
- MarkPaid allowed only for OutForDelivery/Delivered COD orders
- Authorize by permission name, never by role name
- Public Category/Product catalog (catalog-ready only), active shipping-zone list, Checkout Preview, guest Place Order, and guest track (`X-Order-Token`) are anonymous where designed
- Guest order access: store **hash only**; return raw token **once**; never log raw token or tracking URLs; invalid token must not enumerate Orders (prefer 404)
- Cloudinary credentials (`Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret`) must live in User Secrets / environment secrets — never commit real values; never log API secrets or signed sensitive values
- After first Owner login: disable bootstrap and remove `Permixa:Bootstrap:OwnerPassword` from User Secrets
- EF Core `EnableSensitiveDataLogging` is forced **off** for `AppDbContext`

## Reporting

Define your vulnerability reporting process for this application. For Permixa framework issues, follow Permixa's `SECURITY.md` upstream.
