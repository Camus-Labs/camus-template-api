using FluentAssertions;
using emc.camus.application.RateLimiting;

namespace emc.camus.application.test.RateLimiting;

public class RateLimitAttributeTests
{
    private const string ValidPolicyName = "strict";

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidPolicyName_SetsProperty()
    {
        // Arrange
        var policyName = ValidPolicyName;

        // Act
        var attribute = new RateLimitAttribute(policyName);

        // Assert
        attribute.PolicyName.Should().Be(policyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidPolicyName_ThrowsArgumentException(string? policyName)
    {
        // Arrange
        // Act
        var act = () => new RateLimitAttribute(policyName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("policyName");
    }

}
