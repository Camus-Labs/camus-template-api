# Dapr Components Configuration

Component configuration files for Dapr integration in the Camus API.

> **📖 Parent Documentation:** See [Main README](../../../README.md) and
[Architecture Guide](../../../docs/architecture.md) for context.

---

## 📁 Files in This Directory

- `dapr-secret-component.yml` - Local secret store component definition
- `secrets.json` - Development secrets (DO NOT commit production secrets)
- `dapr-readme.md` - Quick reference for Dapr commands

---

## 🔐 Secret Store Component

The `dapr-secret-component.yml` configures a local JSON-based secret store for development:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: localsecretstore
spec:
  type: secretstores.local.file
  version: v1
  metadata:
  - name: secretsFile
    value: ./secrets.json
```

**Usage in Application:**

The [emc.camus.secrets.dapr](../../Adapters/emc.camus.secrets.dapr/README.md) adapter uses this component to
retrieve secrets at runtime.

---

## 🚀 Running Dapr Sidecar

### Development (Local)

Run from this directory:

```bash
cd src/Infrastructure/dapr
dapr run --app-id camus-app --dapr-http-port 3500 --dapr-grpc-port 50001 --resources-path .
```

Or from project root:

```bash
dapr run --app-id camus-app --dapr-http-port 3500 --dapr-grpc-port 50001 --resources-path ./src/Infrastructure/dapr
```

**Why Fixed Ports?**

- `--dapr-http-port 3500` ensures consistent port matching application configuration
- `--dapr-grpc-port 50001` sets a predictable gRPC port
- Without these, Dapr uses random ports that won't match your application

### Test Secret Retrieval

```bash
# In another terminal, test Dapr secret access
curl http://localhost:3500/v1.0/secrets/localsecretstore/AccessKey
```

---

## 🔧 Configuration

### Development Secrets (`secrets.json`)

```json
{
  "AccessKey": "dev-access-key",
  "AccessSecret": "dev-access-secret",
  "XApiKey": "dev-api-key-12345",
  "RsaPrivateKeyPem": "<your-rsa-private-key-pem>"
}
```

**⚠️ Security Notes:**

- This file is for **local development only**
- Never commit real secrets to version control
- Add `secrets.json` to `.gitignore`
- Use Azure Key Vault, AWS Secrets Manager, or similar in production

---

## 🏭 Production Configuration

For production, configure Dapr to use a proper secret store:

### Azure Key Vault

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: azurekeyvault
spec:
  type: secretstores.azure.keyvault
  version: v1
  metadata:
  - name: vaultName
    value: "your-keyvault-name"
  - name: azureClientId
    value: "your-client-id"
  - name: azureClientSecret
    value: "your-client-secret"
  - name: azureTenantId
    value: "your-tenant-id"
```

### AWS Secrets Manager

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: awssecretsmanager
spec:
  type: secretstores.aws.secretmanager
  version: v1
  metadata:
  - name: region
    value: "us-east-1"
  - name: accessKey
    value: "your-access-key"
  - name: secretKey
    value: "your-secret-key"
```

> **📖 Learn More:** See
[Dapr Secret Store Documentation](https://docs.dapr.io/reference/components-reference/supported-secret-stores/)

---

## Integration

The [emc.camus.secrets.dapr](../../Adapters/emc.camus.secrets.dapr/README.md) adapter reads secrets from
the configured Dapr secret store. Register the adapter and run the Dapr sidecar alongside the application.
See the adapter README for the `appsettings.json` configuration that references the secret store name
defined in the component YAML.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `connection refused` on port 3500 | Dapr sidecar not running — start it with `dapr run` |
| Secret not found | Secret name mismatch between `appsettings.json` and `secrets.json` |
| Wrong secret store used | `SecretStoreName` in app settings doesn't match `metadata.name` in component YAML |
| Dapr not initialized | Run `dapr init` to install Dapr runtime and default containers |

---

## 🔗 Related Documentation

- **[Secrets Adapter README](../../Adapters/emc.camus.secrets.dapr/README.md)** - How the
  application consumes secrets
- **[Architecture Guide](../../../docs/architecture.md)** - Dapr's role in the system architecture
- **[Deployment Guide](../../../docs/deployment.md)** - Production Dapr configuration

---

## ✅ Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) - Dapr requires Docker
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/) - Dapr command-line tools
- Dapr initialized: `dapr init`

**Verify Installation:**

```bash
dapr --version
docker ps  # Should show Dapr containers (placement, redis, zipkin)
```
