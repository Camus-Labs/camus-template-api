using FluentAssertions;
using emc.camus.api.Mapping.V1;
using emc.camus.application.ApiInfo;

namespace emc.camus.api.test.Mapping.V1;

public class ApiInfoMappingExtensionsTests
{
    private static readonly List<string> TestFeatures = ["auth", "rate-limiting"];
    private static readonly List<string> EmptyFeatures = [];

    // --- ToFilter ---

    [Fact]
    public void ToFilter_ValidVersion_CreatesFilterWithVersion()
    {
        // Arrange
        // Act
        var filter = ApiInfoMappingExtensions.ToFilter("2.0");

        // Assert
        filter.Version.Should().Be("2.0");
    }

    // --- ToResponse ---

    [Fact]
    public void ToResponse_ValidDetailView_MapsAllProperties()
    {
        // Arrange
        var view = new ApiInfoDetailView("1.0", "Available", TestFeatures);

        // Act
        var response = view.ToResponse();

        // Assert
        response.Version.Should().Be("1.0");
        response.Status.Should().Be("Available");
        response.Features.Should().BeEquivalentTo(TestFeatures);
    }

    [Fact]
    public void ToResponse_EmptyFeatures_MapsEmptyList()
    {
        // Arrange
        var view = new ApiInfoDetailView("2.0", "Active", EmptyFeatures);

        // Act
        var response = view.ToResponse();

        // Assert
        response.Features.Should().BeEmpty();
    }
}
