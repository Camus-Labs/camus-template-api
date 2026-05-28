using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class UserMappingExtensionsTests
{
    private const string TestUsername = "testuser";
    private const string RoleName = "Admin";
    private const string RoleDescription = "Administrator";
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ValidRoleId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly string[] RolePermissions = ["read", "write"];
    private static readonly RoleModel[] SingleRoleList =
    [
        new()
        {
            Id = ValidRoleId,
            Name = RoleName,
            Description = RoleDescription,
            Permissions = RolePermissions
        }
    ];
    private static readonly RoleModel[] EmptyRoleList = [];

    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModelWithRoles_MapsAllProperties()
    {
        // Arrange
        var userModel = new UserModel
        {
            Id = ValidUserId,
            Username = TestUsername
        };

        // Act
        var entity = userModel.ToEntity(SingleRoleList);

        // Assert
        entity.Id.Should().Be(ValidUserId);
        entity.Username.Should().Be(TestUsername);
        entity.Roles.Should().ContainSingle();
        entity.Roles[0].Id.Should().Be(ValidRoleId);
        entity.Roles[0].Name.Should().Be(RoleName);
        entity.Roles[0].Description.Should().Be(RoleDescription);
        entity.Roles[0].Permissions.Should().BeEquivalentTo(RolePermissions);
    }

    [Fact]
    public void ToEntity_EmptyRoles_MapsToEmptyRolesList()
    {
        // Arrange
        var userModel = new UserModel
        {
            Id = ValidUserId,
            Username = TestUsername
        };

        // Act
        var entity = userModel.ToEntity(EmptyRoleList);

        // Assert
        entity.Roles.Should().BeEmpty();
    }
}
