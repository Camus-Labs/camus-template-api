using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class RequestTimeoutSettingsTests
{
    // --- Validate: Valid Configuration ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new RequestTimeoutSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_CustomValidValues_DoesNotThrow()
    {
        // Arrange
        var settings = new RequestTimeoutSettings
        {
            DefaultTimeoutSeconds = 15,
            TightTimeoutSeconds = 3,
            ExtendedTimeoutSeconds = 120
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: Invalid Values ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validate_DefaultTimeoutSecondsOutOfRange_ThrowsInvalidOperationException(int value)
    {
        // Arrange
        var settings = new RequestTimeoutSettings { DefaultTimeoutSeconds = value };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(RequestTimeoutSettings.DefaultTimeoutSeconds)}*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validate_TightTimeoutSecondsOutOfRange_ThrowsInvalidOperationException(int value)
    {
        // Arrange
        var settings = new RequestTimeoutSettings { TightTimeoutSeconds = value };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(RequestTimeoutSettings.TightTimeoutSeconds)}*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validate_ExtendedTimeoutSecondsOutOfRange_ThrowsInvalidOperationException(int value)
    {
        // Arrange
        var settings = new RequestTimeoutSettings { ExtendedTimeoutSeconds = value };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(RequestTimeoutSettings.ExtendedTimeoutSeconds)}*");
    }

    // --- Default Values ---

    [Fact]
    public void Constructor_DefaultValues_AreExpected()
    {
        // Act
        var settings = new RequestTimeoutSettings();

        // Assert
        settings.DefaultTimeoutSeconds.Should().Be(30);
        settings.TightTimeoutSeconds.Should().Be(10);
        settings.ExtendedTimeoutSeconds.Should().Be(60);
    }
}
