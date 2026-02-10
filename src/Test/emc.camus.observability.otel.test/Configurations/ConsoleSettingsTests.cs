using emc.camus.observability.otel.Configurations;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Configurations;

public class ConsoleSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new ConsoleSettings
        {
            Enabled = true,
            OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
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
    public void Validate_WithNullOrEmptyOutputTemplate_ThrowsArgumentException(string? outputTemplate)
    {
        // Arrange
        var settings = new ConsoleSettings
        {
            Enabled = true,
            OutputTemplate = outputTemplate!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OutputTemplate cannot be null or empty*")
            .And.ParamName.Should().Be("OutputTemplate");
    }

    [Fact]
    public void Validate_WithEnabledFalse_StillValidatesOutputTemplate()
    {
        // Arrange
        var settings = new ConsoleSettings
        {
            Enabled = false,
            OutputTemplate = "" // Invalid template
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("OutputTemplate cannot be null or empty*")
            .And.ParamName.Should().Be("OutputTemplate");
    }
}
