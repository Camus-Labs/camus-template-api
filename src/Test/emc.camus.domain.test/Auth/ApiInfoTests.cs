using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.domain.test.Auth;

/// <summary>
/// Unit tests for ApiInfo domain entity.
/// Note: ApiInfo is marked with [ExcludeFromCodeCoverage] as it's a simple DTO.
/// These minimal tests verify object initialization and default values only.
/// </summary>
public class ApiInfoTests
{
    [Fact]
    public void Properties_ShouldDefaultToEmptyOrNull()
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
}
