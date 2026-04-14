# ── Build Stage ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ── Runtime Stage ─────────────────────────────────────────
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy published Blazor WASM files
COPY --from=build /app/publish/wwwroot .

# Nginx config for Blazor SPA routing
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80