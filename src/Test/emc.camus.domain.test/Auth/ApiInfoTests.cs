using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class ApiInfoTests
{
    private const string ValidName = "Test API";
    private const string ValidVersion = "1.0.0";
    private const string ValidStatus = "Available";

    // --- Constructor ---

    [Fact]
    public void Constructor_RequiredParametersOnly_SetsDefaults()
    {
        // Arrange
        var name = ValidName;
        var version = ValidVersion;
        var status = ValidStatus;

        // Act
        var apiInfo = new ApiInfo(name, version, status);

        // Assert
        apiInfo.Name.Should().Be(name);
        apiInfo.Version.Should().Be(version);
        apiInfo.Status.Should().Be(status);
        apiInfo.Features.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_AllParameters_SetsAllProperties()
    {
        // Arrange
        var version = ValidVersion;
        var status = ValidStatus;
        var features = new List<string> { "auth", "rate-limiting" };
        var name = "Custom API";

        // Act
        var apiInfo = new ApiInfo(name, version, status, features);

        // Assert
        apiInfo.Name.Should().Be(name);
        apiInfo.Version.Should().Be(version);
        apiInfo.Status.Should().Be(status);
        apiInfo.Features.Should().BeEquivalentTo(features);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidVersion_ThrowsArgumentException(string? version)
    {
        // Arrange
        // Act
        var act = () => new ApiInfo(ValidName, version!, ValidStatus);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("version");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidStatus_ThrowsArgumentException(string? status)
    {
        // Arrange
        // Act
        var act = () => new ApiInfo(ValidName, ValidVersion, status!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("status");
    }

    [Fact]
    public void Constructor_NullFeatures_DefaultsToEmptyList()
    {
        // Arrange
        // Act
        var apiInfo = new ApiInfo(ValidName, ValidVersion, ValidStatus, features: null);

        // Assert
        apiInfo.Features.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        // Act
        var act = () => new ApiInfo(name!, ValidVersion, ValidStatus);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }



    // --- Reconstitute ---

    [Fact]
    public void Reconstitute_ValidData_RebuildsAllFields()
    {
        // Arrange
        var name = "Custom API";
        var version = ValidVersion;
        var status = ValidStatus;
        var features = new List<string> { "auth", "rate-limiting" };

        // Act
        var apiInfo = ApiInfo.Reconstitute(name, version, status, features);

        // Assert
        apiInfo.Name.Should().Be(name);
        apiInfo.Version.Should().Be(version);
        apiInfo.Status.Should().Be(status);
        apiInfo.Features.Should().BeEquivalentTo(features);
    }

}
