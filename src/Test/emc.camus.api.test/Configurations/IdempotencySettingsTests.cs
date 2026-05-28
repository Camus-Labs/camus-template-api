using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class IdempotencySettingsTests
{
    // --- Validate: Valid Configuration ---

    [Theory]
    [InlineData(300, 86400)]
    [InlineData(60, 3600)]
    public void Validate_ValidSettings_DoesNotThrow(int standardTtl, int longTermTtl)
    {
        // Arrange
        var settings = new IdempotencySettings
        {
            StandardTtlSeconds = standardTtl,
            LongTermTtlSeconds = longTermTtl
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
    [InlineData(604801)]
    public void Validate_StandardTtlSecondsOutOfRange_ThrowsInvalidOperationException(int value)
    {
        // Arrange
        var settings = new IdempotencySettings { StandardTtlSeconds = value };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*StandardTtlSeconds*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(604801)]
    public void Validate_LongTermTtlSecondsOutOfRange_ThrowsInvalidOperationException(int value)
    {
        // Arrange
        var settings = new IdempotencySettings { LongTermTtlSeconds = value };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LongTermTtlSeconds*");
    }

}
