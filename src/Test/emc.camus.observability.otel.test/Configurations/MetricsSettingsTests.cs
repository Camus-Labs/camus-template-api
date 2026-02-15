using emc.camus.observability.otel.Configurations;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Configurations;

public class MetricsSettingsTests
{
    [Theory]
    [InlineData(MetricsExporter.None)]
    [InlineData(MetricsExporter.Console)]
    [InlineData(MetricsExporter.Otlp)]
    public void Validate_WithValidExporter_DoesNotThrow(MetricsExporter exporter)
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = exporter,
            OtlpEndpoint = "http://localhost:4317",
            DisabledMetrics = Array.Empty<string>(),
            DisabledMeters = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithOtlpExporterAndNullEndpoint_ThrowsArgumentException()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Otlp,
            OtlpEndpoint = null,
            DisabledMetrics = Array.Empty<string>(),
            DisabledMeters = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'*")
            .And.ParamName.Should().Be("OtlpEndpoint");
    }

    [Fact]
    public void Validate_WithOtlpExporterAndInvalidUri_ThrowsArgumentException()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Otlp,
            OtlpEndpoint = "not-a-uri",
            DisabledMetrics = Array.Empty<string>(),
            DisabledMeters = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OtlpEndpoint must be a valid URI: 'not-a-uri'*")
            .And.ParamName.Should().Be("OtlpEndpoint");
    }

    [Fact]
    public void Validate_WithOtlpExporterAndValidEndpoint_DoesNotThrow()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Otlp,
            OtlpEndpoint = "http://localhost:4317",
            DisabledMetrics = Array.Empty<string>(),
            DisabledMeters = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullDisabledMetrics_ThrowsArgumentException()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.None,
            DisabledMetrics = null,
            DisabledMeters = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("DisabledMetrics cannot be null*")
            .And.ParamName.Should().Be("DisabledMetrics");
    }

    [Fact]
    public void Validate_WithNullDisabledMeters_ThrowsArgumentException()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.None,
            DisabledMetrics = Array.Empty<string>(),
            DisabledMeters = null
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("DisabledMeters cannot be null*")
            .And.ParamName.Should().Be("DisabledMeters");
    }

    [Fact]
    public void Validate_WithPopulatedDisabledMetricsAndMeters_DoesNotThrow()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.None,
            DisabledMetrics = new[] { "http.server.duration", "http.client.duration" },
            DisabledMeters = new[] { ".infrastructure", ".business" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNonOtlpExporter_DoesNotValidateEndpoint()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Console,
            OtlpEndpoint = "", // Invalid endpoint, but should not matter for non-OTLP exporter
            DisabledMetrics = Array.Empty<string>(),
            DisabledMeters = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
