# Database: PostgreSQL

## Current state

- Основная БД для `dev/stage/prod`: PostgreSQL.
- EF Core provider: `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Основной connection string key: `ConnectionStrings:WishlistDb`.
- Env override: `ConnectionStrings__WishlistDb`.
- Локальный docker host-port по умолчанию: `55432` (`POSTGRES_HOST_PORT`).

## Local run

1. Поднять PostgreSQL:

```bash
docker compose up -d postgres
```

2. Применить EF миграции:

```bash
dotnet ef database update \
  --project backend/src/Wishlist.Api.csproj \
  --startup-project backend/src/Wishlist.Api.csproj
```

3. Запустить API:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/src/Wishlist.Api.csproj
```

Или одной командой:

```bash
npm run api:bootstrap
```

## Docker compose

- Сервис `postgres`:
  - image: `postgres:16`
  - volume: `pgdata`
  - healthcheck: `pg_isready`
- `backend` ждёт `postgres` через `depends_on.condition=service_healthy`.

## Readiness

- `GET /health` - liveness.
- `GET /health/ready` - readiness + проверка подключения к PostgreSQL (`CanConnectAsync`).

## PostgreSQL schema notes

- `Guid` -> `uuid`
- `DateTime` -> `timestamp with time zone`
- theme tokens (`tokens_json`) -> `jsonb`
- naming: snake_case для таблиц/колонок

## Data migration strategy

- Вариант A (default для MVP): **без переноса legacy data**. Dev база сбрасывается, создаётся новая initial migration под PostgreSQL.
- Вариант B (опционально): одноразовый мигратор `sqlite -> postgres` отдельным скриптом/утилитой.
  - Этот вариант пока не реализован.
  - Точка расширения: отдельный проект/команда в `backend/src` или `scripts/`, который читает SQLite и пишет в PostgreSQL через EF.
