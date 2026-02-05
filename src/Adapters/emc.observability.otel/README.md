# emc.observability.otel

## Overview

This package provides OpenTelemetry-based observability for .NET applications, including distributed tracing, metrics, and structured logging. It is designed to be used with the Camus stack and integrates with the OpenTelemetry Collector for flexible exporter routing.

## Supported Exporters

### Tracing

- OTLP (default, via Collector)
- Jaeger
- Zipkin
- Console (for development)

### Metrics

- OTLP (default, via Collector)
- Prometheus
- Console (for development)

### Logs

- OTLP (default, via Collector)
- Loki (via Collector's otlphttp/loki exporter)
- Console (for development)

## Configuration Schema

Example (appsettings.json):

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Otlp|Jaeger|Zipkin|Console",
      "OtlpEndpoint": "http://localhost:4317"
    },
    "Metrics": {
      "Exporter": "Otlp|Prometheus|Console",
      "OtlpEndpoint": "http://localhost:4317"
    },
    "Logs": {
      "Exporter": "Otlp|Console",
      "OtlpEndpoint": "http://localhost:4317"
    }
  }
}
```

- Defaults: If no exporter is specified, OTLP is used.
- Endpoints: Set the appropriate endpoint for your collector or exporter.

## Log Routing to Loki

Logs are sent from your .NET app to the OpenTelemetry Collector using OTLP. The collector is responsible for forwarding logs to Loki using its configured exporters. For details and the latest configuration, see the `otel-collector-config.yaml` file in this repository. This avoids documentation drift and ensures you always have the most accurate routing information.

## Notes

- Ensure your application is configured to send logs, traces, and metrics to the collector endpoint.
- Loki integration is handled by the collector; no direct Loki sink is needed in the .NET app.
- Only the exporters listed above are supported out-of-the-box. If you need additional exporters, extend the code and update the collector configuration accordingly.
- For more details, see the main project README and otel-collector-config.yaml.
