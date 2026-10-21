# Sitio web: compila Angular y lo sirve con Caddy (que ademas reenvia /api y /hubs a la API).
# El contexto de construccion es la RAIZ del repositorio (el docker-compose ya lo indica).

# ---------- 1. Compilar Angular ----------
FROM node:24-alpine AS build
WORKDIR /app

COPY Frontend/SistemaClimaticoWeb/package*.json ./
# "npm ci" instala exactamente lo que dice package-lock.json; si el lock no coincide con package.json, usa "npm install".
RUN (npm ci --no-audit --no-fund || npm install --no-audit --no-fund)

COPY Frontend/SistemaClimaticoWeb/ ./
RUN npm run build

# ---------- 2. Servidor web ----------
FROM caddy:2-alpine
COPY --from=build /app/dist/SistemaClimaticoWeb/browser /srv
COPY Docker/Caddyfile /etc/caddy/Caddyfile
EXPOSE 80 443
