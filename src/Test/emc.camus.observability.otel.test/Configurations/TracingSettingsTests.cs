using emc.camus.observability.otel.Configurations;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Configurations;

public class TracingSettingsTests
{
    [Theory]
    [InlineData("none")]
    [InlineData("console")]
    [InlineData("otlp")]
    [InlineData("OTLP")] // Case insensitive
    public void Validate_WithValidExporter_DoesNotThrow(string exporter)
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyExporter_ThrowsArgumentException(string? exporter)
    {
        // Arrange
        var settings = new TracingSettings
        {
            Exporter = exporter!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Exporter cannot be null or empty*")
            .And.ParamName.Should().Be("Exporter");
    }

    [Fact]
    public void Validate_WithInvalidExporter_ThrowsArgumentException()
    {
        // Arrange
        var settings = new TracingSettings
        {
            Exporter = "invalid"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Exporter value 'invalid'. Valid values are: otlp, console, none*")
            .And.ParamName.Should().Be("Exporter");
    }

    [Fact]
    public void Validate_WithOtlpExporterAndNullEndpoint_ThrowsArgumentException()
    {
        // Arrange
        var settings = new TracingSettings
        {
            Exporter = "otlp",
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
            Exporter = "otlp",
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
            Exporter = "otlp",
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
            Exporter = "console",
            OtlpEndpoint = "" // Invalid endpoint, but should not matter for non-OTLP exporter
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
