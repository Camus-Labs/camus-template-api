using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class RoleMappingExtensionsTests
{
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
            Name = "Admin",
            Description = "Administrator role",
            Permissions = new[] { "read", "write", "delete" }
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Id.Should().Be(ValidRoleId);
        entity.Name.Should().Be("Admin");
        entity.Description.Should().Be("Administrator role");
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
