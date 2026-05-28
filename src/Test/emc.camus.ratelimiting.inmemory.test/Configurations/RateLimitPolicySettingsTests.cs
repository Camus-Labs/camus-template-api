using FluentAssertions;
using emc.camus.ratelimiting.inmemory.Configurations;

namespace emc.camus.ratelimiting.inmemory.test.Configurations;

public class RateLimitPolicySettingsTests
{
    private const int DefaultPermitLimit = 100;
    private const int DefaultWindowSeconds = 60;

    private static RateLimitPolicySettings CreateSettings(
        string policyName = "default",
        int permitLimit = DefaultPermitLimit,
        int windowSeconds = DefaultWindowSeconds) =>
        new()
        {
            PolicyName = policyName,
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
        var act = () => settings.Validate();

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
        var act = () => settings.Validate();

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
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PermitLimit*");
    }

    [Fact]
    public void Validate_PermitLimitAboveMaximum_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateSettings(policyName: "strict", permitLimit: 100001);

        // Act
        var act = () => settings.Validate();

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
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WindowSeconds*");
    }

    [Fact]
    public void Validate_WindowSecondsAboveMaximum_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateSettings(policyName: "relaxed", windowSeconds: 3601);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*relaxed*WindowSeconds*");
    }

    // --- Validate: Invalid PolicyName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidPolicyName_ThrowsInvalidOperationException(string? policyName)
    {
        // Arrange
        var settings = new RateLimitPolicySettings
        {
            PolicyName = policyName!,
            PermitLimit = DefaultPermitLimit,
            WindowSeconds = DefaultWindowSeconds
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PolicyName*");
    }
}
