using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class UserMappingExtensionsTests
{
    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModelWithRoles_MapsAllProperties()
    {
        // Arrange
        var userId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userModel = new UserModel
        {
            Id = userId,
            Username = "testuser"
        };

        var roleModels = new List<RoleModel>
        {
            new()
            {
                Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Admin",
                Description = "Administrator",
                Permissions = new List<string> { "read", "write" }
            }
        };

        // Act
        var entity = userModel.ToEntity(roleModels);

        // Assert
        entity.Id.Should().Be(userId);
        entity.Username.Should().Be("testuser");
        entity.Roles.Should().ContainSingle();
        entity.Roles[0].Name.Should().Be("Admin");
    }

}
