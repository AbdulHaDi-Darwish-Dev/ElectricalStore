# ElectricalStore

ASP.NET Core store API consuming **Permixa** IAM from NuGet.

## Quick start (Windows — preferred)

```powershell
cd E:\ElectricalStore
.\scripts\init-dev-secrets.ps1
.\dev.ps1
```

This opens the API and Next.js storefront in separate windows:

- Frontend: http://localhost:3100
- Admin: http://localhost:3100/admin
- Backend: http://localhost:5180
- Health: http://localhost:5180/health

Stop both (only processes started by the launcher):

```powershell
.\stop-dev.ps1
```

Re-running `.\dev.ps1` safely restarts launcher-owned previous processes first.

**One backend owner at a time:** use either `.\dev.ps1` **or** Visual Studio debugging — not both (they share port 5180).

Or start the API alone:

```powershell
dotnet run --project src/ElectricalStore.Api --launch-profile http
```

Uses local SQL Server `DESKTOP-30CDIBP\MSSQLSERVER22` / database `ElectricalStore.Db` with Windows Authentication (configured by the setup script + `appsettings.Development.json`).

Then:

1. `POST /auth/login` with the Owner credentials from the setup output / user-secrets
2. Call catalog or admin endpoints (see Swagger), or import the Postman collection below

### Postman / Frontend API Contract

- Collection: [`docs/postman/ElectricalStore.postman_collection.json`](docs/postman/ElectricalStore.postman_collection.json)
- Environment: [`docs/postman/ElectricalStore.Local.postman_environment.json`](docs/postman/ElectricalStore.Local.postman_environment.json)

Set `baseUrl`, fill `ownerEmail` / `ownerPassword` locally, run **Login** to populate tokens. Reuse `idempotencyKey` on Place Order retries; use `guestOrderToken` for guest `X-Order-Token` flows. Details: [docs/DEVELOPMENT-GUIDE.md](docs/DEVELOPMENT-GUIDE.md#postman--frontend-api-contract).

See [docs/DEVELOPMENT-GUIDE.md](docs/DEVELOPMENT-GUIDE.md) for daily start, bootstrap disable steps, Docker, and Testcontainers.

## Optional Docker SQL

```bash
cp .env.example .env
docker compose up -d
./scripts/init-dev-secrets.sh   # Linux/macOS; Windows prefers the .ps1 path above
dotnet run --project src/ElectricalStore.Api
```

## Auth endpoints

- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout`
- `GET /me`

Catalog / storefront (examples):

- `GET /catalog/categories`, `GET /catalog/products`
- `GET /shipping/zones`
- `POST /checkout/preview`
- `POST /orders` (requires `Idempotency-Key`)

Back Office requires Bearer + feature/`Iam.*` permissions (see Swagger tags).

## Logging (production)

The API writes structured logs to **stdout/stderr** (JSON console in Production). Persist/rotate/retain via Docker/VPS host configuration — not inside the app. See [docs/DEVELOPMENT-GUIDE.md](docs/DEVELOPMENT-GUIDE.md) and [docs/SECURITY.md](docs/SECURITY.md).

With `--resend` also:

- `POST /auth/email-confirmation/request`
- `POST /auth/email-confirmation/confirm`

## License

Choose and add a license for **this application**. Permixa packages remain Apache-2.0.
