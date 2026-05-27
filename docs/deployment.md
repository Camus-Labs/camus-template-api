# Deployment Guide

## Production Deployment

### Prerequisites

- Docker Engine 20.10+
- Container registry access (Docker Hub, ACR, etc.)
- SSL/TLS certificates (for HTTPS)

## Docker Deployment

### 1. Build Production Image

Build the production Docker image using the `Dockerfile` at the repository root.

### 2. Run with Docker Compose

Start the production stack using the `docker-compose.prod.yml` file in detached mode.

**Services started:**

- API (port 80)
- OpenTelemetry Collector
- Network: `camus-prod`

### 3. Environment Configuration

Override configuration via environment variables using the `__` separator. See the
[Persistence Adapter README](../src/Adapters/emc.camus.persistence.postgresql/README.md)
for the full `DatabaseSettings` and `DataPersistenceSettings` JSON
structure.

Secrets (JWT private key, API key, database passwords) are managed through Dapr secret stores — see the
[Secrets Adapter README](../src/Adapters/emc.camus.secrets.dapr/README.md) for details.

## Azure Container Apps

### Quick Deploy

Use the [Azure Container Apps documentation](https://learn.microsoft.com/en-us/azure/container-apps/) to deploy
the production image. The deployment requires:

1. A resource group and Container Apps environment
2. The container image published to your registry
3. Ingress configured for external traffic on port 80
4. Environment variables mapped to the JSON settings structure above
5. Secret references for database credentials

### Configure Scaling

Configure HTTP-based autoscaling with a concurrency target of 50 requests per replica, scaling between 1 and 10
replicas. See [Azure Container Apps scaling rules](https://learn.microsoft.com/en-us/azure/container-apps/scale-app)
for configuration details.

## Monitoring

### Prometheus Metrics

Metrics are exported to the OpenTelemetry Collector via OTLP gRPC (port 4317). The Collector's Prometheus exporter
serves the scrape endpoint — configure Prometheus to scrape the Collector, not the application directly. See the
[Observability Adapter README](../src/Adapters/emc.camus.observability.otel/README.md) for configuration details.

## Dapr Secret Store Configuration

The application uses Dapr for secret management. Configure appropriate secret stores for production.

### Azure Key Vault

Configure a Dapr Azure Key Vault component and update `appsettings.Production.json` with `SecretStoreName`
set to `"azurekeyvault"`.

See [Dapr Components README](../src/Infrastructure/dapr/README.md#-production-configuration) for the component
YAML template and [Secrets Adapter README](../src/Adapters/emc.camus.secrets.dapr/README.md) for the application
settings structure.

### AWS Secrets Manager

Configure a Dapr AWS Secrets Manager component and update `appsettings.Production.json` with `SecretStoreName`
set to `"awssecretsmanager"`.

See [Dapr Components README](../src/Infrastructure/dapr/README.md#-production-configuration) for the component
YAML template and [Secrets Adapter README](../src/Adapters/emc.camus.secrets.dapr/README.md) for the application
settings structure.

**Deployment Steps:**

1. Deploy Dapr components to your cluster/environment
2. Ensure Dapr sidecar is configured in your deployment
3. Configure managed identity or IAM roles for secret access
4. Update application settings with correct `SecretStoreName`

> **📖 Learn More:** See [Dapr Components Configuration](../src/Infrastructure/dapr/README.md) and
[Secrets Adapter Documentation](../src/Adapters/emc.camus.secrets.dapr/README.md) for detailed configuration.

---

## Security Checklist

- [ ] Use HTTPS in production (TLS 1.2+)
- [ ] Store secrets in Azure Key Vault or AWS Secrets Manager via Dapr
- [ ] Enable managed identity for Azure resources
- [ ] Configure CORS for specific origins only
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Use strong JWT signing keys (RSA 2048+)
- [ ] Enable rate limiting
- [ ] Configure firewall rules
- [ ] Regular security updates

## Rollback Strategy

To roll back a deployment, re-tag the previous known-good image as `latest` and recreate the containers using
`docker-compose up -d --force-recreate`. Maintain version tags (e.g., `v1.0.0`, `v1.1.0`) for each release to enable
quick rollback to any prior version.

## Troubleshooting

### Container won't start

Check container logs and inspect the container state using `docker logs` and `docker inspect` for the production
container name.

### Database connection issues

Verify PostgreSQL is reachable from the container by running `pg_isready` inside the container targeting the
database host.

### Performance issues

- Check resource limits in `docker-compose.prod.yml`
- Review OTLP-exported metrics via Prometheus/Grafana
- Enable Grafana dashboards for monitoring

## Resources

- [Docker Documentation](https://docs.docker.com)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps)
- [.NET Deployment Guide](https://learn.microsoft.com/aspnet/core/host-and-deploy)
