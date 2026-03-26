using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class UserMappingExtensionsTests
{
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ValidRoleId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModelWithRoles_MapsAllProperties()
    {
        // Arrange
        var userModel = new UserModel
        {
            Id = ValidUserId,
            Username = "testuser"
        };

        var roleModels = new List<RoleModel>
        {
            new()
            {
                Id = ValidRoleId,
                Name = "Admin",
                Description = "Administrator",
                Permissions = new List<string> { "read", "write" }
            }
        };

        // Act
        var entity = userModel.ToEntity(roleModels);

        // Assert
        entity.Id.Should().Be(ValidUserId);
        entity.Username.Should().Be("testuser");
        entity.Roles.Should().ContainSingle();
        entity.Roles[0].Name.Should().Be("Admin");
    }

}
