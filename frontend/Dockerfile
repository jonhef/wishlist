FROM node:20-alpine

WORKDIR /app

COPY frontend/dev-server.mjs ./dev-server.mjs
COPY frontend/src ./src

EXPOSE 5173

CMD ["node", "dev-server.mjs"]
