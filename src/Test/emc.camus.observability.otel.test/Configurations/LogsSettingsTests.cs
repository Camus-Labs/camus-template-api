using emc.camus.observability.otel.Configurations;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Configurations;

public class LogsSettingsTests
{
    [Theory]
    [InlineData("none")]
    [InlineData("console")]
    [InlineData("otlp")]
    [InlineData("OTLP")] // Case insensitive
    public void Validate_WithValidExporter_DoesNotThrow(string exporter)
    {
        // Arrange
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
            Exporter = exporter,
            OtlpEndpoint = "http://localhost:4317"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullConsole_ThrowsArgumentException()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Console = null,
            Exporter = "none"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Console settings cannot be null*")
            .And.ParamName.Should().Be("Console");
    }

    [Fact]
    public void Validate_WithInvalidConsoleSettings_PropagatesException()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings { OutputTemplate = "" },
            Exporter = "none"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OutputTemplate cannot be null or empty*")
            .And.ParamName.Should().Be("OutputTemplate");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyExporter_ThrowsArgumentException(string? exporter)
    {
        // Arrange
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
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
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
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
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
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
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
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
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
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
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings(),
            Exporter = "console",
            OtlpEndpoint = "" // Invalid endpoint, but should not matter for non-OTLP exporter
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
