using emc.camus.ratelimiting.memory.Configurations;
using FluentAssertions;

namespace emc.camus.ratelimiting.memory.test.Configurations;

/// <summary>
/// Unit tests for RateLimitPolicy validation logic.
/// </summary>
public class RateLimitPolicyTests
{
    [Fact]
    public void Validate_WithValidPolicy_DoesNotThrow()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 100,
            WindowSeconds = 60
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]          // Zero is invalid
    [InlineData(-1)]         // Negative values not allowed
    [InlineData(-100)]       // Large negative value
    [InlineData(100001)]     // Just above maximum
    [InlineData(200000)]     // Far above maximum
    public void Validate_WithInvalidPermitLimit_ThrowsArgumentException(int permitLimit)
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = permitLimit,
            WindowSeconds = 60
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PermitLimit must be between 1 and 100,000*")
            .And.ParamName.Should().Be("PermitLimit");
    }

    [Theory]
    [InlineData(1)]          // Minimum valid value
    [InlineData(100)]        // Typical small limit
    [InlineData(1000)]       // Moderate limit
    [InlineData(10000)]      // Large limit
    [InlineData(100000)]     // Maximum valid value
    public void Validate_WithValidPermitLimit_DoesNotThrow(int permitLimit)
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = permitLimit,
            WindowSeconds = 60
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    [InlineData(3601)]
    [InlineData(7200)]
    public void Validate_WithInvalidWindowSeconds_ThrowsArgumentException(int windowSeconds)
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 100,
            WindowSeconds = windowSeconds
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*WindowSeconds must be between 1 and 3,600*")
            .And.ParamName.Should().Be("WindowSeconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(3600)]
    public void Validate_WithValidWindowSeconds_DoesNotThrow(int windowSeconds)
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 100,
            WindowSeconds = windowSeconds
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_IncludesPolicyNameInErrorMessage()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 0,
            WindowSeconds = 60
        };

        // Act
        var act = () => policy.Validate("my-custom-policy");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*my-custom-policy*");
    }

    [Fact]
    public void Validate_WithBothInvalidValues_ThrowsForPermitLimitFirst()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 0,
            WindowSeconds = 0
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("PermitLimit");
    }

    [Fact]
    public void Validate_WithMinimumValidValues_DoesNotThrow()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 1,
            WindowSeconds = 1
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithMaximumValidValues_DoesNotThrow()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            PermitLimit = 100000,
            WindowSeconds = 3600
        };

        // Act
        var act = () => policy.Validate("test-policy");

        // Assert
        act.Should().NotThrow();
    }
}
