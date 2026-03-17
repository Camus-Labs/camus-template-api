# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - YYYY-MM-DD

### Added

- Clean/Hexagonal architecture with Domain, Application, API, and Adapter layers
- Dependency inversion with port/adapter pattern
- CQRS-style type organization (Commands, Results, Filters, Views per feature)
- API versioning with `Asp.Versioning` (v1, v2) and versioned DTO folders
- JWT Bearer authentication with RSA256 signature validation
- API Key authentication for service-to-service communication
- Token generation, listing, and revocation endpoints
- Dapr-based secrets management for credentials and keys
- CORS configuration with policy-based origin control
- Rate limiting adapter with policy-based sliding window algorithm
- IP-based rate limiting with proxy header support (X-Forwarded-For, X-Real-IP)
- `[RateLimit]` attribute for controller/action-level policy assignment
- RFC-compliant rate limit headers (RateLimit-Limit, RateLimit-Reset, Retry-After)
- Exempt paths configuration for health checks and monitoring endpoints
- Rate limiting before authentication to protect auth endpoints from brute force
- PostgreSQL adapter with Dapper for lightweight ORM
- Database migration scripts management
- Connection pooling and resilience configuration
- Entity-centric and parameter-based write patterns in repository adapters
- OpenTelemetry integration with multiple exporters (OTLP, Jaeger, Zipkin, Console)
- Distributed tracing with trace context propagation
- OpenTelemetry metrics for rate limit tracking (hits, rejections, undefined policies)
- Prometheus-compatible metrics export
- Serilog structured logging with OTLP exporter to Loki
- Configurable log levels per namespace with trace/span ID correlation
- Swagger/OpenAPI documentation with versioned API definitions
- Custom exception handling middleware with RFC 7807 Problem Details
- Health check endpoints (/health, /ready, /alive)
- Comprehensive XML documentation on all public APIs
- Pre-commit review checklist optimized for AI-driven code review
- Docker support with development and production Dockerfiles
- Docker Compose configurations for local development with hot-reload
- VS Code debugging support for containerized applications
- Observability stack (Jaeger, Prometheus, Grafana, Loki) via Docker Compose
- .NET 9.0 target framework
- Comprehensive test project structure with 100% coverage target across all layers
- Copilot instruction files per layer (Domain, Application, API, Adapters, Persistence, Testing, C#, Documentation)
- AI agent definitions for TDD workflow (product owner, architect, tester, developer) and automated fixes
- Multi-model concurrent reviewer agents for code, documentation, agents, and prompts
- Review prompt templates for code, documentation, agent, and prompt compliance
- GitHub Actions workflows for CI, dependency vulnerability scanning, Markdown lint, Docker lint, and version check
