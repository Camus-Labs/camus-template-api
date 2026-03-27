using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class RoleTests
{
    private static readonly Guid ValidId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidName = "Admin";
    private const string ValidDescription = "Administrator role";
    private static readonly List<string> ValidPermissions = ["read", "write"];

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidNameOnly_CreatesRoleWithGeneratedId()
    {
        // Arrange
        var name = ValidName;

        // Act
        var role = new Role(name);

        // Assert
        role.Id.Should().NotBeEmpty();
        role.Name.Should().Be(name);
        role.Description.Should().BeNull();
        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_AllParameters_SetsAllProperties()
    {
        // Arrange
        var name = ValidName;
        var description = ValidDescription;
        var permissions = new List<string> { "read", "write" };
        var id = ValidId;

        // Act
        var role = new Role(name, description, permissions, id);

        // Assert
        role.Id.Should().Be(id);
        role.Name.Should().Be(name);
        role.Description.Should().Be(description);
        role.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        // Act
        var act = () => new Role(name!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Constructor_EmptyGuidId_ThrowsArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        var act = () => new Role(ValidName, id: emptyId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*not*equal*")
            .And.ParamName.Should().Be("id.Value");
    }

    [Fact]
    public void Constructor_NullId_GeneratesNewId()
    {
        // Arrange
        // Act
        var role = new Role(ValidName, id: null);

        // Assert
        role.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_NullPermissions_DefaultsToEmptyList()
    {
        // Arrange
        // Act
        var role = new Role(ValidName, permissions: null);

        // Assert
        role.Permissions.Should().BeEmpty();
    }

    // --- Reconstitute ---

    [Fact]
    public void Reconstitute_ValidData_RebuildsAllFields()
    {
        // Arrange
        var id = ValidId;
        var name = ValidName;
        var description = ValidDescription;
        var permissions = ValidPermissions;

        // Act
        var role = Role.Reconstitute(id, name, description, permissions);

        // Assert
        role.Id.Should().Be(id);
        role.Name.Should().Be(name);
        role.Description.Should().Be(description);
        role.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public void Reconstitute_NullDescription_SetsDescriptionToNull()
    {
        // Arrange
        // Act
        var role = Role.Reconstitute(ValidId, ValidName, null, ValidPermissions);

        // Assert
        role.Description.Should().BeNull();
    }
}
