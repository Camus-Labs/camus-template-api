using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class RoleMappingExtensionsTests
{
    private const string RoleName = "Admin";
    private const string RoleDescription = "Administrator role";
    private static readonly Guid ValidRoleId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly string[] ExpectedPermissions = ["read", "write", "delete"];

    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModel_MapsAllProperties()
    {
        // Arrange
        var model = new RoleModel
        {
            Id = ValidRoleId,
            Name = RoleName,
            Description = RoleDescription,
            Permissions = ExpectedPermissions
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Id.Should().Be(ValidRoleId);
        entity.Name.Should().Be(RoleName);
        entity.Description.Should().Be(RoleDescription);
        entity.Permissions.Should().BeEquivalentTo(ExpectedPermissions);
    }

    [Fact]
    public void ToEntity_NullPermissions_MapsToEmptyList()
    {
        // Arrange
        var model = new RoleModel
        {
            Id = ValidRoleId,
            Name = "Guest",
            Description = null,
            Permissions = null
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Permissions.Should().BeEmpty();
    }
}
