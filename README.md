# Wishlist Monorepo Bootstrap

## Structure

```text
.
├── backend
├── frontend
├── infra
└── docs
```

- `backend/` - API service
- `frontend/` - web UI + dev server
- `infra/` - Docker Compose and infra files
- `docs/` - documentation

## One step after `git clone` (from repo root)

Prerequisites: `docker` + `docker compose`, `npm`.

```bash
cp .env.example .env
docker compose up --build
```

Services will be available at:

- frontend: `http://localhost:5183`
- backend health: `http://localhost:19080/health`
- backend ready: `http://localhost:19080/health/ready`
- postgres: `localhost:56432`

## Development commands

- Backend run: `npm run backend:run`
- Frontend dev: `npm run frontend:dev`
- .NET API dev run: `npm run api:dev`
- .NET API dev run + auto migrations: `npm run api:dev:migrate`
- .NET API bootstrap (postgres + migrations + run): `npm run api:bootstrap`
- Postgres only (local): `npm run db:up`
- Up all (docker compose): `docker compose up --build`
- Down all (docker compose): `docker compose down`
- Up dev compose (watch + bind mount): `docker compose -f docker-compose.dev.yml up`
- Down dev compose: `docker compose -f docker-compose.dev.yml down`

## Database (PostgreSQL)

- Primary connection key: `ConnectionStrings:WishlistDb`
- For env override: `ConnectionStrings__WishlistDb`
- In compose, backend connects to the `postgres` service automatically.
- For stage/prod, set `ConnectionStrings__WishlistDb` and `POSTGRES_*` via secrets/env, not via git.

## Global Nginx

Docker Compose no longer starts a local `nginx` container.

Recommended production setup:

- use your global `nginx` on the host as the only public entry point
- proxy `/api/*` -> `http://127.0.0.1:19080/`
- proxy everything else -> `http://127.0.0.1:5183`

The files in `infra/nginx/` are kept as reference templates for the host-level `nginx` setup, but they are not used by Docker Compose.

## Basic root scripts

- `npm run format`
- `npm run lint`
- `npm run test`
- `npm run build`

## EF Core migrations workflow

1. Restore tooling:

```bash
dotnet tool restore
```

2. Add migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project backend/src/Wishlist.Api.csproj \
  --startup-project backend/src/Wishlist.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations
```

3. Apply migration:

```bash
dotnet ef database update \
  --project backend/src/Wishlist.Api.csproj \
  --startup-project backend/src/Wishlist.Api.csproj
```

Details on PostgreSQL and the data migration plan (option A/B): `docs/db.md`.

## Stop containers

```bash
docker compose down
```
