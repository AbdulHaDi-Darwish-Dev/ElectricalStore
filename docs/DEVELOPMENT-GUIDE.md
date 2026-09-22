# Development guide

> **Purpose:** Local workflows for developers.  
> **Authority:** How to run and extend this app day-to-day.  
> **Update when:** Secrets, Compose, EF, or auth flows change.

## Windows local development (preferred on Abdulhadi's machine)

Uses the installed SQL Server named instance with **Windows Authentication**:

`Server=DESKTOP-30CDIBP\MSSQLSERVER22;Database=ElectricalStore.Db;Trusted_Connection=True;TrustServerCertificate=True;`

### First-time setup

```powershell
cd E:\ElectricalStore
.\scripts\init-dev-secrets.ps1
dotnet run --project src/ElectricalStore.Api
```

The PowerShell script is **idempotent**: it preserves existing JWT keys and Owner password unless you pass `-RotateJwt` or `-RotateOwnerPassword`.  
If it generates a new Owner password, it prints that password **once** — store it locally; it is never committed.

### Normal daily start / restart

```powershell
cd E:\ElectricalStore
.\dev.ps1
```

Opens API + frontend in separate PowerShell windows on **dedicated** ports (**5180** / **3100**). Re-running `.\dev.ps1` stops previous launcher-owned shells first, then starts fresh. Uses `npm.cmd` for the frontend (avoids blocked `npm.ps1` under ExecutionPolicy). Next is pinned to port **3100** (no silent fallback to 3101).

Official local URLs:

- Frontend: http://localhost:3100  
- Admin: http://localhost:3100/admin  
- Backend: http://localhost:5180  
- Health: http://localhost:5180/health  
- Swagger: http://localhost:5180/swagger  

**One backend owner at a time:** `.\dev.ps1` **or** Visual Studio — not both. Both use port 5180.

Frontend env: copy `frontend/.env.example` → `frontend/.env.local` (gitignored) if you need overrides. Defaults in code already match 5180/3100.

### Local E2E fixtures

Playwright tests use dedicated Development accounts and a gitignored env file. **Do not commit passwords.**

1. Start the stack: `.\dev.ps1` and wait until `GET http://localhost:5180/health` succeeds.
2. Prepare credentials (writes user-secrets + `frontend/.env.e2e.local`):

   ```powershell
   .\scripts\prepare-local-e2e.ps1
   ```

   Then **restart** the API so `LocalDevAccountFixtureSeeder` creates users/roles:

   ```powershell
   .\stop-dev.ps1
   .\dev.ps1
   ```

3. Run E2E (Playwright loads `.env.e2e.local` automatically):

   ```powershell
   cd frontend
   npm run test:e2e:install   # once
   npm run test:e2e
   ```

4. **LocalDevFixtures (Development only):**
   - `LocalDevFixtures:Enabled=true` in `appsettings.Development.json`
   - Catalog seed: `E2E Category`, `E2E Product` (SKU `E2E-STD-001`), `E2E Shipping Zone`
   - Account seed (after prepare script sets passwords in user-secrets): Customer / `E2E-Admin` / `E2E-Limited` (Orders.Read + Inventory.Read only)

5. **Never** enable `LocalDevFixtures` in Production (`appsettings.json` keeps `Enabled: false`).

6. **Local media (optional but needed for real upload E2E):** set Cloudinary user-secrets (`Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret`). Without Cloudinary, Development may use `Media:AllowLocalDevStorage` → in-memory FakeImageStorage (never Production). Production never falls back to FakeImageStorage.

7. **Login rate limit:** normal policy is 20/min. Widened to 200/min only when **both** Development **and** `LocalDevFixtures:Enabled` are true.

See also [`frontend/e2e/README.md`](../frontend/e2e/README.md).

Stop launcher-tracked processes only:

```powershell
.\stop-dev.ps1
```

If ports are busy after stop and Visual Studio (or another tool) owns them, stop that session manually — the scripts will not kill unrelated processes.

API alone (without the storefront):

```powershell
dotnet run --project src/ElectricalStore.Api --launch-profile http
```

Swagger: http://localhost:5180/swagger

## Postman / Frontend API Contract

Executable collection for Storefront + Back Office integration (frozen backend surface):

| File | Path |
|------|------|
| Collection | [`docs/postman/ElectricalStore.postman_collection.json`](postman/ElectricalStore.postman_collection.json) |
| Environment | [`docs/postman/ElectricalStore.Local.postman_environment.json`](postman/ElectricalStore.Local.postman_environment.json) |

1. Import both into Postman.
2. Select **ElectricalStore Local**; set `baseUrl` (default `http://localhost:5180`).
3. Set `ownerEmail` / `ownerPassword` from your local user-secrets / setup output — leave empty in git; never commit real values.
4. **Login** stores `accessToken`, `refreshToken`, and `userId` into the environment (scripts never `console.log` tokens).
5. Authenticated requests use collection Bearer `{{accessToken}}`. Public catalog/checkout/guest requests are explicitly unauthenticated.
6. **`idempotencyKey`:** Place Order initializes it only when empty. Reuse the same value when retrying the same checkout attempt; clear or replace it before placing a *new* intended order.
7. **`guestOrderToken`:** filled from Place Order’s one-time `guestAccessToken`. Guest track/modify/cancel send `X-Order-Token: {{guestOrderToken}}`. Never paste real tokens into the committed collection.
8. Media uploads: multipart field `file` — select a local JPEG/PNG/WebP in Postman (max `Media:MaxImageSizeMb`, default 5).

