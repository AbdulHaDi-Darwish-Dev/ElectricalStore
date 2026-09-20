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
| `src/app/admin` | Back-office shell |
| `src/components/shared` | Layout shells |
| `src/config` | Brand, site, store (currency), navigation |
| `src/lib/api` | Fetch + ProblemDetails + query helpers |
| `src/lib/auth` | Reserved — auth not implemented |
| `src/features/catalog` | Public catalog DTOs + API |
| `src/features/shipping` | Public shipping DTOs + API |
| `src/features/checkout` | Checkout preview DTOs + API |

## Dev URLs

- Frontend: `http://localhost:3000`
- API: `http://localhost:5080`
