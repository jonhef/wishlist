.PHONY: format lint test build backend-run frontend-dev up-all up-dev down-dev start stop api-dev api-dev-migrate ef-update ef-add

format:
	npm run format

lint:
	npm run lint

test:
	npm run test

build:
	npm run build

backend-run:
	npm run backend-run

frontend-dev:
	npm run frontend-dev

up-all:
	docker compose up --build

up-dev:
	docker compose -f docker-compose.dev.yml up

down-dev:
	docker compose -f docker-compose.dev.yml down

start:
	docker compose up --build

stop:
	docker compose down

api-dev:
	ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/src/Wishlist.Api.csproj

api-dev-migrate:
	ASPNETCORE_ENVIRONMENT=Development APPLY_MIGRATIONS_ON_STARTUP=true dotnet run --project backend/src/Wishlist.Api.csproj

ef-update:
	dotnet ef database update --project backend/src/Wishlist.Api.csproj --startup-project backend/src/Wishlist.Api.csproj

ef-add:
	@test -n "$(NAME)" || (echo "Usage: make ef-add NAME=MigrationName" && exit 1)
	dotnet ef migrations add $(NAME) --project backend/src/Wishlist.Api.csproj --startup-project backend/src/Wishlist.Api.csproj --output-dir Infrastructure/Persistence/Migrations
