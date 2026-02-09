using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.domain.test.Auth;

/// <summary>
/// Unit tests for ApiInfo domain entity.
/// </summary>
public class ApiInfoTests
{
    [Fact]
    public void Name_ShouldBeSettable()
    {
        // Arrange
        var apiInfo = new ApiInfo();
        var expectedName = "Test API";

        // Act
        apiInfo.Name = expectedName;

        // Assert
        apiInfo.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Version_ShouldBeSettable()
    {
        // Arrange
        var apiInfo = new ApiInfo();
        var expectedVersion = "v2.0";

        // Act
        apiInfo.Version = expectedVersion;

        // Assert
        apiInfo.Version.Should().Be(expectedVersion);
    }

    [Fact]
    public void Status_ShouldBeSettable()
    {
        // Arrange
        var apiInfo = new ApiInfo();
        var expectedStatus = "Running";

        // Act
        apiInfo.Status = expectedStatus;

        // Assert
        apiInfo.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public void Features_ShouldBeSettable()
    {
        // Arrange
        var apiInfo = new ApiInfo();
        var expectedFeatures = new List<string> { "Authentication", "Logging", "Versioning" };

        // Act
        apiInfo.Features = expectedFeatures;

        // Assert
        apiInfo.Features.Should().BeEquivalentTo(expectedFeatures);
    }

    [Fact]
    public void Properties_ShouldDefaultToEmpty()
    {
        // Arrange & Act
        var apiInfo = new ApiInfo();

        // Assert
        apiInfo.Name.Should().BeEmpty();
        apiInfo.Version.Should().BeEmpty();
        apiInfo.Status.Should().BeEmpty();
        apiInfo.Features.Should().BeNull();
    }

    [Fact]
    public void ApiInfo_WithAllProperties_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var apiInfo = new ApiInfo
        {
            Name = "My API",
            Version = "v1.0",
            Status = "Running",
            Features = new List<string> { "Auth", "Logging" }
        };

        // Assert
        apiInfo.Name.Should().Be("My API");
        apiInfo.Version.Should().Be("v1.0");
        apiInfo.Status.Should().Be("Running");
        apiInfo.Features.Should().HaveCount(2);
        apiInfo.Features.Should().Contain("Auth");
        apiInfo.Features.Should().Contain("Logging");
    }

    [Fact]
    public void Features_CanBeEmptyList()
    {
        // Arrange & Act
        var apiInfo = new ApiInfo
        {
            Features = new List<string>()
        };

        // Assert
        apiInfo.Features.Should().NotBeNull();
        apiInfo.Features.Should().BeEmpty();
    }

    [Fact]
    public void Features_CanContainMultipleItems()
    {
        // Arrange
        var features = new List<string>
        {
            "Authentication",
            "Authorization",
            "Versioning",
            "Logging",
            "Observability",
            "RateLimiting"
        };

        // Act
        var apiInfo = new ApiInfo
        {
            Features = features
        };

        // Assert
        apiInfo.Features.Should().HaveCount(6);
        apiInfo.Features.Should().BeEquivalentTo(features);
    }
}
