# Deployment Guide

## Production Deployment

### Prerequisites

- Docker Engine 20.10+
- Container registry access (Docker Hub, ACR, etc.)
- SSL/TLS certificates (for HTTPS)

## Docker Deployment

### 1. Build Production Image

```bash
docker build -t camus-api:latest -f Dockerfile .
```

### 2. Run with Docker Compose

```bash
docker-compose -f docker-compose.prod.yml up -d
```

**Services started:**

- API (port 80)
- OpenTelemetry Collector
- Network: `camus-prod`

### 3. Environment Configuration

Override configuration via environment variables using the `__` separator:

```env
# Database
DatabaseSettings__Host=db
DatabaseSettings__Port=5432
DatabaseSettings__Database=camus
DatabaseSettings__UserSecretName=DBUser
DatabaseSettings__PasswordSecretName=DBSecret

# Persistence provider
DataPersistenceSettings__Provider=PostgreSQL

# Observability
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=your-key
```

Secrets (JWT private key, API key, DB passwords) are managed through Dapr secret stores —
see [Secrets Adapter README](../src/Adapters/emc.camus.secrets.dapr/README.md) for details.

## Azure Container Apps

### Quick Deploy

```bash
# Create resource group
az group create --name camus-rg --location eastus

# Create container app environment
az containerapp env create \
  --name camus-env \
  --resource-group camus-rg \
  --location eastus

# Deploy container app
az containerapp create \
  --name camus-api \
  --resource-group camus-rg \
  --environment camus-env \
  --image your-registry/camus-api:latest \
  --target-port 80 \
  --ingress external \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection=secretref:db-connection
```

### Configure Scaling

```bash
az containerapp update \
  --name camus-api \
  --resource-group camus-rg \
  --min-replicas 1 \
  --max-replicas 10 \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

## Monitoring

### Application Insights Integration

Set connection string in environment:

```env
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=https://xxx
```

### Prometheus Metrics

Expose metrics endpoint:

```yaml
- name: metrics
  containerPort: 80
  protocol: TCP
```

Scrape configuration:

```yaml
- job_name: 'camus-api'
  static_configs:
    - targets: ['camus-api:80']
  metrics_path: '/metrics'
```

## Dapr Secret Store Configuration

The application uses Dapr for secret management. Configure appropriate secret stores for production.

### Azure Key Vault

Configure a Dapr Azure Key Vault component and update `appsettings.Production.json` with `SecretStoreName`
set to `"azurekeyvault"`.

See [Dapr Components README — Production
Configuration](../src/Infrastructure/dapr/README.md#-production-configuration)
for the component YAML template and
[Secrets Adapter README](../src/Adapters/emc.camus.secrets.dapr/README.md)
for the application settings structure.

### AWS Secrets Manager

Configure a Dapr AWS Secrets Manager component and update `appsettings.Production.json` with `SecretStoreName`
set to `"awssecretsmanager"`.

See [Dapr Components README — Production
Configuration](../src/Infrastructure/dapr/README.md#-production-configuration)
for the component YAML template and
[Secrets Adapter README](../src/Adapters/emc.camus.secrets.dapr/README.md)
for the application settings structure.

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

```bash
# Tag current version
docker tag camus-api:latest camus-api:v1.0.0

# Deploy new version
docker tag camus-api:latest camus-api:v1.1.0
docker-compose up -d

# Rollback if needed
docker tag camus-api:v1.0.0 camus-api:latest
docker-compose up -d --force-recreate
```

## Troubleshooting

### Container won't start

```bash
# Check logs
docker logs camus-api-prod

# Inspect container
docker inspect camus-api-prod
```

### Database connection issues

```bash
# Verify PostgreSQL is reachable from the container
docker exec -it camus-api-prod pg_isready -h db
```

### Performance issues

- Check resource limits in `docker-compose.prod.yml`
- Review Application Insights performance metrics
- Enable Grafana dashboards for monitoring

## Resources

- [Docker Documentation](https://docs.docker.com)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps)
- [.NET Deployment Guide](https://learn.microsoft.com/aspnet/core/host-and-deploy)
