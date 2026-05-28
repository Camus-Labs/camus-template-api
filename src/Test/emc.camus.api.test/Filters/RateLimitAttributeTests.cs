using FluentAssertions;
using emc.camus.api.Filters;

namespace emc.camus.api.test.Filters;

public class RateLimitAttributeTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_ValidPolicyName_SetsProperty()
    {
        // Arrange
        var policyName = "default";

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
        // Act
        var act = () => new RateLimitAttribute(policyName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("policyName");
    }

    // --- AttributeUsage ---

    [Fact]
    public void Class_HasAttributeUsage_AllowsClassAndMethodTargets()
    {
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(RateLimitAttribute), typeof(AttributeUsageAttribute))!;

        // Assert
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void Class_HasAttributeUsage_DisallowsMultiple()
    {
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(RateLimitAttribute), typeof(AttributeUsageAttribute))!;

        // Assert
        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Class_HasAttributeUsage_IsInherited()
    {
        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(RateLimitAttribute), typeof(AttributeUsageAttribute))!;

        // Assert
        usage.Inherited.Should().BeTrue();
    }

    // --- Namespace verification (AC-01, AC-04) ---

    [Fact]
    public void Class_ResolvesFromApiFiltersNamespace()
    {
        // Act
        var type = typeof(RateLimitAttribute);

        // Assert
        type.Namespace.Should().Be("emc.camus.api.Filters");
    }
}
