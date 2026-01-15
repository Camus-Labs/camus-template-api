# Build stage - optimized for multi-platform builds
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy solution and all project files for restore
COPY src/CamusApp.sln ./
COPY src/Api/emc.main.api/*.csproj ./Api/emc.main.api/
COPY src/Domain/emc.domain/*.csproj ./Domain/emc.domain/
COPY src/Application/emc.application/*.csproj ./Application/emc.application/
COPY src/Adapters/emc.datapersistance.postgresql/*.csproj ./Adapters/emc.datapersistance.postgresql/
COPY src/Adapters/emc.observability.otel/*.csproj ./Adapters/emc.observability.otel/
COPY src/Adapters/emc.secretstorage.dapr/*.csproj ./Adapters/emc.secretstorage.dapr/

# Restore all dependencies
RUN dotnet restore ./Api/emc.main.api/emc.camus.main.api.csproj

# Copy everything else and build
COPY src/. ./
WORKDIR /src/Api/emc.main.api
RUN dotnet publish emc.camus.main.api.csproj \
    -c Release \
    -o /app/publish \
    -p:UseAppHost=false \
    -p:GenerateFullPaths=true

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Copy published files from build stage with correct ownership
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

# Switch to non-root user (app user already exists in the base image)
USER app

# Expose port (documentation and for docker run -P)
EXPOSE 80

# Environment variables - can be overridden at runtime
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# OpenTelemetry defaults - override in docker-compose or Kubernetes
ENV OpenTelemetry__Tracing__Exporter=otlp
ENV OpenTelemetry__Tracing__OtlpEndpoint=http://localhost:4317
ENV OpenTelemetry__Metrics__Exporter=otlp
ENV OpenTelemetry__Metrics__OtlpEndpoint=http://localhost:4317
ENV OpenTelemetry__Logs__Exporter=otlp
ENV OpenTelemetry__Logs__OtlpEndpoint=http://localhost:4317

# Run the app
ENTRYPOINT ["dotnet", "emc.camus.main.api.dll"]
