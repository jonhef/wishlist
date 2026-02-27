# Backend EF Core setup

## Requirements

- .NET SDK 10+
- Local tool restore: `dotnet tool restore`

## Environment variables

- `ConnectionStrings__WishlistDb` - primary connection string (PostgreSQL)
- `ASPNETCORE_ENVIRONMENT=Development` - dev mode
- `APPLY_MIGRATIONS_ON_STARTUP=true` - apply pending migrations on app start (dev only)
- `Auth__AccessTokenTtlMinutes=15` - access token TTL
- `Auth__RefreshTokenTtlDays=30` - refresh token TTL
- `Auth__AccessTokenTtlSeconds=1` - optional test override for short-lived access tokens

`APPLY_MIGRATIONS_ON_STARTUP` is ignored outside `Development`.

## Migrations workflow

Run from repository root:

```bash
dotnet tool restore

dotnet ef migrations add InitialCreate \
  --project backend/src/Wishlist.Api.csproj \
  --startup-project backend/src/Wishlist.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations

dotnet ef database update \
  --project backend/src/Wishlist.Api.csproj \
  --startup-project backend/src/Wishlist.Api.csproj
```

## Auth endpoints

- `POST /auth/register` with `{ email, password }`
  - validates email format
  - password policy: minimum 12 chars
  - duplicate email -> `409 Conflict`
- `POST /auth/login` with `{ email, password }`
  - response: `{ accessToken, refreshToken }` (or refresh in cookie when enabled)
  - invalid credentials -> `401 Unauthorized` (without hints)
- `POST /auth/refresh` with `{ refreshToken }` or refresh cookie
  - returns new token pair
  - refresh token rotation enabled
  - refresh replay / invalid token -> `401 Unauthorized`
- `POST /auth/logout` with `{ refreshToken }` or refresh cookie
  - revokes current refresh token
  - next refresh with same token fails

## Authorization (OwnerOnly)

- Middleware configured: `UseAuthentication()` + `UseAuthorization()`.
- Helper for current user id from claims: `Api/Auth/CurrentUserAccessor.cs`.
- Policy `OwnerOnly`: `Api/Auth/AuthorizationPolicies.cs`.
- Policy handler: `Api/Auth/OwnerAuthorizationHandler.cs`.

Protected wishlist endpoints:

- `POST /wishlists` (token required)
- `GET /wishlists` (owner scope)
- `GET /wishlists/{wishlistId}` (owner only)
- `PATCH /wishlists/{wishlistId}` (owner only)
- `DELETE /wishlists/{wishlistId}` (owner only)

Expected behavior:

- no token on protected endpoint -> `401 Unauthorized`
- accessing another user's wishlist/items -> `403 Forbidden`

## Wishlist CRUD (private)

Endpoints:

- `POST /wishlists` with `{ title, description?, themeId? }`
- `GET /wishlists?cursor=...&limit=...`
- `GET /wishlists/{id}`
- `PATCH /wishlists/{id}` with `{ title?, description?, themeId? }`
- `DELETE /wishlists/{id}` (soft delete)

Response model:

- `id`, `title`, `description`, `themeId`, `updatedAt`, `itemsCount`

Rules:

- list returns only current user's wishlists
- `limit` is capped at `50`
- `PATCH` with empty payload returns `400`
- `DELETE` is soft delete (`isDeleted`, `deletedAtUtc`)
- for `themeId` access mismatch we return `400 Bad Request` (chosen behavior)

## Wishlist Items CRUD

Endpoints:

- `POST /wishlists/{id}/items`
- `GET /wishlists/{id}/items?cursor=...&limit=...`
- `POST /wishlists/{id}/items/rebalance`
- `PATCH /wishlists/{id}/items/{itemId}`
- `DELETE /wishlists/{id}/items/{itemId}`

Item fields:

- `name` (required)
- `url` (optional)
- `priceAmount` / `priceCurrency` (optional pair)
  - `priceAmount` is stored in **minor units** (`int`)
  - supported currencies: `EUR`, `USD`, `RUB`, `JPY`
- `priority` (optional `decimal`, higher = more important)
- `notes` (optional, max 2000)

Rules:

- cannot add/list/update/delete items in foreign wishlist (`403`)
- `POST /items` without `priority` appends to bottom (`bottom - 1024`, empty list -> `0`)
- list sorting is fixed and stable: `priority DESC`, then `createdAtUtc DESC`, then `id DESC`
- `POST /items/rebalance` reassigns priorities with large gaps while preserving visible order
- item list excludes soft-deleted items
- URL without scheme is normalized by prepending `https://`
- `priceCurrency` without `priceAmount` returns `400`
- unsupported `priceCurrency` returns `400`

## Wishlist Sharing (public read-only)

Endpoints:

- `POST /wishlists/{id}/share` -> returns `{ publicUrl }`
- `DELETE /wishlists/{id}/share`
- `GET /public/wishlists/{token}?cursor=...&limit=...&sort=priority|added|price&order=asc|desc` (no auth)

Behavior:

- `POST /share` always rotates token (old token becomes invalid)
- token is generated as random base64url value and only `token_hash` is stored in DB
- `DELETE /share` disables sharing
- public response contains `title`, `description`, `themeTokens`, `items`
- public item payload: `id`, `name`, `url?`, `priceAmount?`, `priceCurrency?`, `notes?`, `createdAt` (`priority` is internal and never returned)
- public items listing is cursor-paginated, `limit` max is `50`
- sorting rules:
  - `sort=priority` (default): `priority DESC`, then `created_at_utc DESC`, then `id DESC`
  - `sort=added`: `created_at_utc DESC`, then `id DESC`
  - `sort=price`: normalized price in wishlist `base_currency` (`order=asc|desc`), unknown prices at the end,
    tie-breakers `created_at_utc DESC`, then `id DESC`
