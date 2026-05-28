using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class UserTests
{
    private static readonly Guid ValidId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ValidRoleId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const string ValidUsername = "testuser";
    private const string AdminRoleName = "Admin";
    private static readonly IReadOnlyList<string> WriteDeletePermissions = ["write", "delete"];
    private static readonly string[] ReadWritePermissions = ["read", "write"];
    private static readonly string[] ReadWriteDeletePermissions = ["read", "write", "delete"];
    private static readonly List<Role> EmptyRoleList = [];
    private static readonly string[] NoPermissions = [];

    private static Role CreateRole(string name = AdminRoleName) =>
        Role.Reconstitute(
            ValidRoleId,
            name,
            "Test role",
            ReadWritePermissions.ToList());

    private static readonly List<Role> SingleRoleList = [CreateRole()];

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
        var id = ValidId;

        // Act
        var user = new User(username, SingleRoleList, id);

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
            .WithMessage("*username*")
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

    [Fact]
    public void Constructor_UsernameExceedsMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var longUsername = new string('a', 201);

        // Act
        var act = () => new User(longUsername);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("username");
    }

    // --- Reconstitute ---

    [Fact]
    public void Reconstitute_ValidData_RebuildsAllFields()
    {
        // Arrange
        var id = ValidId;
        var username = ValidUsername;

        // Act
        var user = User.Reconstitute(id, username, SingleRoleList);

        // Assert
        user.Id.Should().Be(id);
        user.Username.Should().Be(username);
        user.Roles.Should().ContainSingle();
    }

    // --- GetPermissions ---

    public static readonly TheoryData<List<Role>, string[]> GetPermissionsCases = new()
    {
        { EmptyRoleList, NoPermissions },
        {
            new List<Role>
            {
                Role.Reconstitute(ValidRoleId, AdminRoleName, "Admin role", ReadWritePermissions.ToList())
            },
            ReadWritePermissions
        },
        {
            new List<Role>
            {
                Role.Reconstitute(ValidRoleId, AdminRoleName, "Admin role", ReadWritePermissions.ToList()),
                Role.Reconstitute(new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Editor", "Editor role", WriteDeletePermissions.ToList())
            },
            ReadWriteDeletePermissions
        }
    };

    [Theory]
    [MemberData(nameof(GetPermissionsCases))]
    public void GetPermissions_VariousRoleConfigurations_ReturnsExpectedPermissions(List<Role> roles, string[] expectedPermissions)
    {
        // Arrange
        var user = User.Reconstitute(ValidId, ValidUsername, roles);

        // Act
        var permissions = user.GetPermissions();

        // Assert
        permissions.Should().BeEquivalentTo(expectedPermissions);
    }
}
