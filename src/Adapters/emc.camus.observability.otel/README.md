# emc.camus.observability.otel

OpenTelemetry-based observability adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Architecture Guide](../../../../docs/architecture.md)

---

## 📋 Overview

This adapter provides comprehensive observability through OpenTelemetry, including distributed tracing, metrics collection, and structured logging with Serilog. It integrates seamlessly with the OpenTelemetry Collector for flexible telemetry routing.

---

## ✨ Features

- 📊 **Distributed Tracing** - Request flow visualization across services
- 📈 **Metrics Collection** - ASP.NET Core, HTTP client, and runtime metrics
- 📝 **Structured Logging** - Serilog with OpenTelemetry integration
- 🔄 **Multiple Exporters** - Console, OTLP, Jaeger, Zipkin, Prometheus
- 🎯 **Activity Source Wrapper** - Easy manual instrumentation
- ⚙️ **Configurable** - Settings via `appsettings.json`

---

## 🚀 Usage

### 1. Register in Program.cs

```csharp
using emc.camus.observability.otel;

const string SERVICE_NAME = "camus-api";
const string SERVICE_VERSION = "1.0.0";
const string INSTANCE_ID = Environment.MachineName;
const string ENV_NAME = builder.Environment.EnvironmentName;

// Add observability (tracing, metrics, logging)
builder.AddObservability(SERVICE_NAME, SERVICE_VERSION, INSTANCE_ID, ENV_NAME);

var app = builder.Build();

// Add response trace ID middleware
app.UseObservability();

app.Run();
```

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Otlp",
      "OtlpEndpoint": "http://localhost:4317"
    },
    "Metrics": {
      "Exporter": "Otlp",
      "OtlpEndpoint": "http://localhost:4317"
    },
    "Logs": {
      "Exporter": "Otlp",
      "OtlpEndpoint": "http://localhost:4317"
    }
  }
}
```

---

## 📊 Supported Exporters

### Tracing Exporters

| Exporter | Value | Use Case |
| -------- | ----- | -------- |
| OTLP | `"Otlp"` | Recommended - sends to OpenTelemetry Collector |
| Jaeger | `"Jaeger"` | Direct export to Jaeger (port 6831) |
| Zipkin | `"Zipkin"` | Direct export to Zipkin |
| Console | `"Console"` | Development debugging |

### Metrics Exporters

| Exporter | Value | Use Case |
| -------- | ----- | -------- |
| OTLP | `"Otlp"` | Recommended - sends to OpenTelemetry Collector |
| Prometheus | `"Prometheus"` | Direct Prometheus scraping (port 9464) |
| Console | `"Console"` | Development debugging |

### Logs Exporters

| Exporter | Value        | Use Case                                         |
|----------|--------------|--------------------------------------------------|
| OTLP     | `"Otlp"`     | Recommended - sends to OpenTelemetry Collector   |
| Console  | `"Console"`  | Development debugging                            |

> **💡 Recommendation:** Use OTLP exporter to send all telemetry to the OpenTelemetry Collector, which handles routing to Jaeger, Prometheus, Loki, etc. This provides maximum flexibility without code changes.

---

## 🏗️ Telemetry Flow

```text
Your Application (with this adapter)
       │
       ├─→ Traces ──┐
       ├─→ Metrics ─┤
       └─→ Logs ────┤
                    │
       (OTLP gRPC/HTTP on port 4317/4318)
                    ↓
         OpenTelemetry Collector
                    │
       ├────────────┼────────────┐
       ↓            ↓            ↓
    Jaeger      Prometheus    Loki
 (Tracing UI)  (Metrics)    (Logs)
       └────────────┴────────────┘
                    ↓
                 Grafana
             (Visualization)
```

> **📖 Stack Configuration:** See [Observability Components README](../../../Infrastructure/observability/README.md) for setting up the observability stack.

---

## 🔧 Configuration Examples

### Development (Console Output)

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Console"
    },
    "Metrics": {
      "Exporter": "Console"
    }
  }
}
```

