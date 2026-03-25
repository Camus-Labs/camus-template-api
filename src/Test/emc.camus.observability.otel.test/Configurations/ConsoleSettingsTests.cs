using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class ConsoleSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new ConsoleSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_CustomOutputTemplate_DoesNotThrow()
    {
        // Arrange
        var settings = new ConsoleSettings { OutputTemplate = "[{Timestamp:HH:mm:ss}] {Message}" };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidOutputTemplate_ThrowsInvalidOperationException(string? outputTemplate)
    {
        // Arrange
        var settings = new ConsoleSettings { OutputTemplate = outputTemplate! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutputTemplate*null*empty*");
    }
}
