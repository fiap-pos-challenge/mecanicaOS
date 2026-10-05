# syntax=docker/dockerfile:1

# ---- build -------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (only project and build files) so the layer is cached between code changes.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props BannedSymbols.txt .editorconfig ./
COPY src/Oficina.Domain/Oficina.Domain.csproj src/Oficina.Domain/
COPY src/Oficina.Application/Oficina.Application.csproj src/Oficina.Application/
COPY src/Oficina.Infrastructure/Oficina.Infrastructure.csproj src/Oficina.Infrastructure/
COPY src/Oficina.Api/Oficina.Api.csproj src/Oficina.Api/
RUN dotnet restore src/Oficina.Api/Oficina.Api.csproj

COPY src/ src/
RUN dotnet publish src/Oficina.Api/Oficina.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false \
    -p:DebugType=None \
    -p:DebugSymbols=false

# ---- runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# Non-root user provided by the official .NET images.
USER $APP_UID
EXPOSE 8080

# The runtime image has no curl/wget; bash /dev/tcp avoids installing extra packages.
HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\\r\\nHost: localhost\\r\\n\\r\\n' >&3 && grep -q Healthy <&3"]

ENTRYPOINT ["dotnet", "Oficina.Api.dll"]
