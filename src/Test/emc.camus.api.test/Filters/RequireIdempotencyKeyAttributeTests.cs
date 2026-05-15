using FluentAssertions;
using emc.camus.api.Filters;

namespace emc.camus.api.test.Filters;

public class RequireIdempotencyKeyAttributeTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_ValidPolicyName_SetsProperty()
    {
        // Arrange
        var policyName = "default";

        // Act
        var attribute = new RequireIdempotencyKeyAttribute(policyName);

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
        var act = () => new RequireIdempotencyKeyAttribute(policyName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("policyName");
    }
}
