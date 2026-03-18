using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class GeneratedTokenTests
{
    private static readonly Guid ValidCreatorUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidCreatorUsername = "admin";
    private const string ValidSuffix = "token1";
    private const string ValidTokenUsername = "admin-token1";
    private readonly List<string> ValidPermissions = ["read", "write"];
    private static readonly DateTime ValidExpiration = DateTime.UtcNow.AddMonths(6);

    private static User CreateCreator(Guid? id = null, string username = ValidCreatorUsername, List<string>? permissions = null)
    {
        var perms = permissions ?? ["read", "write"];
        var role = new Role("testrole", permissions: perms);
        return new User(username, [role], id ?? ValidCreatorUserId);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsAllProperties()
    {
        // Arrange
        var creator = CreateCreator();
        var suffix = ValidSuffix;
        var permissions = new List<string> { "read", "write" };
        var expiresOn = ValidExpiration;

        // Act
        var token = new GeneratedToken(creator, suffix, permissions, expiresOn);

        // Assert
        token.Jti.Should().NotBeEmpty();
        token.CreatorUserId.Should().Be(creator.Id);
        token.CreatorUsername.Should().Be(creator.Username);
        token.TokenUsername.Should().Be($"{creator.Username}-{suffix}");
        token.Permissions.Should().BeEquivalentTo(permissions);
        token.ExpiresOn.Should().Be(expiresOn);
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullCreator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GeneratedToken(null!, ValidSuffix, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("creator");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSuffix_ThrowsArgumentException(string? suffix)
    {
        // Arrange
        var creator = CreateCreator();

        // Act
        var act = () => new GeneratedToken(creator, suffix!, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("suffix");
    }

    [Fact]
    public void Constructor_SuffixExceedsMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var creator = CreateCreator();
        var longSuffix = new string('a', 21);

        // Act
        var act = () => new GeneratedToken(creator, longSuffix, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("suffix");
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("has@symbol")]
    [InlineData("has/slash")]
    public void Constructor_SuffixInvalidFormat_ThrowsArgumentException(string suffix)
    {
        // Arrange
        var creator = CreateCreator();

        // Act
        var act = () => new GeneratedToken(creator, suffix, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*alphanumeric*")
            .And.ParamName.Should().Be("suffix");
    }

    [Fact]
    public void Constructor_NullPermissions_ThrowsArgumentNullException()
    {
        // Arrange
        var creator = CreateCreator();

        // Act
        var act = () => new GeneratedToken(creator, ValidSuffix, null!, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("permissions");
    }

    [Fact]
    public void Constructor_EmptyPermissions_ThrowsArgumentException()
    {
        // Arrange
        var creator = CreateCreator();
        var emptyPermissions = new List<string>();

        // Act
        var act = () => new GeneratedToken(creator, ValidSuffix, emptyPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*permission*")
            .And.ParamName.Should().Be("permissions.Count");
    }

    [Fact]
    public void Constructor_PermissionsNotSubsetOfCreator_ThrowsInvalidOperationException()
    {
        // Arrange
        var creator = CreateCreator(permissions: ["read"]);
        var permissions = new List<string> { "read", "write" };

        // Act
        var act = () => new GeneratedToken(creator, ValidSuffix, permissions, ValidExpiration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot grant*write*");
    }

    [Fact]
    public void Constructor_ExpirationTooSoon_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var creator = CreateCreator();
        var tooSoon = DateTime.UtcNow.AddMinutes(30);

        // Act
        var act = () => new GeneratedToken(creator, ValidSuffix, ValidPermissions, tooSoon);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("expiresOn");
    }

    [Fact]
    public void Constructor_ExpirationTooFar_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var creator = CreateCreator();
        var tooFar = DateTime.UtcNow.AddYears(1).AddDays(1);

        // Act
        var act = () => new GeneratedToken(creator, ValidSuffix, ValidPermissions, tooFar);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("expiresOn");
    }

    // --- Reconstitute ---

    [Fact]
    public void Reconstitute_ValidData_RebuildsAllFields()
    {
        // Arrange
        var jti = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var creatorUserId = ValidCreatorUserId;
        var creatorUsername = ValidCreatorUsername;
        var tokenUsername = ValidTokenUsername;
        var permissions = ValidPermissions;
        var expiresOn = ValidExpiration;
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var isRevoked = true;
        var revokedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var token = GeneratedToken.Reconstitute(jti, creatorUserId, creatorUsername, tokenUsername, permissions, expiresOn, createdAt, isRevoked, revokedAt);

        // Assert
        token.Jti.Should().Be(jti);
        token.CreatorUserId.Should().Be(creatorUserId);
        token.CreatorUsername.Should().Be(creatorUsername);
        token.TokenUsername.Should().Be(tokenUsername);
        token.Permissions.Should().BeEquivalentTo(permissions);
        token.ExpiresOn.Should().Be(expiresOn);
        token.CreatedAt.Should().Be(createdAt);
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public void Reconstitute_NotRevoked_SetsRevokedAtToNull()
    {
        // Arrange
        var jti = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        // Act
        var token = GeneratedToken.Reconstitute(jti, ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, ValidPermissions, ValidExpiration,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), false, null);

        // Assert
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
    }

    // --- Revoke ---

    [Fact]
    public void Revoke_NotRevoked_SetsIsRevokedAndRevokedAt()
    {
        // Arrange
        var creator = CreateCreator(permissions: ["read"]);
        var token = new GeneratedToken(creator, ValidSuffix, new List<string> { "read" }, ValidExpiration);

        // Act
        token.Revoke(ValidCreatorUserId);

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var creator = CreateCreator(permissions: ["read"]);
        var token = new GeneratedToken(creator, ValidSuffix, new List<string> { "read" }, ValidExpiration);
        token.Revoke(ValidCreatorUserId);

        // Act
        var act = () => token.Revoke(ValidCreatorUserId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already revoked*");
    }

    // --- IsActive ---

    [Fact]
    public void IsActive_NotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var creator = CreateCreator(permissions: ["read"]);
        var token = new GeneratedToken(creator, ValidSuffix, new List<string> { "read" }, ValidExpiration);

        // Act
        var result = token.IsActive();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Revoked_ReturnsFalse()
    {
        // Arrange
        var creator = CreateCreator(permissions: ["read"]);
        var token = new GeneratedToken(creator, ValidSuffix, new List<string> { "read" }, ValidExpiration);
        token.Revoke(ValidCreatorUserId);

        // Act
        var result = token.IsActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Expired_ReturnsFalse()
    {
        // Arrange
        var expiresOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = GeneratedToken.Reconstitute(
            new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername,
            ValidPermissions, expiresOn,
            new DateTime(2023, 12, 1, 0, 0, 0, DateTimeKind.Utc), false, null);

        // Act
        var result = token.IsActive();

        // Assert
        result.Should().BeFalse();
    }
}
