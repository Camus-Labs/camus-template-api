# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Authentication & Authorization

- JWT Bearer authentication with RSA256 signature validation
- API Key authentication via X-Api-Key header
- Dapr-based secrets management for credentials and keys
- CORS configuration with policy-based origin control

#### Rate Limiting

- Rate limiting adapter with policy-based sliding window algorithm
- IP-based rate limiting with proxy header support (X-Forwarded-For, X-Real-IP)
- `[RateLimit]` attribute for controller/action-level policy assignment
- RFC-compliant rate limit headers (RateLimit-Limit, RateLimit-Reset, Retry-After)
- OpenTelemetry metrics for rate limit tracking (hits, rejections, undefined policies)
- Rate limit configuration validation with fail-fast startup
- Exempt paths configuration for health checks and monitoring endpoints

#### Observability

- OpenTelemetry integration with multiple exporters (OTLP, Jaeger, Zipkin, Console)
- Distributed tracing with trace context propagation
- Prometheus-compatible metrics export
- Serilog structured logging with OTLP exporter to Loki
- Configurable log levels per namespace
- Trace and span IDs in log entries for correlation

#### Data Persistence

- PostgreSQL adapter with Dapper for lightweight ORM
- Connection pooling and resilience configuration
- Health checks for database connectivity

#### API Features

- Swagger/OpenAPI documentation with multiple API versions
- API versioning support (v1, v2)
- Custom exception handling middleware with RFC 7807 Problem Details
- Health check endpoints (/health, /ready, /alive)
- Comprehensive XML documentation on all public APIs

#### Architecture

- Clean/Hexagonal architecture with clear layer separation
- Domain, Application, and Adapter layer structure
- Dependency inversion with port/adapter pattern
- Comprehensive test project structure for all layers

### Security

- Rate limiting runs before authentication to protect auth endpoints from brute force attacks
- IP-based limiting prevents abuse from anonymous attackers
- JWT tokens with RSA256 asymmetric encryption
- API Key authentication for service-to-service communication
- Secrets never stored in code or configuration files
- CORS policies to prevent unauthorized cross-origin requests
- Comprehensive logging of rate limit violations for security monitoring

### Infrastructure

- Docker support with development and production Dockerfiles
- Docker Compose configurations for local development
- Hot-reload support in development containers
- VS Code debugging support for containerized applications
- Observability stack (Jaeger, Prometheus, Grafana, Loki) via Docker Compose
- .NET 9.0 target framework
- GitHub Actions workflows for CI/CD (if configured)
