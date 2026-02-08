# Build stage - optimized for multi-platform builds
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy solution and all project files for restore
COPY src/CamusApp.sln ./
COPY src/Api/emc.camus.api/*.csproj ./Api/emc.camus.api/
COPY src/Domain/emc.camus.domain/*.csproj ./Domain/emc.camus.domain/
COPY src/Application/emc.camus.application/*.csproj ./Application/emc.camus.application/
COPY src/Adapters/emc.camus.persistence.postgresql/*.csproj ./Adapters/emc.camus.persistence.postgresql/
COPY src/Adapters/emc.camus.observability.otel/*.csproj ./Adapters/emc.camus.observability.otel/
COPY src/Adapters/emc.camus.secrets.dapr/*.csproj ./Adapters/emc.camus.secrets.dapr/
COPY src/Adapters/emc.camus.security.jwt/*.csproj ./Adapters/emc.camus.security.jwt/
COPY src/Adapters/emc.camus.security.apikey/*.csproj ./Adapters/emc.camus.security.apikey/
COPY src/Adapters/emc.camus.documentation.swagger/*.csproj ./Adapters/emc.camus.documentation.swagger/

# Restore all dependencies
RUN dotnet restore ./Api/emc.camus.api/emc.camus.api.csproj

# Copy everything else and build
COPY src/. ./
WORKDIR /src/Api/emc.camus.api
RUN dotnet publish emc.camus.api.csproj \
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
ENTRYPOINT ["dotnet", "emc.camus.api.dll"]
