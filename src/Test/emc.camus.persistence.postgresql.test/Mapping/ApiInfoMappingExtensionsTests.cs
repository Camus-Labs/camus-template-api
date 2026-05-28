using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class ApiInfoMappingExtensionsTests
{
    private const string ApiName = "Test API";
    private const string ApiVersion = "1.0";
    private const string ApiStatus = "active";
    private static readonly string[] ExpectedFeatures = ["feature1", "feature2"];

    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModel_MapsAllProperties()
    {
        // Arrange
        var model = new ApiInfoModel
        {
            Name = ApiName,
            Version = ApiVersion,
            Status = ApiStatus,
            Features = ExpectedFeatures
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Name.Should().Be(ApiName);
        entity.Version.Should().Be(ApiVersion);
        entity.Status.Should().Be(ApiStatus);
        entity.Features.Should().BeEquivalentTo(ExpectedFeatures);
    }

    [Fact]
    public void ToEntity_NullFeatures_MapsToEmptyList()
    {
        // Arrange
        var model = new ApiInfoModel
        {
            Name = ApiName,
            Version = ApiVersion,
            Status = ApiStatus,
            Features = null
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Features.Should().BeEmpty();
    }
}
