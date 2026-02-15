using emc.camus.observability.otel.Configurations;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Configurations;

public class TracingSettingsTests
{
    [Theory]
    [InlineData(TracingExporter.None)]
    [InlineData(TracingExporter.Console)]
    [InlineData(TracingExporter.Otlp)]
    public void Validate_WithValidExporter_DoesNotThrow(TracingExporter exporter)
    {
        // Arrange
        var settings = new TracingSettings
        {
            Exporter = exporter,
            OtlpEndpoint = "http://localhost:4317"
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
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Otlp,
            OtlpEndpoint = null
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
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Otlp,
            OtlpEndpoint = "not-a-uri"
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
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Otlp,
            OtlpEndpoint = "http://localhost:4317"
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
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Console,
            OtlpEndpoint = "" // Invalid endpoint, but should not matter for non-OTLP exporter
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
