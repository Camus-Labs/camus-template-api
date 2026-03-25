using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class LogsSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new LogsSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(LogsExporter.None)]
    [InlineData(LogsExporter.Console)]
    [InlineData(LogsExporter.Otlp)]
    public void Validate_DefinedExporter_DoesNotThrow(LogsExporter exporter)
    {
        // Arrange
        var settings = new LogsSettings { Exporter = exporter };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_InvalidExporter_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings { Exporter = (LogsExporter)999 };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Exporter*");
    }

    [Fact]
    public void Validate_NullConsole_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings { Console = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Console*null*");
    }

    [Fact]
    public void Validate_InvalidConsoleSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings { OutputTemplate = null! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutputTemplate*");
    }

    [Fact]
    public void Validate_OtlpExporter_ValidEndpoint_DoesNotThrow()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Exporter = LogsExporter.Otlp,
            OtlpEndpoint = "http://collector:4317"
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
    public void Validate_OtlpExporter_InvalidEndpoint_ThrowsInvalidOperationException(string? endpoint)
    {
        // Arrange
        var settings = new LogsSettings
        {
            Exporter = LogsExporter.Otlp,
            OtlpEndpoint = endpoint!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*null*empty*");
    }

    [Fact]
    public void Validate_OtlpExporter_InvalidUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Exporter = LogsExporter.Otlp,
            OtlpEndpoint = "not-a-valid-uri"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*valid*URI*");
    }

    [Fact]
    public void Validate_NonOtlpExporter_InvalidEndpoint_DoesNotThrow()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Exporter = LogsExporter.Console,
            OtlpEndpoint = "not-a-valid-uri"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
