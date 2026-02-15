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
        var apiInfo = new ApiInfo("1.0", "Available");

        // Assert
        apiInfo.Name.Should().Be("My Basic API");
        apiInfo.Version.Should().Be("1.0");
        apiInfo.Status.Should().Be("Available");
        apiInfo.Features.Should().NotBeNull();
    }

    [Fact]
    public void ApiInfo_WithAllProperties_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var apiInfo = new ApiInfo(
            "v1.0",
            "API Key Authentication",
            new List<string> { "Auth", "Logging" },
            "My API"
        );

        // Assert
        apiInfo.Name.Should().Be("My API");
        apiInfo.Version.Should().Be("v1.0");
        apiInfo.Status.Should().Be("API Key Authentication");
        apiInfo.Features.Should().HaveCount(2);
        apiInfo.Features.Should().Contain("Auth");
        apiInfo.Features.Should().Contain("Logging");
    }

    [Fact]
    public void ApiInfo_WithEmptyStatus_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new ApiInfo("1.0", "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*status*");
    }

    [Fact]
    public void ApiInfo_WithWhitespaceStatus_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new ApiInfo("1.0", "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*status*");
    }
}
