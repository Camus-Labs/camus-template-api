using emc.camus.observability.otel.Configurations;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Configurations;

public class OpenTelemetrySettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = TracingExporter.None },
            Metrics = new MetricsSettings { Exporter = MetricsExporter.None },
            Logs = new LogsSettings { Exporter = LogsExporter.None }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullTracing_ThrowsArgumentException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = null,
            Metrics = new MetricsSettings { Exporter = MetricsExporter.None },
            Logs = new LogsSettings { Exporter = LogsExporter.None }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Tracing settings cannot be null*")
            .And.ParamName.Should().Be("Tracing");
    }

    [Fact]
    public void Validate_WithNullMetrics_ThrowsArgumentException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = TracingExporter.None },
            Metrics = null,
            Logs = new LogsSettings { Exporter = LogsExporter.None }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Metrics settings cannot be null*")
            .And.ParamName.Should().Be("Metrics");
    }

    [Fact]
    public void Validate_WithNullLogs_ThrowsArgumentException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = TracingExporter.None },
            Metrics = new MetricsSettings { Exporter = MetricsExporter.None },
            Logs = null
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Logs settings cannot be null*")
            .And.ParamName.Should().Be("Logs");
    }

    [Fact]
    public void Validate_WithTracingOtlpAndNullEndpoint_PropagatesException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings
            {
                Exporter = TracingExporter.Otlp,
                OtlpEndpoint = null
            },
            Metrics = new MetricsSettings { Exporter = MetricsExporter.None },
            Logs = new LogsSettings { Exporter = LogsExporter.None }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'*")
            .And.ParamName.Should().Be("OtlpEndpoint");
    }
}
