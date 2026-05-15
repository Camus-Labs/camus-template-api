# Observability Stack Configuration

Configuration files for the OpenTelemetry-based observability stack used in Camus development.

> **📖 Parent Documentation:** See [Main README](../../../README.md) and
[Architecture Guide](../../../docs/architecture.md) for context.

---

## 📁 Files in This Directory

- `otel-collector-config.yaml` - OpenTelemetry Collector configuration (development)
- `otel-collector-config.prod.yaml` - OpenTelemetry Collector configuration (production)
- `prometheus-config.yml` - Prometheus scrape targets
- `grafana-config.yaml` - Grafana datasource provisioning
- `loki-config.yaml` - Loki log aggregation server configuration

---

## 🔧 Component Overview

### OpenTelemetry Collector

Central telemetry pipeline that receives traces, metrics, and logs from your application via OTLP (OpenTelemetry
Protocol).

**Key Features:**

- Receives OTLP over gRPC (port 4317) and HTTP (port 4318)
- Tail sampling for intelligent trace retention
- Exports traces to Jaeger, metrics to Prometheus, logs to Loki
- Memory limits to prevent resource exhaustion

**Usage in Application:**

The [emc.camus.observability.otel](../../Adapters/emc.camus.observability.otel/README.md) adapter sends telemetry
to the collector.

### Prometheus

Metrics collection and storage system that scrapes the OpenTelemetry Collector's Prometheus endpoint.

**Configuration:**

- Scrapes metrics from `camus-otel-collector:8889` every 15 seconds
- Retention: 1 day (configurable)

### Jaeger

Distributed tracing UI for visualizing request flows across services.

**Configuration:**

- Receives traces from OpenTelemetry Collector via OTLP
- In-memory storage (500k traces max for development)
- UI available at `http://localhost:16686`

### Grafana

Unified visualization platform with pre-configured datasources for Prometheus, Jaeger, and Loki.

**Configuration:**

- Anonymous viewer access enabled for development
- Dark theme by default
- Three datasources: Prometheus (metrics), Jaeger (traces), Loki (logs)

### Loki

Log aggregation system that receives structured logs from the OpenTelemetry Collector.

**Configuration:**

- Receives logs via OTLP at `/otlp/v1/logs`
- Schema v13 with TSDB storage
- Filesystem-based storage for development

---

## Configuration

Each YAML file in this directory configures one observability component. See the file list above for the complete
inventory. For application-side OpenTelemetry settings (`appsettings.json` keys and exporter options), see the
[Observability Adapter README][obs-adapter-readme].

---

## 🚀 Running the Stack

### Prerequisites

- Docker Desktop (macOS)
- [Camus application](../../Api/emc.camus.api/) configured with observability

### Via Docker Compose (Recommended)

Run the observability stack using the VS Code task **`docker-compose-up-dev-no-api`** from the Command Palette
(**Tasks: Run Task**), or run the equivalent `docker-compose` command from the project root.

To run individual containers manually, refer to the service definitions in `docker-compose.dev.yml` for the
correct image versions, port mappings, volume mounts, and environment variables.

---

## 🌐 Service Endpoints

Once running, access the observability tools:

| Service | URL | Purpose |
| ------- | --- | ------- |
| **Jaeger UI** | <http://localhost:16686> | View distributed traces |
| **Prometheus UI** | <http://localhost:9090> | Query metrics and targets |
| **Grafana** | <http://localhost:3000> | Unified dashboards and exploration |
| **Loki API** | <http://localhost:3100> | Log query API (used by Grafana) |
| **OTLP gRPC** | <http://localhost:4317> | Application telemetry endpoint |
| **OTLP HTTP** | <http://localhost:4318> | Alternative HTTP endpoint |
| **OTel Collector Metrics** | <http://localhost:8889/metrics> | Collector's own Prometheus metrics |

---

## 🔗 Application Integration

### Configure Application

Add `OpenTelemetry` settings to `appsettings.json` with OTLP exporter endpoints for tracing, metrics, and logs.
See [Observability Adapter README][obs-adapter-readme] for the complete configuration structure and exporter options.

### Register in Program.cs

Call `builder.AddObservability(serviceName, serviceVersion, instanceId, environmentName)` to register telemetry,
then call `app.UseObservability()` to add the response trace-ID middleware.

For complete configuration details, see the [Observability Adapter README][obs-adapter-readme].

[obs-adapter-readme]: ../../Adapters/emc.camus.observability.otel/README.md

---

## 📊 Viewing Telemetry

### Traces in Jaeger

1. Open <http://localhost:16686>
2. Select `camus-api` service
3. Search for traces
4. Click a trace to see the full request flow with timings

### Metrics in Prometheus

1. Open <http://localhost:9090>
2. Query metrics like `http_server_request_duration_bucket`
3. View targets at <http://localhost:9090/targets>

### Logs in Grafana

1. Open <http://localhost:3000>
2. Navigate to **Explore**
3. Select **Loki** datasource
4. Query: `{service_name="camus-api"}` or `{app="camus-api"}`
5. Logs are automatically correlated with traces via `trace_id`

### Unified View in Grafana

1. Open <http://localhost:3000>
2. **Explore** tab with **Prometheus** for metrics
3. Switch to **Jaeger** for trace exploration
4. Switch to **Loki** for log queries
5. Use trace IDs to correlate across all three signals

---

## 🧹 Cleanup

Use the VS Code task **`docker-compose-down`** from the Command Palette (**Tasks: Run Task**) to stop and remove
all containers. To also remove persistent volumes, use **`docker-compose-down-clean-data`** instead.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| No traces in Jaeger | Application exporter not set to `"Otlp"` or Collector not forwarding to Jaeger |
| Prometheus targets DOWN | Collector not exposing metrics on port 8889 or network mismatch |
| Grafana datasource error | Datasource URL doesn't match container name and port in Docker network |
| Loki shows no logs | Collector pipeline missing `loki` exporter or Loki endpoint unreachable |
| Containers exit immediately | Port conflict — check `docker ps` for existing containers on the same ports |

---

## 🏭 Production Configuration

For production deployments:

- Use `otel-collector-config.prod.yaml` with appropriate exporters
- Configure persistent storage for Prometheus and Loki
- Set up authentication for Grafana
- Use managed services (Azure Monitor, Datadog, etc.) instead of self-hosted
- Adjust retention policies based on compliance requirements

See [Deployment Guide](../../../docs/deployment.md) for details.

---

## 📚 Related Documentation

- **[emc.camus.observability.otel Adapter](../../Adapters/emc.camus.observability.otel/README.md)**
  \- Application-side observability configuration
- **[Architecture Guide](../../../docs/architecture.md)** - Observability stack architecture
- **[Debugging Guide](../../../docs/debugging.md)** - Using observability for debugging
