# OpenTelemetry Collector (OTLP) for Camus

This folder contains a minimal OpenTelemetry Collector config to receive OTLP traces/metrics/logs and export them to console via the `debug` exporter (replacement for the deprecated `logging` exporter). You can enable Jaeger/Zipkin/Azure Monitor by uncommenting lines in `otel-collector.yaml`.

## Run with Docker (recommended for dev)

```bash
# From repo root
docker run --name camus-otel-collector --rm \
  -p 4317:4317 -p 4318:4318 \
  -v "$(pwd)/src/Adapters/emc.observability.otel/otel-collector.yaml:/etc/otelcol/config.yaml" \
  otel/opentelemetry-collector-contrib:latest \
  --config /etc/otelcol/config.yaml
```

- OTLP gRPC: http://localhost:4317
- OTLP HTTP/Protobuf: http://localhost:4318

If you use 4318 (HTTP/Protobuf) in your app, set exporter protocol:

```csharp
options.Protocol = OtlpTransportProtocol.HttpProtobuf;
```

## Run natively (Homebrew)

```bash
brew install opentelemetry-collector-contrib
otelcol-contrib --config /absolute/path/to/src/Adapters/emc.observability.otel/otel-collector.yaml
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

For logs: Jaeger/Zipkin don’t store logs. Send logs to a log backend (Elasticsearch/Loki/Azure/GCP/AWS) via Serilog or OTLP→Collector→log exporter. Use `trace_id` from `TraceSpanEnricher` to correlate. The Collector YAML here uses the `debug` exporter so you can see incoming logs/traces/metrics in the container output during development.
