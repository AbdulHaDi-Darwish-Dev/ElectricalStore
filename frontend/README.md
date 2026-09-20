# ElectricalStore Frontend

Arabic-first / RTL-first Next.js App Router foundation for Storefront + Back Office.

## Architecture (F1)

- **One app** for storefront (`/`) and admin (`/admin`)
- **App Router** + TypeScript + Tailwind CSS v4
- **Server-first storefront**; admin is more client-interactive later
- **Native fetch** (`src/lib/api`) — no Axios, no generated SDK
- **TanStack Query** provider ready for browser server-state (admin/account)
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
- Semantic color tokens: `src/app/globals.css` (`--primary`, `--background`, …)

`ElectricalStore` is the **technical** repository name only.

## Environment variables

See `.env.example`:

| Variable | Role |
|---|---|
| `API_BASE_URL` | Server-side API base |
| `NEXT_PUBLIC_API_BASE_URL` | Browser API base |
| `NEXT_PUBLIC_SITE_URL` | Public site origin |

## Scripts

```bash
npm run dev
npm run lint
npm run typecheck
npm run build
```

## Folder ownership

| Path | Role |
|---|---|
| `src/app/(storefront)` | Public storefront routes |
| `src/app/admin` | Back-office shell |
| `src/components/shared` | Layout shells (header/footer/sidebar) |
| `src/components/ui` | Reserved for selective primitives later |
| `src/config` | Brand, site, navigation |
| `src/lib/api` | Fetch + ProblemDetails types |
| `src/lib/auth` | Reserved — auth not implemented in F1 |
| `src/lib/query` | QueryClient factory |
| `src/features` | Feature modules (empty until features land) |

## Intentionally NOT in F1

- Catalog, cart, checkout, orders
- Login / register / refresh / `/me`
- Admin CRUD, permissions UX, inventory, shipping
- Business Next.js proxy routes
- Full i18n framework
- shadcn component library install (deferred until needed)

## Dev URLs

- Frontend: `http://localhost:3000`
- API: `http://localhost:5080`
