# emc.camus.secrets.dapr

Dapr secret provider adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Architecture Guide](../../../../docs/architecture.md)

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

```csharp
using emc.camus.secrets.dapr;

// Add Dapr secrets to DI container
builder.AddDaprSecrets();

var app = builder.Build();

// Force secret provider initialization (fail-fast pattern)
app.UseDaprSecrets();

app.Run();
```

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "DaprSecretProvider": {
    "DaprHttpEndpoint": "http://localhost:3500",
    "SecretStoreName": "localsecretstore"
  }
}
```

### 3. Use in Your Code

```csharp
public class MyService
{
    private readonly ISecretProvider _secretProvider;
    
    public MyService(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }
    
    public async Task DoSomethingAsync()
    {
        var apiKey = await _secretProvider.GetSecretAsync("XApiKey");
        // Use the secret
    }
}
```

---

## ⚙️ Configuration

### Development (Local Secret Store)

For development, configure a local file-based secret store:

**1. Dapr Component** (`src/Infrastructure/dapr/dapr-secret-component.yml`):

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

```bash
cd src/Infrastructure/dapr
dapr run --app-id camus-app \
  --dapr-http-port 3500 \
  --dapr-grpc-port 50001 \
  --resources-path .
```

> **📖 Full Guide:** See [Dapr Components README](../../../Infrastructure/dapr/README.md) for detailed Dapr setup.

### Production (Azure Key Vault)

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
  - name: azureTenantId
    value: "your-tenant-id"
  # Use managed identity in production
```

Update `appsettings.Production.json`:

```json
{
  "DaprSecretProvider": {
    "DaprHttpEndpoint": "http://localhost:3500",
    "SecretStoreName": "azurekeyvault"
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

Mock the interface in tests:

```csharp
var mockSecretProvider = new Mock<ISecretProvider>();
mockSecretProvider
    .Setup(x => x.GetSecretAsync("XApiKey"))
    .ReturnsAsync("test-api-key");

var service = new MyService(mockSecretProvider.Object);
```

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
