using FluentAssertions;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class ApiInfoSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = new ApiInfoSettings
        {
            Name = "Test API",
            Version = "1.0",
            Status = "Available",
            Features = new List<string> { "feature1" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidName_ThrowsInvalidOperationException(string? name)
    {
        // Arrange
        var settings = new ApiInfoSettings
        {
            Name = name!,
            Version = "1.0",
            Status = "Available"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Name*cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidVersion_ThrowsInvalidOperationException(string? version)
    {
        // Arrange
        var settings = new ApiInfoSettings
        {
            Name = "Test API",
            Version = version!,
            Status = "Available"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Version*cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidStatus_ThrowsInvalidOperationException(string? status)
    {
        // Arrange
        var settings = new ApiInfoSettings
        {
            Name = "Test API",
            Version = "1.0",
            Status = status!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Status*cannot be null or empty*");
    }

    [Fact]
    public void Validate_NullFeatures_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new ApiInfoSettings
        {
            Name = "Test API",
            Version = "1.0",
            Status = "Available",
            Features = null!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Features*cannot be null*");
    }

    [Fact]
    public void Validate_EmptyFeatures_DoesNotThrow()
    {
        // Arrange
        var settings = new ApiInfoSettings
        {
            Name = "Test API",
            Version = "1.0",
            Status = "Available",
            Features = new List<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
