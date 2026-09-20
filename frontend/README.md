# ElectricalStore Frontend

Arabic-first / RTL-first Next.js App Router foundation for Storefront + Back Office.

## Architecture

- **One app** for storefront (`/`) and admin (`/admin`)
- **App Router** + TypeScript + Tailwind CSS v4
- **Server-first storefront**; admin is more client-interactive later
- **Native fetch** (`src/lib/api`) — no Axios, no generated OpenAPI SDK
- **TanStack Query** for browser server-state later (not for public SSR catalog)
- **Zustand / React Hook Form / Zod** installed; cart and forms not built yet
- **ASP.NET Core** remains the business backend (`http://localhost:5080`)

## Arabic / RTL

- Root `<html lang="ar" dir="rtl">`
- Arabic UI copy in shells; English identifiers in code
- Font: Noto Sans Arabic via `next/font` (swap centrally in `src/config/fonts.ts` + root layout)

## Brand-agnostic

Commercial identity is **not** finalized. Do not hardcode a store brand.

- Brand strings / logo placeholders: `src/config/brand.ts`
- Site locale / URL / metadata defaults: `src/config/site.ts`
- Display currency (not an API field): `src/config/store.ts` → `currencyCode: "SYP"`
- Semantic color tokens: `src/app/globals.css`

`ElectricalStore` is the **technical** repository name only.

## Environment variables

See `.env.example`:

| Variable | Role |
|---|---|
| `API_BASE_URL` | Server-side API base (`getServerApiBaseUrl`) |
| `NEXT_PUBLIC_API_BASE_URL` | Browser API base (`getBrowserApiBaseUrl`) |
| `NEXT_PUBLIC_SITE_URL` | Public site origin |

## Public API contract layer (F2)

Feature-owned modules — types match backend JSON; no invented fields; no runtime Zod validation of responses.

| Module | Functions | Routes |
|---|---|---|
| `src/features/catalog` | `getCategories`, `getCategory`, `getProducts`, `getProduct` | `/catalog/categories`, `/catalog/products` |
| `src/features/shipping` | `getDeliveryZones` | `/shipping/zones` |
| `src/features/checkout` | `previewCheckout` | `POST /checkout/preview` |

- Catalog/shipping GETs use `next.revalidate` (60s) for SSR-friendly caching.
- Checkout preview uses `cache: "no-store"`.
- HTTP errors throw `ApiError` (`status` + `code`); pages decide 404 UX later.
- **Not in F2:** Place Order, auth, admin APIs, cart, catalog UI.

## Public storefront catalog (F3)

Routes: `/`, `/categories`, `/categories/[id]`, `/products`, `/products/[id]` (GUID ids; no slugs).

- Server Components by default; client only for search form, gallery selection, mobile nav
- Uses F2 catalog modules only (`getCategories` / `getCategory` / `getProducts` / `getProduct`)
- Catalog GETs use `next: { revalidate: 60 }` (Data Cache). Do **not** set route-wide `force-dynamic` on the storefront layout — that opts fetches out of intended caching.
- Build prerenders static catalog shells (`/`, `/categories`, sitemap) against the live API; keep ASP.NET running for production-oriented `next build` verification
- Price display via `src/lib/format` + `storeConfig.currencyDisplay` (SYP)
- SEO: `generateMetadata`, Product JSON-LD, sitemap includes public categories/products when the API is available
- **Not in F3:** cart, checkout, auth, admin features

## Frontend-only cart (F4)

Route: `/cart` (not indexable).

- **No backend cart** — Zustand + `localStorage` key `electricalstore.cart`, persistence `version: 1`
- Cart lines keyed by `variantId`; `lastKnownUnitPrice` is a **display snapshot only**
- Quantity rules use each variant’s `quantityIncrement` (Piece/Meter); float-normalized
- Checkout Preview / Place Order are **not** called in F4 — F5 reconcilies price/stock/totals
- Invalid/malformed persisted JSON resets to an empty cart

## Guest checkout (F5)

Routes: `/checkout`, `/orders/[id]/confirmation` (non-indexable).

- **Checkout Preview** (`previewCheckout`) is authoritative for prices/stock/shipping/minimum — cart snapshots are display-only
- **Place Order** goes Browser → Next `/api/guest-orders/place` → ASP.NET (security boundary only; same-origin `Origin` required)
- `guestAccessToken` stored in order-scoped **HttpOnly** cookie `electricalstore.guest-order.{id}`
  - Path: `/api/guest-orders/{id}` (not sent on unrelated pages)
  - Max-Age: **30 days** (pragmatic frontend window — backend guest token has **no** expiry; 24h is idempotency-only)
  - Secure in production; SameSite=Lax
- Confirmation reload via Next `/api/guest-orders/{id}` + `X-Order-Token`; responses use `Cache-Control: private, no-store`
- Idempotency-Key in **sessionStorage** as `{ key, fingerprint }` (SHA-256; no plaintext PII); new key when payload changes
- Cart clears **only** after successful HTTP 201

## Customer auth + account (F6)

- Access token: memory only; refresh: HttpOnly `electricalstore.auth.refresh`
- Routes: `/login`, `/register`, `/account`, `/account/orders`
- Production Auth go-live still gated (trusted ingress) — see `docs/TRUSTED-CLIENT-IP.md`

## Admin foundation (F7)

- Protected `/admin` shell; permission-aware nav; no business CRUD yet
- Details: [`docs/ADMIN.md`](docs/ADMIN.md)
- Authorize by effective permission codes only — never role names

## Scripts

```bash
npm run dev
npm run lint
npm run typecheck
npm run test
npm run build
```

## Folder ownership

| Path | Role |
|---|---|
| `src/app/(storefront)` | Public storefront routes |
| `src/app/admin` | Back-office shell (F7 foundation) |
| `src/components/admin` | Admin shell / gates / primitives |
| `src/components/shared` | Storefront layout shells |
| `src/config` | Brand, site, store (currency), storefront navigation |
| `src/lib/api` | Fetch + ProblemDetails + query helpers |
| `src/lib/auth` | Session, refresh coordinator, permission helpers |
| `src/features/admin` | Admin permission catalog + nav + query conventions |
| `src/features/catalog` | Public catalog DTOs + API |
| `src/features/cart` | Frontend-only cart (Zustand + persistence) |
| `src/features/orders` | Place Order / Order DTOs + guest/customer clients |
| `src/features/shipping` | Public shipping DTOs + API |
| `src/features/checkout` | Preview + form schema + idempotency helpers |
| `src/features/auth` | Login/register schemas + register API |
| `src/components/cart` | Cart badge, add-to-cart, cart page UI |
| `src/components/checkout` | Checkout page UI |
| `src/components/orders` | Order confirmation UI |
| `src/components/storefront` | Catalog presentation components |
| `src/app/api/auth` | Login/refresh/logout cookie BFF |
| `src/app/api/guest-orders` | HttpOnly guest-token security handlers |

## Dev URLs

- Frontend: `http://localhost:3000`
- API: `http://localhost:5080`
- Local launcher (repo root): `.\dev.ps1` / `.\stop-dev.ps1`