### Production (Azure Monitor)

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Otlp",
      "OtlpEndpoint": "http://otel-collector:4317"
    }
  },
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=..."
}
```

### Direct Jaeger Export

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Jaeger",
      "JaegerEndpoint": "http://localhost:6831"
    }
  }
}
```

---

## 🎯 Manual Instrumentation

Use the `IActivitySourceWrapper` for custom spans:

```csharp
public class ProductService
{
    private readonly IActivitySourceWrapper _activitySource;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(IActivitySourceWrapper activitySource, ILogger<ProductService> logger)
    {
        _activitySource = activitySource;
        _logger = logger;
    }
    
    public async Task<Product> GetProductAsync(int id)
    {
        using var activity = _activitySource.StartActivity("GetProduct", ActivityKind.Internal);
        activity?.SetTag("product.id", id);
        
        try
        {
            _logger.LogInformation("Fetching product {ProductId}", id);
            
            var product = await _repository.GetByIdAsync(id);
            
            activity?.SetTag("product.found", product != null);
            
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found", id);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error fetching product {ProductId}", id);
            throw;
        }
    }
}
```

---

## 📝 Structured Logging

The adapter configures Serilog for structured logging:

```csharp
// Logs are automatically enriched with trace context
_logger.LogInformation("User {UserId} created order {OrderId}", userId, orderId);

// Trace ID and span ID are automatically included
// Output: [INF] [TraceId: abc123] [SpanId: def456] User 42 created order 789
```

### Log Levels

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.AspNetCore": "Information"
      }
    }
  }
}
```

---

## 🔗 Response Trace ID Middleware

The `UseObservability()` method adds middleware that includes the trace ID in response headers:

```http
HTTP/1.1 200 OK
X-Trace-Id: 4bf92f3577b34da6a3ce929d0e0e4736
Content-Type: application/json
```

**Benefits:**

- Correlate client requests with server logs
- Debug issues by providing trace ID to support
- Track requests across distributed systems

---

## 🏭 Production Considerations

### Sampling

Implement sampling to reduce overhead in high-traffic scenarios:

```csharp
builder.Services.AddOpenTelemetryTracing(options =>
{
    options.SetSampler(new TraceIdRatioBasedSampler(0.1)); // Sample 10% of traces
});
```

### Resource Attributes

Enrich telemetry with environment information:

```csharp
builder.AddObservability(
    serviceName: "camus-api",
    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
    instanceId: Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName,
    environmentName: builder.Environment.EnvironmentName
);
```

---

## 🔗 Related Documentation

- **[Observability Stack README](../../../Infrastructure/observability/README.md)** - Infrastructure setup (Jaeger, Prometheus, Grafana)
- **[Architecture Guide](../../../../docs/architecture.md)** - Observability architecture overview
- **[Debugging Guide](../../../../docs/debugging.md)** - Using observability in development
- **[OpenTelemetry Documentation](https://opentelemetry.io/docs/instrumentation/net/)** - Official .NET instrumentation guide

---

## 📦 Dependencies

- `OpenTelemetry.Extensions.Hosting` - OpenTelemetry integration
- `OpenTelemetry.Instrumentation.AspNetCore` - ASP.NET Core instrumentation
- `OpenTelemetry.Instrumentation.Http` - HTTP client instrumentation
- `OpenTelemetry.Instrumentation.Runtime` - .NET runtime metrics
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - OTLP exporter
- `OpenTelemetry.Exporter.Jaeger` - Jaeger exporter
- `OpenTelemetry.Exporter.Zipkin` - Zipkin exporter
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` - Prometheus exporter
- `Serilog.AspNetCore` - Structured logging
- `Serilog.Sinks.OpenTelemetry` - Serilog OTLP sink

---

## 💡 Best Practices

- ✅ Use OTLP exporter with OpenTelemetry Collector
- ✅ Add custom spans for important business operations
- ✅ Include relevant tags (user ID, order ID, etc.)
- ✅ Use structured logging with context
- ✅ Implement sampling in production
- ✅ Monitor collector health and performance
- ❌ Don't log sensitive information (passwords, tokens)
- ❌ Don't create excessive spans (performance impact)
- ❌ Don't ignore error status codes on activities
