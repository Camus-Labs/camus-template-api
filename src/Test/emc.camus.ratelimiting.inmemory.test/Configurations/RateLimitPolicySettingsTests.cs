using FluentAssertions;
using emc.camus.ratelimiting.inmemory.Configurations;

namespace emc.camus.ratelimiting.inmemory.test.Configurations;

public class RateLimitPolicySettingsTests
{
    private const string ValidPolicyName = "default";
    private const int ValidPermitLimit = 100;
    private const int ValidWindowSeconds = 60;

    private static RateLimitPolicySettings CreateSettings(
        int permitLimit = ValidPermitLimit,
        int windowSeconds = ValidWindowSeconds) =>
        new()
        {
            PermitLimit = permitLimit,
            WindowSeconds = windowSeconds
        };

    // --- Validate: Valid Configuration ---

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(100000)]
    public void Validate_ValidPermitLimit_DoesNotThrow(int permitLimit)
    {
        // Arrange
        var settings = CreateSettings(permitLimit: permitLimit);

        // Act
        var act = () => settings.Validate(ValidPolicyName);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(3600)]
    public void Validate_ValidWindowSeconds_DoesNotThrow(int windowSeconds)
    {
        // Arrange
        var settings = CreateSettings(windowSeconds: windowSeconds);

        // Act
        var act = () => settings.Validate(ValidPolicyName);

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: Invalid PermitLimit ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_PermitLimitBelowMinimum_ThrowsInvalidOperationException(int permitLimit)
    {
        // Arrange
        var settings = CreateSettings(permitLimit: permitLimit);

        // Act
        var act = () => settings.Validate(ValidPolicyName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PermitLimit*");
    }

    [Fact]
    public void Validate_PermitLimitAboveMaximum_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateSettings(permitLimit: 100001);

        // Act
        var act = () => settings.Validate("strict");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*strict*PermitLimit*");
    }

    // --- Validate: Invalid WindowSeconds ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WindowSecondsBelowMinimum_ThrowsInvalidOperationException(int windowSeconds)
    {
        // Arrange
        var settings = CreateSettings(windowSeconds: windowSeconds);

        // Act
        var act = () => settings.Validate(ValidPolicyName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WindowSeconds*");
    }

    [Fact]
    public void Validate_WindowSecondsAboveMaximum_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateSettings(windowSeconds: 3601);

        // Act
        var act = () => settings.Validate("relaxed");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*relaxed*WindowSeconds*");
    }

    // --- Validate: Invalid PolicyName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidPolicyName_ThrowsArgumentException(string? policyName)
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        var act = () => settings.Validate(policyName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("policyName");
    }
}
