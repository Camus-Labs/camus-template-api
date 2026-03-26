using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.domain.Auth;

namespace emc.camus.application.test.Auth;

public class AuthMappingExtensionsTests
{
    private static readonly Guid ValidCreatorUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidCreatorUsername = "admin";
    private static readonly Guid ValidJti = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidTokenUsername = "admin-token1";
    private static readonly List<string> ValidPermissions = ["api.read", "api.write"];
    private static readonly DateTime ValidExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime ValidCreatedAt = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    // --- ToSummaryView ---

    [Fact]
    public void ToSummaryView_ActiveToken_MapsAllProperties()
    {
        // Arrange
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername,
            ValidPermissions, ValidExpiration, ValidCreatedAt, false, null);

        // Act
        var view = token.ToSummaryView();

        // Assert
        view.Jti.Should().Be(ValidJti);
        view.TokenUsername.Should().Be(ValidTokenUsername);
        view.Permissions.Should().BeEquivalentTo(ValidPermissions);
        view.ExpiresOn.Should().Be(ValidExpiration);
        view.CreatedAt.Should().Be(ValidCreatedAt);
        view.IsRevoked.Should().BeFalse();
        view.RevokedAt.Should().BeNull();
        view.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ToSummaryView_RevokedToken_MapsRevokedState()
    {
        // Arrange
        var revokedAt = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername,
            ValidPermissions, ValidExpiration, ValidCreatedAt, true, revokedAt);

        // Act
        var view = token.ToSummaryView();

        // Assert
        view.IsRevoked.Should().BeTrue();
        view.RevokedAt.Should().Be(revokedAt);
        view.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ToSummaryView_ExpiredToken_MapsExpiredState()
    {
        // Arrange
        var pastExpiration = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidCreatorUserId, ValidCreatorUsername, ValidTokenUsername,
            ValidPermissions, pastExpiration, ValidCreatedAt, false, null);

        // Act
        var view = token.ToSummaryView();

        // Assert
        view.IsValid.Should().BeFalse();
    }

    // --- ToPermissionClaims ---

    [Fact]
    public void ToPermissionClaims_UserWithPermissions_ReturnsClaims()
    {
        // Arrange
        var role = new Role("testrole", permissions: ["api.read", "api.write"]);
        var user = new User("testuser", [role], ValidCreatorUserId);

        // Act
        var claims = user.ToPermissionClaims();

        // Assert
        claims.Should().HaveCount(2);
        claims.Should().Contain(c => c.Type == Permissions.ClaimType && c.Value == "api.read");
        claims.Should().Contain(c => c.Type == Permissions.ClaimType && c.Value == "api.write");
    }

    [Fact]
    public void ToPermissionClaims_UserWithNoPermissions_ReturnsEmptyList()
    {
        // Arrange
        var role = new Role("emptyrole");
        var user = new User("testuser", [role], ValidCreatorUserId);

        // Act
        var claims = user.ToPermissionClaims();

        // Assert
        claims.Should().BeEmpty();
    }

    [Fact]
    public void ToPermissionClaims_UserWithDuplicatePermissionsAcrossRoles_ReturnsDistinctClaims()
    {
        // Arrange
        var role1 = new Role("role1", permissions: ["api.read", "api.write"]);
        var role2 = new Role("role2", permissions: ["api.read", "token.create"]);
        var user = new User("testuser", [role1, role2], ValidCreatorUserId);

        // Act
        var claims = user.ToPermissionClaims();

        // Assert
        claims.Should().HaveCount(3);
        claims.Select(c => c.Value).Should().BeEquivalentTo(["api.read", "api.write", "token.create"]);
    }
}
