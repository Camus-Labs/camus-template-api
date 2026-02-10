using emc.camus.application.RateLimiting;
using FluentAssertions;

namespace emc.camus.application.test.RateLimiting;

/// <summary>
/// Unit tests for RateLimitAttribute to verify policy name validation.
/// </summary>
public class RateLimitAttributeTests
{
    [Fact]
    public void Constructor_WithValidPolicyName_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute("strict");

        // Assert
        attribute.PolicyName.Should().Be("strict");
    }

    [Fact]
    public void Constructor_WithNullPolicyName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new RateLimitAttribute(null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be null or whitespace*")
            .And.ParamName.Should().Be("policyName");
    }

    [Fact]
    public void Constructor_WithEmptyPolicyName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new RateLimitAttribute("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be null or whitespace*")
            .And.ParamName.Should().Be("policyName");
    }

    [Fact]
    public void Constructor_WithWhitespacePolicyName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new RateLimitAttribute("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be null or whitespace*")
            .And.ParamName.Should().Be("policyName");
    }
}
