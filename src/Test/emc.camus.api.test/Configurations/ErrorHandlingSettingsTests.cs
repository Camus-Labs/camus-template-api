using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class ErrorHandlingSettingsTests
{
    // --- Validate: Valid Settings ---

    [Fact]
    public void Validate_DefaultSettings_Succeeds()
    {
        // Arrange
        var settings = new ErrorHandlingSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithValidAdditionalRules_Succeeds()
    {
        // Arrange
        var settings = new ErrorHandlingSettings
        {
            AdditionalRules = new List<ErrorCodeMappingRule>
            {
                new() { Type = "CustomException", ErrorCode = "custom_error" }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: AdditionalRules Validation ---

    [Fact]
    public void Validate_NullAdditionalRules_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new ErrorHandlingSettings
        {
            AdditionalRules = null!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdditionalRules*null*");
    }

    [Fact]
    public void Validate_InvalidRuleInAdditionalRules_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new ErrorHandlingSettings
        {
            AdditionalRules = new List<ErrorCodeMappingRule>
            {
                new() { ErrorCode = "valid_error", Type = "SomeType" },
                new() { ErrorCode = "" }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ErrorCode*null*empty*");
    }
}
