using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class ApiInfoMappingExtensionsTests
{
    private static readonly string[] ExpectedFeatures = ["feature1", "feature2"];

    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModel_MapsAllProperties()
    {
        // Arrange
        var model = new ApiInfoModel
        {
            Name = "Test API",
            Version = "1.0",
            Status = "active",
            Features = new[] { "feature1", "feature2" }
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Name.Should().Be("Test API");
        entity.Version.Should().Be("1.0");
        entity.Status.Should().Be("active");
        entity.Features.Should().BeEquivalentTo(ExpectedFeatures);
    }

    [Fact]
    public void ToEntity_NullFeatures_MapsToEmptyList()
    {
        // Arrange
        var model = new ApiInfoModel
        {
            Name = "Test API",
            Version = "1.0",
            Status = "active",
            Features = null
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Features.Should().BeEmpty();
    }
}