Swagger groups store endpoints with feature-oriented tags (for example `Catalog - Categories`, `Back Office - Categories`). Tags are documentation only — not API versions. Prefer `.WithTags` on route groups. Do not introduce API versioning until a real backward-compatibility requirement exists.

### After first successful Owner bootstrap

```powershell
dotnet user-secrets set "Permixa:Bootstrap:Enabled" "false" --project src/ElectricalStore.Api
dotnet user-secrets remove "Permixa:Bootstrap:OwnerPassword" --project src/ElectricalStore.Api
```

Check bootstrap flag:

```powershell
dotnet user-secrets list --project src/ElectricalStore.Api
```

(Do not paste secret values into chat/docs.)

Development startup still migrates Permixa IAM + `AppDbContext` and seeds app permissions when `Permixa:AppSeed:Enabled` is true. Production never auto-migrates/seeds from this helper.

## Cloudinary (Media)

Image uploads require Cloudinary configuration. Committed `appsettings.json` has empty placeholders only.

Set via User Secrets (Development) or environment variables (Production), for example:

```powershell
dotnet user-secrets set "Cloudinary:CloudName" "<your-cloud-name>" --project src/ElectricalStore.Api
dotnet user-secrets set "Cloudinary:ApiKey" "<your-api-key>" --project src/ElectricalStore.Api
dotnet user-secrets set "Cloudinary:ApiSecret" "<your-api-secret>" --project src/ElectricalStore.Api
```

Optional media policy (defaults shown):

```json
"Media": {
  "MaxImageSizeMb": 5,
  "AllowedContentTypes": [ "image/jpeg", "image/png", "image/webp" ]
}
```

Changing `MaxImageSizeMb` affects **new uploads only** — existing Cloudinary assets are not resized or deleted.

If Cloudinary is not configured, the API starts but image upload/delete operations return `Media.NotConfigured` (HTTP 503). Automated tests inject `FakeImageStorage` and do not call the real Cloudinary service.

## Automated tests

Integration and Infrastructure tests continue to use **Testcontainers** (ephemeral SQL). They do **not** require the local named instance or Docker Compose.

## Docker (optional)

Docker Compose SQL remains available for container-based workflows and is **not** the preferred Windows daily path on this machine:

```powershell
cp .env.example .env   # if needed
docker compose up -d
```

Linux/macOS setup uses `scripts/init-dev-secrets.sh` (Docker-oriented connection by default; override with `CONNECTION_STRING=...`).

## Business migrations

```bash
export APP_CONNECTION_STRING="Server=...;Database=ElectricalStore.Db;..."
dotnet ef migrations add <Name> \
  --project src/ElectricalStore.Infrastructure \
  --startup-project src/ElectricalStore.Api \
  --context AppDbContext \
  --output-dir Persistence/Migrations
```

## Production migrations

Do **not** rely on startup migrate. Apply Permixa package migrations and `AppDbContext` migrations in your release process (order: Permixa first, then App).

## Bootstrap lifecycle

enable → run once → verify Owner login → set `Permixa:Bootstrap:Enabled=false` → remove `OwnerPassword` user secret.  
Do **not** delete the Owner account.

## SampleNotes

SampleNotes teaching slice has been removed (`DropSampleNotes` migration).

## Data Protection keys

Guest Place Order idempotency encrypts the guest access token with ASP.NET Data Protection.
Configure `DataProtection:KeysPath` (shared durable directory in production) or accept the default `{BaseDirectory}/dp-keys`.
Ephemeral keys (process-local) make idempotent guest retries fail after restart.

## Forwarded headers / client IP (login rate limiting)

Permixa login rate limiting partitions by `Connection.RemoteIpAddress`. When Next proxies login, ASP.NET must trust only a configured hop. See [TRUSTED-CLIENT-IP.md](TRUSTED-CLIENT-IP.md).

| Environment | Default |
|-------------|---------|
| Development | `ForwardedHeaders:Enabled=true`, KnownProxies `127.0.0.1` / `::1` |
| Production | `Enabled=false` until operators set KnownProxies and/or KnownNetworks |

Do not enable Production ForwardedHeaders without a documented reverse-proxy / Next trust boundary.

## Operational logging

Uses built-in Microsoft.Extensions.Logging (no Seq/ELK/OpenTelemetry in-app).

| Environment | Console | Levels (high level) |
|-------------|---------|---------------------|
| Development | default text console | `ElectricalStore` Debug; EF command Warning |
| Production | JSON console (`appsettings.Production.json`) | Default/ElectricalStore Information; AspNetCore/EF Warning |

**Request logs:** method, route template, status code, elapsed ms, TraceId. Health `GET /health` is skipped. Never bodies or sensitive headers.

**Business operational logs (Information):** order placed/confirmed/preparing/out-for-delivery/delivered/cancelled/marked paid; inventory adjust (ids + quantities). Idempotency replay at Debug with OrderId only.

**Audit vs logs:** `InventoryAdjustment`, `OrderModificationAudit`, and Permixa IAM audit are durable accountability. Logs are for troubleshooting only.

**Docker/VPS:** write to stdout/stderr; configure host/Docker `json-file` rotation (see commented example in `docker-compose.yml`). Do not store permanent logs only inside an ephemeral container filesystem.

