using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class ApiInfoTests
{
    private const string ValidVersion = "1.0.0";
    private const string ValidStatus = "Available";
    private readonly List<string> ValidFeatures = ["auth", "rate-limiting"];

    // --- Constructor ---

    [Fact]
    public void Constructor_RequiredParametersOnly_SetsDefaults()
    {
        // Arrange
        var version = ValidVersion;
        var status = ValidStatus;

        // Act
        var apiInfo = new ApiInfo(version, status);

        // Assert
        apiInfo.Name.Should().Be("My Basic API");
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
        var apiInfo = new ApiInfo(version, status, features, name);

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
        var act = () => new ApiInfo(version!, ValidStatus);

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
        var act = () => new ApiInfo(ValidVersion, status!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("status");
    }

    [Fact]
    public void Constructor_NullFeatures_DefaultsToEmptyList()
    {
        // Arrange
        // Act
        var apiInfo = new ApiInfo(ValidVersion, ValidStatus, features: null);

        // Assert
        apiInfo.Features.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullName_UsesDefaultName()
    {
        // Arrange
        // Act
        var apiInfo = new ApiInfo(ValidVersion, ValidStatus, name: null);

        // Assert
        apiInfo.Name.Should().Be("My Basic API");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        // Arrange
        // Act
        var act = () => new ApiInfo(ValidVersion, ValidStatus, name: name);

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
        var features = ValidFeatures;

        // Act
        var apiInfo = ApiInfo.Reconstitute(name, version, status, features);

        // Assert
        apiInfo.Name.Should().Be(name);
        apiInfo.Version.Should().Be(version);
        apiInfo.Status.Should().Be(status);
        apiInfo.Features.Should().BeEquivalentTo(features);
    }

}
