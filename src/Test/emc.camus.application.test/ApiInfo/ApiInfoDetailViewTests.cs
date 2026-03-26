using FluentAssertions;
using emc.camus.application.ApiInfo;

namespace emc.camus.application.test.ApiInfo;

public class ApiInfoDetailViewTests
{
    private const string ValidVersion = "1.0";
    private const string ValidStatus = "Available";
    private static readonly IReadOnlyList<string> ValidFeatures = new List<string> { "Auth", "Tokens" };

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var version = ValidVersion;
        var status = ValidStatus;
        var features = ValidFeatures;

        // Act
        var view = new ApiInfoDetailView(version, status, features);

        // Assert
        view.Version.Should().Be(version);
        view.Status.Should().Be(status);
        view.Features.Should().BeEquivalentTo(features);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidVersion_ThrowsArgumentException(string? version)
    {
        // Arrange
        // Act
        var act = () => new ApiInfoDetailView(version!, ValidStatus, ValidFeatures);

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
        var act = () => new ApiInfoDetailView(ValidVersion, status!, ValidFeatures);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("status");
    }

    [Fact]
    public void Constructor_NullFeatures_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new ApiInfoDetailView(ValidVersion, ValidStatus, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("features");
    }
}
