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

- frontend (nginx + static build): `http://localhost:5183`
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

## Nginx

Default `docker compose up --build` now starts an `nginx` container for the frontend.

- public entry point: `http://localhost:5183`
- `nginx` serves the built `frontend/dist` folder
- `nginx` proxies `/api/*` to the internal `backend` container

For live frontend development with Vite HMR, use `docker compose -f docker-compose.dev.yml up` instead.

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
