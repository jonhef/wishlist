.PHONY: format lint test build backend-run frontend-dev up-all up-dev down-dev start stop

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
