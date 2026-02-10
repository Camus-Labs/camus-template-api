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
            Tracing = new TracingSettings { Exporter = "none" },
            Metrics = new MetricsSettings { Exporter = "none" },
            Logs = new LogsSettings { Exporter = "none" }
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
            Metrics = new MetricsSettings { Exporter = "none" },
            Logs = new LogsSettings { Exporter = "none" }
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
            Tracing = new TracingSettings { Exporter = "none" },
            Metrics = null,
            Logs = new LogsSettings { Exporter = "none" }
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
            Tracing = new TracingSettings { Exporter = "none" },
            Metrics = new MetricsSettings { Exporter = "none" },
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
    public void Validate_WithInvalidTracingSettings_PropagatesException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = "invalid" },
            Metrics = new MetricsSettings { Exporter = "none" },
            Logs = new LogsSettings { Exporter = "none" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Exporter value 'invalid'*")
            .And.ParamName.Should().Be("Exporter");
    }

    [Fact]
    public void Validate_WithInvalidMetricsSettings_PropagatesException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = "none" },
            Metrics = new MetricsSettings { Exporter = "invalid" },
            Logs = new LogsSettings { Exporter = "none" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Exporter value 'invalid'*")
            .And.ParamName.Should().Be("Exporter");
    }

    [Fact]
    public void Validate_WithInvalidLogsSettings_PropagatesException()
    {
        // Arrange
        var settings = new OpenTelemetrySettings
        {
            Tracing = new TracingSettings { Exporter = "none" },
            Metrics = new MetricsSettings { Exporter = "none" },
            Logs = new LogsSettings { Exporter = "invalid" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Exporter value 'invalid'*")
            .And.ParamName.Should().Be("Exporter");
    }
}
