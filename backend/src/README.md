# Backend EF Core setup

## Requirements

- .NET SDK 10+
- Local tool restore: `dotnet tool restore`

## Environment variables

- `ConnectionStrings__DefaultConnection` - primary connection string
- `DB_CONNECTION_STRING` - fallback connection string
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
