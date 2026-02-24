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

## Один шаг после `git clone`

Prerequisites: `docker` + `docker compose`, `npm`.

```bash
npm run up:all
```

Сервисы будут доступны по адресам:

- frontend: `http://localhost:5173`
- backend health: `http://localhost:18080/health`

## Команды разработки

- Backend run: `npm run backend:run`
- Frontend dev: `npm run frontend:dev`
- Up all (docker compose): `npm run up:all`

## Базовые root scripts

- `npm run format`
- `npm run lint`
- `npm run test`
- `npm run build`

## Остановка контейнеров

```bash
npm run down
```
