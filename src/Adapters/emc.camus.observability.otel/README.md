# emc.camus.observability.otel

OpenTelemetry-based observability adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

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

Call `builder.AddObservability(serviceName, serviceVersion, instanceId, environmentName)` to register tracing, metrics, and structured logging. Then call `app.UseObservability()` to add the response trace-ID middleware.

See `ObservabilitySetupExtensions` in this adapter for the full registration API and `Program.cs` for the wiring example.

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
      "OtlpEndpoint": "http://localhost:4317",
      "DisabledMetrics": [
        "http.server.request.duration",
        "http.server.active_requests"
      ],
      "DisabledMeters": [ ".infrastructure" ]
    },
    "Logs": {
      "Console": {
        "Enabled": true,
        "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] (trace_id={trace_id} span_id={span_id}) {Message:lj}{NewLine}{Exception}"
      },
      "Exporter": "Otlp",
      "OtlpEndpoint": "http://localhost:4317"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
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

Inject `IActivitySourceWrapper` to create custom spans for business-critical operations. Call `StartActivity(name, kind)` to open a span, set tags for context, and set error status on failures. The wrapper is disposed automatically at the end of the `using` block.

See `IActivitySourceWrapper` in the Application layer for the interface contract, and `ObservabilitySetupExtensions.cs` for registration details.

---

## 📝 Structured Logging

The adapter configures Serilog for structured logging. Logs are automatically enriched with trace context (trace ID and span ID). Use Serilog’s message template syntax with named placeholders for structured properties.

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
Trace-Id: 4bf92f3577b34da6a3ce929d0e0e4736
Content-Type: application/json
```

**Benefits:**

- Correlate client requests with server logs
- Debug issues by providing trace ID to support
- Track requests across distributed systems

---

## 🏭 Production Considerations

### Sampling

Implement sampling to reduce overhead in high-traffic scenarios. Configure a `TraceIdRatioBasedSampler` (e.g., 0.1 for 10% of traces) via the OpenTelemetry tracing builder.

### Resource Attributes

Enrich telemetry with environment information by passing service name, version, instance ID, and environment name to `AddObservability()`. See `ObservabilitySetupExtensions.cs` for parameter details.

---

## 🔗 Integration

The adapter registers all observability services via two extension methods in `ObservabilitySetupExtensions.cs`:

1. **`builder.AddObservability(serviceName, serviceVersion, instanceId, environmentName)`** — Configures OpenTelemetry tracing, metrics, and Serilog structured logging based on `appsettings.json`. Registers the selected exporters and enriches telemetry with service resource attributes.
2. **`app.UseObservability()`** — Adds middleware that copies the current trace ID into the `Trace-Id` response header for request correlation.

Call these in `Program.cs` early in the pipeline, before authentication and routing middleware.

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| No traces in Jaeger | Exporter set to `"Console"` instead of `"Otlp"`, or OTLP endpoint unreachable |
| Missing `Trace-Id` response header | `app.UseObservability()` not called in the pipeline |
| High memory usage from metrics | Too many custom meters or disabled-metrics list not filtering noisy meters |
| Logs not appearing in Loki | Serilog OTLP sink not configured or Collector not forwarding to Loki |
| `ActivitySource` spans not visible | Custom activity source name not registered in the tracing builder |

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
