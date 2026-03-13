# emc.camus.secrets.dapr

Dapr secret provider adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

---

## 📋 Overview

This adapter implements the `ISecretProvider` interface from the Application layer using Dapr's secret management capabilities. It enables secure retrieval of secrets from various secret stores (local files, Azure Key Vault, AWS Secrets Manager, etc.) without code changes.

---

## ✨ Features

- 🔐 **Dapr Integration** - Uses Dapr secret store building blocks
- 🔄 **Interface-Based** - Implements `ISecretProvider` from Application layer
- ⚙️ **Configurable** - Settings via `appsettings.json`
- 🚀 **Fail-Fast** - Validates secret access at startup
- 🎯 **Dependency Injection** - Seamless ASP.NET Core integration

---

## 🚀 Usage

### 1. Register in Program.cs

Call `builder.AddDaprSecrets()` to register the Dapr secret provider in DI, then call `app.UseDaprSecrets()` to force secret initialization at startup (fail-fast pattern).

See `DaprSecretsSetupExtensions` in this adapter for the full registration API.

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "DaprSecretProvider": {
    "BaseHost": "localhost",
    "HttpPort": "3500",
    "SecretStoreName": "default-secret-store",
    "SecretNames": ["AccessKey", "AccessSecret", "XApiKey", "RsaPrivateKeyPem"]
  }
}
```

### 3. Use in Your Code

Inject `ISecretProvider` from the Application layer via constructor injection. Call `GetSecretAsync(secretName)` to retrieve a secret value at runtime.

See `ISecretProvider` in `src/Application/emc.camus.application/Secrets/` for the interface contract.

---

## ⚙️ Configuration

### Development (Local Secret Store)

For development, configure a local file-based secret store:

**1. Dapr Component** (`src/Infrastructure/dapr/dapr-secret-component.yml`):

Configure a local file-based secret store component. See [Dapr Components README](../../../Infrastructure/dapr/README.md) for the component file format and examples.

**2. Secrets File** (`src/Infrastructure/dapr/secrets.json`):

```json
{
  "AccessKey": "dev-access-key",
  "AccessSecret": "dev-access-secret",
  "XApiKey": "dev-api-key-12345",
  "RsaPrivateKeyPem": "<your-rsa-private-key>"
}
```

**3. Run Dapr Sidecar**:

Start the Dapr sidecar from the `src/Infrastructure/dapr` directory using `dapr run` with the `--resources-path .` flag, specifying the app ID, HTTP port (3500), and gRPC port (50001). See [Dapr Components README](../../../Infrastructure/dapr/README.md) for the complete run command.

> **📖 Full Guide:** See [Dapr Components README](../../../Infrastructure/dapr/README.md) for detailed Dapr setup.

### Production

For production deployments, configure Dapr to use proper secret stores like Azure Key Vault or AWS Secrets Manager.

> **📖 Production Configuration:** See [Dapr Components README](../../../Infrastructure/dapr/README.md#production-configuration) for complete production secret store configurations (Azure Key Vault, AWS Secrets Manager, etc.).

Update your production settings to reference the appropriate secret store:

```json
{
  "DaprSecretProvider": {
    "BaseHost": "localhost",
    "HttpPort": "3500",
    "SecretStoreName": "azurekeyvault",
    "SecretNames": ["AccessKey", "AccessSecret", "XApiKey", "RsaPrivateKeyPem"]
  }
}
```

---

## 🏗️ Architecture

### Dependency Inversion

```text
┌─────────────────────────────────────┐
│    Application Layer (Interfaces)   │
│         ISecretProvider              │
└──────────────┬──────────────────────┘
               │ depends on
               ↓
┌──────────────────────────────────────┐
│      Adapter Layer (Implementation)  │
│       DaprSecretProvider             │
│  (implements ISecretProvider)        │
└──────────────┬───────────────────────┘
               │ uses
               ↓
┌──────────────────────────────────────┐
│         Dapr Runtime                 │
│   (Secret Store Building Block)     │
└──────────────────────────────────────┘
```

**Benefits:**

- Application layer doesn't depend on Dapr
- Easy to swap secret providers (Dapr → Azure SDK → AWS SDK)
- Testable with mocked `ISecretProvider`

---

## 🧪 Testing

Mock the `ISecretProvider` interface in unit tests to return predetermined secret values without requiring a running Dapr sidecar.

---

## 🔗 Integration

The adapter registers the Dapr secret provider via two extension methods in `DaprSecretsSetupExtensions.cs`:

1. **`builder.AddDaprSecrets()`** — Reads `DaprSecretProvider` settings from configuration and registers `DaprSecretProvider` as the `ISecretProvider` singleton.
2. **`app.UseDaprSecrets()`** — Resolves `ISecretProvider` from DI and calls `Initialize()` to fetch all configured secrets at startup (fail-fast pattern).

Call `AddDaprSecrets()` before any adapter that depends on `ISecretProvider` (e.g., JWT, API Key, migrations).

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `DaprSecretProvider configuration is missing` | Missing `DaprSecretProvider` section in `appsettings.json` |
| `Failed to retrieve secret 'X'` | Secret name mismatch or Dapr sidecar not running |
| Connection refused on port 3500 | Dapr sidecar not started — run `dapr run` or check container setup |
| Secrets empty in production | Secret store component name doesn't match `SecretStoreName` setting |
| Startup crash with secret error | `UseDaprSecrets()` called before Dapr sidecar is ready |

---

## 🔗 Related Documentation

- **[Application Secrets Interface](../../../Application/emc.camus.application/Secrets/)** - `ISecretProvider` definition
- **[Dapr Components Configuration](../../../Infrastructure/dapr/README.md)** - Dapr setup and configuration
- **[Architecture Guide](../../../../docs/architecture.md)** - Secrets management architecture
- **[Deployment Guide](../../../../docs/deployment.md)** - Production secret management

---

## ⚠️ Security Best Practices

- ✅ Never commit `secrets.json` to version control
- ✅ Use managed identities in production (Azure, AWS)
- ✅ Rotate secrets regularly
- ✅ Use different secrets per environment
- ✅ Enable audit logging on secret stores
- ❌ Never log secret values

---

## 📦 Dependencies

- `Dapr.Client` - Dapr SDK for .NET
- `emc.camus.application` - Application interfaces
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