- disabled/invalid share token returns `404` (not `403`)
- public endpoint is rate-limited (`60 req/min`)

## FX rates auto-update

- sources:
  - ECB daily XML: `https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml`
  - CBR daily XML: `https://www.cbr.ru/scripts/XML_daily.asp`
- canonical storage base: `EUR` (`fx_rates.base_currency = 'EUR'`)
- updater runs on startup and then every `FxRates:RefreshIntervalHours` (default `6`)
- updater uses PostgreSQL advisory lock (`hashtext('fx_rates_update')`) to avoid multi-instance races
- on provider failures, API keeps using latest persisted rates
- if rates are missing and `sort=price` is requested, public endpoint returns `400 validation_error`
- in `Development`, fallback seeding can be enabled with `FxRates:SeedDevelopmentFallback=true`

## Themes v1 (personal)

Endpoints:

- `GET /themes/default` (public preset, no auth)
- `POST /themes` with `{ name, tokens }`
- `GET /themes?cursor=...&limit=...`
- `GET /themes/{id}`
- `PATCH /themes/{id}` with `{ name?, tokens? }`
- `DELETE /themes/{id}`

Tokens schema v1 (see `docs/theme-tokens.schema.json`):

- `schemaVersion`: `1`
- required on save: `colors.bg0`, `colors.text`
- supported color keys: `bg0`, `bg1`, `bg2`, `text`, `mutedText`, `border`, `primary`,
  `primaryHover`, `accentNeon`, `secondary`, `danger`, `success`, `warn`, `error`
- `typography`: `fontDisplay`, `fontBody`, `fontMono`, `letterSpacingDisplay`, `displayFontEnabled`
- `radii`: `sm`, `md`, `lg`
- `effects`: `glowSm`, `glowMd`, `glowLg`, `glowEnabled`, `glowIntensity`, `noiseOpacity`

Rules:

- each theme belongs to `ownerUserId`; only owner can read/update/delete
- wishlist can reference only owner's `themeId` (checked in wishlist service)
- save accepts partial token payload, but `colors.bg0` and `colors.text` are mandatory
- list/get/public always return resolved tokens with fallback from `DefaultDarkPinkNeon`

## Error format (RFC7807)

API returns errors in `application/problem+json` format.

Mapping:

- `validation_error` -> `400` with `errors` extension (`{ field: [messages] }`)
- `unauthorized` -> `401`
- `forbidden` -> `403`
- `not_found` -> `404`

Example (`400`):

```json
{
  "type": "https://wishlist.local/problems/validation-error",
  "title": "Validation error",
  "status": 400,
  "detail": "Validation failed.",
  "instance": "/auth/register",
  "errors": {
    "email": ["Email format is invalid."]
  }
}
```

## Observability v1

- Structured logging via `Serilog` (console sink).
- Correlation id per request:
  - canonical id is request trace id
  - returned in response header `X-Correlation-ID`
- Request log includes:
  - `RequestMethod`
  - `RequestPath` (route template when available)
  - `StatusCode`
  - duration (`Elapsed`)
  - `UserId` (`anonymous` for unauthenticated)
  - `CorrelationId`
- Exceptions are handled by middleware and logged with stack trace + correlation id.
- Passwords/tokens are not logged by middleware (no body/header logging; route template is used to avoid leaking public token values).

## Performance baseline

- All list endpoints are paginated (cursor + `limit`).
- `limit` is capped at `50` in every list service:
  - wishlists
  - wishlist items
  - themes
  - public wishlist items
- Wishlist list avoids N+1 for item counts:
  - first query: paged wishlists
  - second grouped query: item counts by wishlist id
  - no `Include(items)` fan-out in list endpoint

Main indexes used by list/read paths:

- `wishlists(owner_user_id, updated_at_utc, id)`
- `wish_items(wishlist_id, priority, created_at_utc, id)`
- `themes(owner_user_id, created_at_utc, id)`
- `wishlists(share_token_hash)` for public token lookup

PostgreSQL check commands (from repo root):

```bash
psql "host=localhost port=55432 dbname=wishlist user=wishlist password=wishlist_dev_password" <<'SQL'
EXPLAIN
SELECT id, title, description, theme_id, updated_at_utc
FROM wishlists
WHERE owner_user_id = '00000000-0000-0000-0000-000000000000' AND is_deleted = false
ORDER BY updated_at_utc DESC, id DESC
LIMIT 51;

EXPLAIN
SELECT id, wishlist_id, name, updated_at_utc
FROM wish_items
WHERE wishlist_id = '00000000-0000-0000-0000-000000000000' AND is_deleted = false
ORDER BY priority DESC, created_at_utc DESC, id DESC
LIMIT 51;

EXPLAIN
SELECT id, name, created_at_utc
FROM themes
WHERE owner_user_id = '00000000-0000-0000-0000-000000000000'
ORDER BY created_at_utc DESC, id DESC
LIMIT 51;

EXPLAIN
SELECT id, title
FROM wishlists
WHERE share_token_hash = 'ABCDEF' AND is_deleted = false
LIMIT 1;
SQL
```

Expected shape: planner uses indexes for bounded scans (`Index Scan` / `Bitmap Index Scan`) on listed queries.
