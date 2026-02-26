# Wishlist Monorepo Bootstrap

## Структура

```text
.
├── backend
├── frontend
├── infra
└── docs
```

- `backend/` - API сервис
- `frontend/` - web UI + dev server
- `infra/` - Docker Compose и infra-файлы
- `docs/` - документация

## Один шаг после `git clone` (из корня)

Prerequisites: `docker` + `docker compose`, `npm`.

```bash
cp .env.example .env
docker compose up --build
```

Сервисы будут доступны по адресам:

- frontend: `http://localhost:5173`
- backend health: `http://localhost:18080/health`
- backend ready: `http://localhost:18080/health/ready`
- postgres: `localhost:55432`

## Команды разработки

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

- Основной ключ подключения: `ConnectionStrings:WishlistDb`
- Для env override: `ConnectionStrings__WishlistDb`
- В compose backend подключается к `postgres` сервису автоматически.
- Для stage/prod задавай `ConnectionStrings__WishlistDb` и `POSTGRES_*` через secrets/env, не через git.

## Nginx + Cloudflare TLS

В compose добавлен `nginx`, который публикует `80/443` и проксирует:

- `/api/*` -> `backend:8080`
- остальное -> `frontend:5173`

TLS для `wishlist.jonhef.org` подтягивается автоматически при старте из глобального Cloudflare каталога.

Переменные окружения:

- `CLOUDFLARE_GLOBAL_CONFIG_DIR` - путь на хосте к каталогу с cert/key (монтируется в контейнер как `/etc/cloudflare`)
- `CLOUDFLARE_CERT_FILE` - (опционально) полный путь к сертификату внутри контейнера
- `CLOUDFLARE_KEY_FILE` - (опционально) полный путь к приватному ключу внутри контейнера

Пример запуска:

```bash
export CLOUDFLARE_GLOBAL_CONFIG_DIR=/opt/cloudflare
docker compose up --build
```

## Базовые root scripts

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

Подробности по PostgreSQL и плану миграции данных (вариант A/B): `docs/db.md`.

## Остановка контейнеров

```bash
docker compose down
```
