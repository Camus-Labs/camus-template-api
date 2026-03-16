using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class UserTests
{
    private static readonly Guid ValidId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidUsername = "testuser";

    private readonly string[] ExpectedReadWritePermissions = ["read", "write"];
    private readonly string[] ExpectedReadWriteDeletePermissions = ["read", "write", "delete"];

    private static Role CreateRole(string name = "Admin") =>
        Role.Reconstitute(
            new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            name,
            "Test role",
            new List<string> { "read", "write" });

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidUsernameOnly_CreatesUserWithGeneratedId()
    {
        // Arrange
        var username = ValidUsername;

        // Act
        var user = new User(username);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Username.Should().Be(username);
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_AllParameters_SetsAllProperties()
    {
        // Arrange
        var username = ValidUsername;
        var roles = new List<Role> { CreateRole() };
        var id = ValidId;

        // Act
        var user = new User(username, roles, id);

        // Assert
        user.Id.Should().Be(id);
        user.Username.Should().Be(username);
        user.Roles.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        // Act
        var act = () => new User(username!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("username");
    }

    [Fact]
    public void Constructor_EmptyGuidId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        var act = () => new User(ValidUsername, id: emptyId);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*not*equal*")
            .And.ParamName.Should().Be("id.Value");
    }

    [Fact]
    public void Constructor_NullId_GeneratesNewId()
    {
        // Arrange
        // Act
        var user = new User(ValidUsername, id: null);

        // Assert
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_NullRoles_DefaultsToEmptyList()
    {
        // Arrange
        // Act
        var user = new User(ValidUsername, roles: null);

        // Assert
        user.Roles.Should().BeEmpty();
    }

    // --- Reconstitute ---

    [Fact]
    public void Reconstitute_ValidData_RebuildsAllFields()
    {
        // Arrange
        var id = ValidId;
        var username = ValidUsername;
        var roles = new List<Role> { CreateRole() };

        // Act
        var user = User.Reconstitute(id, username, roles);

        // Assert
        user.Id.Should().Be(id);
        user.Username.Should().Be(username);
        user.Roles.Should().ContainSingle();
    }

    // --- GetPermissions ---

    [Fact]
    public void GetPermissions_NoRoles_ReturnsEmptyList()
    {
        // Arrange
        var user = new User(ValidUsername);

        // Act
        var permissions = user.GetPermissions();

        // Assert
        permissions.Should().BeEmpty();
    }

    [Fact]
    public void GetPermissions_SingleRole_ReturnsRolePermissions()
    {
        // Arrange
        var role = Role.Reconstitute(
            new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Admin",
            "Admin role",
            new List<string> { "read", "write" });
        var user = User.Reconstitute(ValidId, ValidUsername, new List<Role> { role });

        // Act
        var permissions = user.GetPermissions();

        // Assert
        permissions.Should().BeEquivalentTo(ExpectedReadWritePermissions);
    }

    [Fact]
    public void GetPermissions_MultipleRolesWithOverlap_ReturnsDistinctPermissions()
    {
        // Arrange
        var role1 = Role.Reconstitute(
            new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Admin",
            "Admin role",
            new List<string> { "read", "write" });

        var role2 = Role.Reconstitute(
            new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "Editor",
            "Editor role",
            new List<string> { "write", "delete" });

        var user = User.Reconstitute(ValidId, ValidUsername, new List<Role> { role1, role2 });

        // Act
        var permissions = user.GetPermissions();

        // Assert
        permissions.Should().BeEquivalentTo(ExpectedReadWriteDeletePermissions);
    }
}
