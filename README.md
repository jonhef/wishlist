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
docker compose up --build
```

Сервисы будут доступны по адресам:

- frontend: `http://localhost:5173`
- backend health: `http://localhost:18080/health`

## Команды разработки

- Backend run: `npm run backend:run`
- Frontend dev: `npm run frontend:dev`
- Up all (docker compose): `docker compose up --build`
- Down all (docker compose): `docker compose down`
- Up dev compose (watch + bind mount): `docker compose -f docker-compose.dev.yml up`
- Down dev compose: `docker compose -f docker-compose.dev.yml down`

## Базовые root scripts

- `npm run format`
- `npm run lint`
- `npm run test`
- `npm run build`

## Остановка контейнеров

```bash
docker compose down
```
