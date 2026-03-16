using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class GeneratedTokenTests
{
    private static readonly Guid ValidCreatorUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidCreatorUsername = "admin";
    private const string ValidTokenUsername = "admin-token1";
    private readonly List<string> ValidPermissions = ["read", "write"];
    private static readonly DateTime ValidExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsAllProperties()
    {
        // Arrange
        var creatorUserId = ValidCreatorUserId;
        var creatorUsername = ValidCreatorUsername;
        var tokenUsername = ValidTokenUsername;
        var permissions = new List<string> { "read", "write" };
        var expiresOn = ValidExpiration;

        // Act
        var token = new GeneratedToken(creatorUserId, creatorUsername, tokenUsername, permissions, expiresOn);

        // Assert
        token.Jti.Should().NotBeEmpty();
        token.CreatorUserId.Should().Be(creatorUserId);
        token.CreatorUsername.Should().Be(creatorUsername);
        token.TokenUsername.Should().Be(tokenUsername);
        token.Permissions.Should().BeEquivalentTo(permissions);
        token.ExpiresOn.Should().Be(expiresOn);
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyCreatorUserId_ThrowsArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        var act = () => new GeneratedToken(emptyId, ValidCreatorUsername, ValidTokenUsername, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*not*equal*")
            .And.ParamName.Should().Be("creatorUserId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidCreatorUsername_ThrowsArgumentException(string? creatorUsername)
    {
        // Arrange
        // Act
        var act = () => new GeneratedToken(ValidCreatorUserId, creatorUsername!, ValidTokenUsername, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("creatorUsername");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTokenUsername_ThrowsArgumentException(string? tokenUsername)
    {
        // Arrange
        // Act
        var act = () => new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, tokenUsername!, ValidPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("tokenUsername");
    }

    [Fact]
    public void Constructor_NullPermissions_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, null!, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("permissions");
    }

    [Fact]
    public void Constructor_EmptyPermissions_ThrowsArgumentException()
    {
        // Arrange
        var emptyPermissions = new List<string>();

        // Act
        var act = () => new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, emptyPermissions, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*permission*")
            .And.ParamName.Should().Be("permissions.Count");
    }

    [Fact]
    public void Constructor_PastExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pastDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var act = () => new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, ValidPermissions, pastDate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*greater*")
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
        var token = new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, new List<string> { "read" }, ValidExpiration);

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, new List<string> { "read" }, ValidExpiration);
        token.Revoke();

        // Act
        var act = () => token.Revoke();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already revoked*");
    }

    // --- IsActive ---

    [Fact]
    public void IsActive_NotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var token = new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, new List<string> { "read" }, ValidExpiration);

        // Act
        var result = token.IsActive();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Revoked_ReturnsFalse()
    {
        // Arrange
        var token = new GeneratedToken(ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername, new List<string> { "read" }, ValidExpiration);
        token.Revoke();

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
