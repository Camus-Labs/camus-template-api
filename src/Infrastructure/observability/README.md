# Observability Stack Configuration

Configuration files for the local observability stack used during development with Camus API.

> **📖 Parent Documentation:** See [Main README](../../../README.md) and [Architecture Guide](../../../docs/architecture.md) for observability architecture overview.

---

## 📁 Files in This Directory

- `otel-collector-config.yaml` - OpenTelemetry Collector configuration (development)
- `otel-collector-config.prod.yaml` - Production OpenTelemetry Collector configuration
- `prometheus-config.yml` - Prometheus scrape targets and configuration
- `grafana-config.yaml` - Grafana data source provisioning
- `loki-config.yaml` - Loki server configuration with retention policies

---

## 🏗️ Observability Stack Components

### OpenTelemetry Collector
**Central telemetry pipeline** for traces, metrics, and logs

- Receives: OTLP (gRPC/HTTP), Prometheus metrics
- Exports: Jaeger (traces), Prometheus (metrics), Loki (logs), Console (dev)
- Port 4317: OTLP gRPC
- Port 4318: OTLP HTTP/Protobuf
- Port 8889: Prometheus metrics from collector itself

### Jaeger
**Distributed tracing UI** for visualizing request flows

- Port 16686: Jaeger UI
- Port 14250: Receives traces from OTel Collector
- Storage: In-memory (dev), configurable for production

### Prometheus
**Metrics collection and storage**

- Port 9090: Prometheus UI and query interface
- Scrapes: OTel Collector metrics endpoint
- Retention: 1 day / 1GB (configurable)

### Grafana
**Visualization and dashboards**

- Port 3000: Grafana UI
- Pre-configured data sources: Prometheus, Jaeger, Loki
- Auth: Anonymous viewer mode enabled (dev only)

### Loki
**Log aggregation**

- Port 3100: Loki API
- Receives: Logs from OTel Collector via OTLP
- Retention: 24 hours (dev configuration)

---

## 🚀 Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) (macOS)
- VS Code with workspace opened

### Run via VS Code Tasks (Recommended)

Open Command Palette (`Cmd+Shift+P`) → **"Tasks: Run Task"**

**Available Tasks:**
- `start-observability-stack` - Creates network and starts all components
- `create-docker-network` - Create shared network
- `run-jaeger-detached` - Start Jaeger only
- `run-otel-collector-detached` - Start OTel Collector only
- `run-prometheus-detached` - Start Prometheus only
- `run-grafana-detached` - Start Grafana only
- `run-loki-detached` - Start Loki only

> **💡 Tip:** Use `start-observability-stack` for first-time setup. It orchestrates all components in correct order.

### Manual Docker Commands (Alternative)

Run from project root:

```bash
# Create shared network
docker network create camus-observability || true

# Start Jaeger
docker run -d --name camus-jaeger --network camus-observability \
  -p 16686:16686 -p 14250:14250 \
  -e SPAN_STORAGE_TYPE=memory -e MEMORY_MAX_TRACES=500000 \
  --memory=1g --memory-swap=1g \
  jaegertracing/all-in-one:latest

# Start OpenTelemetry Collector
docker run -d --name camus-otel-collector --network camus-observability \
  -p 4317:4317 -p 4318:4318 -p 8889:8889 \
  -v "$(pwd)/src/Infrastructure/observability/otel-collector-config.yaml:/etc/otelcol/config.yaml" \
  otel/opentelemetry-collector-contrib:latest \
  --config /etc/otelcol/config.yaml

# Start Prometheus
docker run -d --name camus-prometheus --network camus-observability \
  -p 9090:9090 \
  -v "$(pwd)/src/Infrastructure/observability/prometheus-config.yml:/etc/prometheus/prometheus.yml:ro" \
  prom/prometheus:latest \
  --config.file=/etc/prometheus/prometheus.yml \
  --storage.tsdb.retention.time=1d \
  --storage.tsdb.retention.size=1GB

# Start Grafana
docker run -d --name camus-grafana --network camus-observability \
  -p 3000:3000 \
  -e GF_USERS_DEFAULT_THEME=dark \
  -e GF_AUTH_ANONYMOUS_ENABLED=true \
  -e GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer \
  -v "$(pwd)/src/Infrastructure/observability/grafana-config.yaml:/etc/grafana/provisioning/datasources/grafana-config.yaml:ro" \
  grafana/grafana:latest

# Start Loki
docker run -d --name camus-loki --network camus-observability \
  -p 3100:3100 \
  -v "$(pwd)/src/Infrastructure/observability/loki-config.yaml:/etc/loki/config.yml:ro" \
  grafana/loki:latest \
  -config.file=/etc/loki/config.yml
```

---

## 🔗 Access Endpoints

Once running, access components at:

| Component        | URL                              | Purpose                          |
|------------------|----------------------------------|----------------------------------|
| Jaeger UI        | http://localhost:16686           | Trace visualization              |
| Prometheus UI    | http://localhost:9090            | Metrics queries                  |
| Grafana          | http://localhost:3000            | Dashboards (no login required)   |
| OTel Collector   | http://localhost:4317 (gRPC)     | OTLP traces/metrics/logs         |
| OTel Collector   | http://localhost:4318 (HTTP)     | OTLP HTTP/Protobuf               |
| Loki API         | http://localhost:3100            | Log queries (via Grafana)        |
| Collector Metrics| http://localhost:8889/metrics    | Prometheus format                |

---

## ⚙️ Application Configuration

Configure your application to send telemetry to the collector:

### appsettings.json

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
    }
  }
}
```

### Using HTTP/Protobuf (Port 4318)

If your application uses the HTTP endpoint instead of gRPC:

```csharp
options.Protocol = OtlpTransportProtocol.HttpProtobuf;
options.Endpoint = new Uri("http://localhost:4318");
```

> **📖 Application Integration:** See [emc.camus.observability.otel README](../../Adapters/emc.camus.observability.otel/README.md) for detailed usage.

---

## 📊 Telemetry Flow

```text
Your Application
       ↓ (OTLP gRPC/HTTP)
OpenTelemetry Collector
       ├─→ Jaeger (traces)
       ├─→ Prometheus (metrics)
       └─→ Loki (logs)
              ↓
           Grafana (visualization)
```

**Why Use the Collector?**
- Single endpoint for all telemetry
- Flexible routing and processing
- Easy to change backends without code changes
- Sampling, filtering, and enrichment capabilities

---

## 🏭 Production Configuration

For production, use `otel-collector-config.prod.yaml` with:

- Azure Monitor exporter for Application Insights
- Proper authentication and encryption
- Sampling strategies for high-volume scenarios
- Resource detection and enrichment

**Key Differences (Dev vs Prod):**
- **Dev**: Console exporters, in-memory storage, no auth
- **Prod**: Cloud exporters, persistent storage, authentication, sampling

---

## 🔗 Related Documentation

- **[Observability Adapter README](../../Adapters/emc.camus.observability.otel/README.md)** - Application-side OpenTelemetry configuration
- **[Architecture Guide](../../../docs/architecture.md)** - Observability architecture overview
- **[Debugging Guide](../../../docs/debugging.md)** - Using observability in development

---

## 🛑 Stopping the Stack

```bash
# Stop and remove all containers
docker stop camus-jaeger camus-otel-collector camus-prometheus camus-grafana camus-loki
docker rm camus-jaeger camus-otel-collector camus-prometheus camus-grafana camus-loki

# Remove network
docker network rm camus-observability
```
