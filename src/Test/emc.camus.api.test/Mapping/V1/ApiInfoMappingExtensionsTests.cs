using FluentAssertions;
using emc.camus.api.Mapping.V1;
using emc.camus.application.ApiInfo;

namespace emc.camus.api.test.Mapping.V1;

public class ApiInfoMappingExtensionsTests
{
    // --- ToFilter ---

    [Fact]
    public void ToFilter_ValidVersion_CreatesFilterWithVersion()
    {
        // Arrange
        var version = "2.0";

        // Act
        var filter = ApiInfoMappingExtensions.ToFilter(version);

        // Assert
        filter.Version.Should().Be("2.0");
    }

    // --- ToResponse ---

    [Fact]
    public void ToResponse_ValidDetailView_MapsAllProperties()
    {
        // Arrange
        var features = new List<string> { "auth", "rate-limiting" };
        var view = new ApiInfoDetailView("1.0", "Available", features);

        // Act
        var response = view.ToResponse();

        // Assert
        response.Version.Should().Be("1.0");
        response.Status.Should().Be("Available");
        response.Features.Should().BeEquivalentTo(features);
    }

    [Fact]
    public void ToResponse_EmptyFeatures_MapsEmptyList()
    {
        // Arrange
        var view = new ApiInfoDetailView("2.0", "Active", new List<string>());

        // Act
        var response = view.ToResponse();

        // Assert
        response.Features.Should().BeEmpty();
    }
}
