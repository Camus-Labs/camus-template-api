# Observability Components for Camus

This folder contains the configuration files for the local observability stack used during development:

- `otel-collector-config.yaml`: OpenTelemetry Collector receivers/exporters/processors
- `prometheus-config.yml`: Prometheus scrape configuration
- `grafana-config.yaml`: Grafana provisioning for data sources
- `loki-config.yaml`: Loki server config with dev retention

The stack runs via VS Code tasks with Docker containers and a shared network.

## Prerequisites

- Docker Desktop (macOS)
- VS Code with this workspace opened

## Run via VS Code Tasks (recommended)

Use the built-in tasks to start everything quickly. From the Command Palette, run: “Tasks: Run Task”.

- `start-observability-stack`: Creates network and starts Jaeger, OTel Collector, Prometheus, and Grafana in sequence.
- Individual tasks if needed:
  - `create-docker-network`
  - `run-jaeger-detached`
  - `run-otel-collector-detached`
  - `run-prometheus-detached`
  - `run-grafana-detached`

All tasks are defined in `.vscode/tasks.json` and already point to these files under `src/Adapters/emc.observability.components`.

### Endpoints

- OTLP gRPC: `http://localhost:4317`
- OTLP HTTP/Protobuf: `http://localhost:4318`
- OTel Collector metrics (Prometheus): `http://localhost:8889`
- Prometheus UI: `http://localhost:9090`
- Jaeger UI: `http://localhost:16686`
- Grafana UI (anonymous viewer): `http://localhost:3000`
- Loki API: `http://localhost:3100` (Grafana datasource)

## Manual Docker commands (optional)

If you prefer the terminal, these mirror the tasks. Run from the repo root:

```bash
# Create network once
docker network create camus-observability || true

# Jaeger
docker run -d --name camus-jaeger --network camus-observability \
  -p 16686:16686 -p 14250:14250 \
  -e SPAN_STORAGE_TYPE=memory -e MEMORY_MAX_TRACES=500000 \
  --memory=1g --memory-swap=1g jaegertracing/all-in-one:latest

# OpenTelemetry Collector
docker run -d --name camus-otel-collector --network camus-observability \
  -p 4317:4317 -p 4318:4318 -p 8889:8889 \
  -v "$(pwd)/src/Adapters/emc.observability.components/otel-collector-config.yaml:/etc/otelcol/config.yaml" \
  otel/opentelemetry-collector-contrib:latest --config /etc/otelcol/config.yaml

# Prometheus
docker run -d --name camus-prometheus --network camus-observability \
  -p 9090:9090 \
  -v "$(pwd)/src/Adapters/emc.observability.components/prometheus-config.yml:/etc/prometheus/prometheus.yml:ro" \
  prom/prometheus:latest --config.file=/etc/prometheus/prometheus.yml \
  --storage.tsdb.retention.time=1d --storage.tsdb.retention.size=1GB

# Grafana
docker run -d --name camus-grafana --network camus-observability \
  -p 3000:3000 \
  -e GF_USERS_DEFAULT_THEME=dark \
  -e GF_AUTH_ANONYMOUS_ENABLED=true \
  -e GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer \
  -v "$(pwd)/src/Adapters/emc.observability.components/grafana-config.yaml:/etc/grafana/provisioning/datasources/grafana-config.yaml:ro" \
  grafana/grafana:latest

# Loki
docker run -d --name camus-loki --network camus-observability \
  -p 3100:3100 \
  -v "$(pwd)/src/Adapters/emc.observability.components/loki-config.yaml:/etc/loki/config.yml:ro" \
  grafana/loki:latest -config.file=/etc/loki/config.yml
```

## Configure your app

Set in `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Tracing": { "TracingExporter": "otlp", "OtlpEndpoint": "http://localhost:4317" },
    "Metrics": { "MetricsExporter": "otlp", "OtlpEndpoint": "http://localhost:4317" }
  }
}
```

If you use the OTLP HTTP/Protobuf endpoint (`4318`) in your app:

```csharp
options.Protocol = OtlpTransportProtocol.HttpProtobuf;
```

## View logs in Grafana

- After starting the stack (tasks or manual), open Grafana → Explore.
- Select the `Loki` datasource and run a simple query like `{app="emc.main.api"}` or `{service.name="emc.main.api"}` depending on your log labels.
- Logs are sent from your app → OTLP → Collector → Loki. We also keep `debug` exporter enabled for local inspection in the Collector logs.

## Stopping and cleanup

```bash
# Stop containers
docker stop camus-otel-collector camus-prometheus camus-jaeger camus-grafana

# Remove containers (optional)
docker rm -f camus-otel-collector camus-prometheus camus-jaeger camus-grafana

# Remove network (optional)
docker network rm camus-observability || true
```
