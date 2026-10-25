# syntax=docker/dockerfile:1

# ---- build -------------------------------------------------------------------
FROM node:24-alpine AS build
WORKDIR /app

# Install first (only manifests) so the layer is cached between code changes.
COPY package.json package-lock.json ./
RUN npm ci

# The E2E stack passes production,e2e to turn on the API of features whose data the tests intercept.
ARG NG_CONFIGURATION=production

COPY . .
RUN npx ng build --configuration ${NG_CONFIGURATION}

# ---- runtime -----------------------------------------------------------------
# Unprivileged image: nginx runs as a non-root user on port 8080.
FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime

ENV API_UPSTREAM=http://api:8080

COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/oficina-web/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=5 \
    CMD wget -q -O /dev/null http://127.0.0.1:8080/ || exit 1
