FROM node:20-alpine AS build

WORKDIR /app

COPY package.json ./package.json
COPY frontend/package.json ./frontend/package.json
COPY backend/package.json ./backend/package.json

RUN npm install --workspaces --include-workspace-root=false

COPY frontend ./frontend

WORKDIR /app/frontend

RUN npm run build

FROM nginx:1.27-alpine

COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/frontend/dist /usr/share/nginx/html

EXPOSE 80
