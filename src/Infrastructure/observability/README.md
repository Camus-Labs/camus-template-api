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

## 🚀 Running the Stack

### Prerequisites

- Docker Desktop (macOS)
- [Camus application](../../Api/emc.camus.api/) configured with observability

### Via Docker Compose (Recommended)

Run the complete observability stack using the docker-compose task:

```bash
# From project root
docker-compose -f docker-compose.dev.yml up postgres otel-collector jaeger loki prometheus grafana -d
```

Or use the VS Code task:

- **Command Palette** → **Tasks: Run Task** → `docker-compose-up-observability`

### Manual Docker Commands

If you prefer running containers individually:

```bash
# Create network
docker network create camus-observability || true

# Jaeger (Tracing UI)
docker run -d --name camus-jaeger --network camus-observability \
  -p 16686:16686 -p 4317:4317 \
  -e SPAN_STORAGE_TYPE=memory \
  -e MEMORY_MAX_TRACES=500000 \
  --memory=1g --memory-swap=1g \
  jaegertracing/all-in-one:latest

# OpenTelemetry Collector
docker run -d --name camus-otel-collector --network camus-observability \
  -p 4317:4317 -p 4318:4318 -p 8889:8889 \
  -v "$(pwd)/src/Infrastructure/observability/otel-collector-config.yaml:/etc/otelcol/config.yaml" \
  otel/opentelemetry-collector-contrib:latest \
  --config /etc/otelcol/config.yaml

# Loki (Log Aggregation)
docker run -d --name camus-loki --network camus-observability \
  -p 3100:3100 \
  -v "$(pwd)/src/Infrastructure/observability/loki-config.yaml:/etc/loki/config.yml:ro" \
  grafana/loki:latest \
  -config.file=/etc/loki/config.yml

# Prometheus (Metrics)
docker run -d --name camus-prometheus --network camus-observability \
  -p 9090:9090 \
  -v "$(pwd)/src/Infrastructure/observability/prometheus-config.yml:/etc/prometheus/prometheus.yml:ro" \
  prom/prometheus:latest \
  --config.file=/etc/prometheus/prometheus.yml \
  --storage.tsdb.retention.time=1d \
  --storage.tsdb.retention.size=1GB

# Grafana (Dashboards)
docker run -d --name camus-grafana --network camus-observability \
  -p 3000:3000 \
  -e GF_USERS_DEFAULT_THEME=dark \
  -e GF_AUTH_ANONYMOUS_ENABLED=true \
  -e GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer \
  -v "$(pwd)/src/Infrastructure/observability/grafana-config.yaml:/etc/grafana/provisioning/datasources/grafana-config.yaml:ro" \
  grafana/grafana:latest
```

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

In `appsettings.json` of your API project:

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

### Stop Containers

```bash
docker stop camus-otel-collector camus-prometheus camus-jaeger camus-grafana camus-loki
```

Or via Docker Compose:

```bash
docker-compose -f docker-compose.dev.yml down
```

### Remove Containers

```bash
docker rm -f camus-otel-collector camus-prometheus camus-jaeger camus-grafana camus-loki
```

### Remove Network

```bash
docker network rm camus-observability
```

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
