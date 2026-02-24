# Backend EF Core setup

## Requirements

- .NET SDK 10+
- Local tool restore: `dotnet tool restore`

## Environment variables

- `ConnectionStrings__DefaultConnection` - primary connection string
- `DB_CONNECTION_STRING` - fallback connection string
- `ASPNETCORE_ENVIRONMENT=Development` - dev mode
- `APPLY_MIGRATIONS_ON_STARTUP=true` - apply pending migrations on app start (dev only)

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
