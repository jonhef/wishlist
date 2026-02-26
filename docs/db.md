# Database: PostgreSQL

## Current state

- Primary DB for `dev/stage/prod`: PostgreSQL.
- EF Core provider: `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Primary connection string key: `ConnectionStrings:WishlistDb`.
- Env override: `ConnectionStrings__WishlistDb`.
- Default local docker host port: `55432` (`POSTGRES_HOST_PORT`).

## Local run

1. Start PostgreSQL:

```bash
docker compose up -d postgres
```

2. Apply EF migrations:

```bash
dotnet ef database update \
  --project backend/src/Wishlist.Api.csproj \
  --startup-project backend/src/Wishlist.Api.csproj
```

3. Run API:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/src/Wishlist.Api.csproj
```

Or with one command:

```bash
npm run api:bootstrap
```

## Docker compose

- `postgres` service:
  - image: `postgres:16`
  - volume: `pgdata`
  - healthcheck: `pg_isready`
- `backend` waits for `postgres` using `depends_on.condition=service_healthy`.

## Readiness

- `GET /health` - liveness.
- `GET /health/ready` - readiness + PostgreSQL connectivity check (`CanConnectAsync`).

## PostgreSQL schema notes

- `Guid` -> `uuid`
- `DateTime` -> `timestamp with time zone`
- theme tokens (`tokens_json`) -> `jsonb`
- naming: snake_case for tables/columns

## Data migration strategy

- Option A (default for MVP): **no legacy data migration**. Dev DB is reset and a new initial migration is created for PostgreSQL.
- Option B (optional): one-time `sqlite -> postgres` migrator as a separate script/utility.
  - This option is not implemented yet.
  - Extension point: a separate project/command in `backend/src` or `scripts/` that reads SQLite and writes to PostgreSQL through EF.
