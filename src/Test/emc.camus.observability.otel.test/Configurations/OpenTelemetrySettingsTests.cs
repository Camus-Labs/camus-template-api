using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class OpenTelemetrySettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new OpenTelemetrySettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullTracing_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings { Tracing = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Tracing*null*");
    }

    [Fact]
    public void Validate_NullMetrics_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings { Metrics = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Metrics*null*");
    }

    [Fact]
    public void Validate_NullLogs_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings { Logs = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Logs*null*");
    }

    [Fact]
    public void Validate_InvalidTracingSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = (TracingExporter)999 }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Exporter*");
    }

    [Fact]
    public void Validate_InvalidMetricsSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Metrics = new MetricsSettings { Exporter = (MetricsExporter)999 }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Exporter*");
    }

    [Fact]
    public void Validate_InvalidLogsSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Logs = new LogsSettings { Exporter = (LogsExporter)999 }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Exporter*");
    }

    [Fact]
    public void Validate_AllValidSubSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings
            {
                Exporter = TracingExporter.Otlp,
                OtlpEndpoint = "http://collector:4317"
            },
            Metrics = new MetricsSettings
            {
                Exporter = MetricsExporter.Otlp,
                OtlpEndpoint = "http://collector:4317"
            },
            Logs = new LogsSettings
            {
                Exporter = LogsExporter.Otlp,
                OtlpEndpoint = "http://collector:4317"
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
