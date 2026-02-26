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

- frontend: `http://localhost:5173`
- backend health: `http://localhost:18080/health`
- backend ready: `http://localhost:18080/health/ready`
- postgres: `localhost:55432`

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

## Nginx + Cloudflare TLS

In compose, `nginx` is added, publishing `80/443` and proxying:

- `/api/*` -> `backend:8080`
- everything else -> `frontend:5173`

TLS for `wishlist.jonhef.org` is loaded automatically at startup from the global Cloudflare directory.

Environment variables:

- `CLOUDFLARE_GLOBAL_CONFIG_DIR` - host path to the cert/key directory (mounted into the container as `/etc/cloudflare`)
- `CLOUDFLARE_CERT_FILE` - (optional) full path to the certificate inside the container
- `CLOUDFLARE_KEY_FILE` - (optional) full path to the private key inside the container

Launch example:

```bash
export CLOUDFLARE_GLOBAL_CONFIG_DIR=/opt/cloudflare
docker compose up --build
```

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
