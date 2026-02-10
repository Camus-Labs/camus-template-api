using emc.camus.documentation.swagger.Configurations;
using FluentAssertions;

namespace emc.camus.documentation.swagger.test.Configurations;

/// <summary>
/// Unit tests for ApiVersionInfo configuration validation.
/// </summary>
public class ApiVersionInfoTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = "v1",
            Title = "API Version 1",
            Description = "First version of the API"
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyVersion_ShouldThrow()
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = "",
            Title = "API Version 1"
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Version cannot be null or empty*")
            .WithParameterName("Version");
    }

    [Fact]
    public void Validate_WithNullVersion_ShouldThrow()
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = null,
            Title = "API Version 1"
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Version cannot be null or empty*")
            .WithParameterName("Version");
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldThrow()
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = "v1",
            Title = ""
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be null or empty*")
            .WithParameterName("Title");
    }

    [Fact]
    public void Validate_WithNullTitle_ShouldThrow()
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = "v1",
            Title = null
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be null or empty*")
            .WithParameterName("Title");
    }

    [Fact]
    public void Validate_WithEmptyDescription_ShouldNotThrow()
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = "v1",
            Title = "API Version 1",
            Description = ""
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("v1", "Version 1")]
    [InlineData("v2.0", "Version 2.0")]
    [InlineData("1.0", "First Version")]
    public void Validate_WithVariousValidFormats_ShouldNotThrow(string version, string title)
    {
        // Arrange
        var versionInfo = new ApiVersionInfo
        {
            Version = version,
            Title = title
        };

        // Act & Assert
        var act = () => versionInfo.Validate();
        act.Should().NotThrow();
    }
}
