using System.Linq;
using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.domain.Auth;
using Microsoft.Extensions.Time.Testing;

namespace emc.camus.application.test.Auth;

public class AuthMappingExtensionsTests
{
    private static readonly Guid ValidJti = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidTokenUsername = "admin-token1";
    private static readonly IReadOnlyList<string> ValidPermissions = [Permissions.ApiRead, Permissions.ApiWrite];
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiration = FixedNow.UtcDateTime.AddYears(1).AddDays(-1);
    private static readonly DateTime ValidCreatedAt = FixedNow.UtcDateTime.AddYears(-1);

    // --- ToSummaryView ---

    [Fact]
    public void ToSummaryView_ActiveToken_MapsAllProperties()
    {
        // Arrange
        var token = GeneratedToken.Reconstitute(
            ValidJti, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "admin", ValidTokenUsername,
            ValidPermissions.ToList(), ValidExpiration, ValidCreatedAt, false, null,
            timeProvider: new FakeTimeProvider(FixedNow));

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
        var revokedAt = FixedNow.UtcDateTime.AddMonths(-6);
        var token = GeneratedToken.Reconstitute(
            ValidJti, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "admin", ValidTokenUsername,
            ValidPermissions.ToList(), ValidExpiration, ValidCreatedAt, true, revokedAt,
            timeProvider: new FakeTimeProvider(FixedNow));

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
        var pastExpiration = FixedNow.UtcDateTime.AddMonths(-6);
        var token = GeneratedToken.Reconstitute(
            ValidJti, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "admin", ValidTokenUsername,
            ValidPermissions.ToList(), pastExpiration, ValidCreatedAt, false, null,
            timeProvider: new FakeTimeProvider(FixedNow));

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
        var role = new Role("testrole", permissions: [Permissions.ApiRead, Permissions.ApiWrite]);
        var user = new User("testuser", [role], new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Act
        var claims = user.ToPermissionClaims();

        // Assert
        claims.Should().HaveCount(2);
        claims.Should().Contain(c => c.Type == Permissions.ClaimType && c.Value == Permissions.ApiRead);
        claims.Should().Contain(c => c.Type == Permissions.ClaimType && c.Value == Permissions.ApiWrite);
    }

    [Fact]
    public void ToPermissionClaims_UserWithNoPermissions_ReturnsEmptyList()
    {
        // Arrange
        var role = new Role("emptyrole");
        var user = new User("testuser", [role], new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Act
        var claims = user.ToPermissionClaims();

        // Assert
        claims.Should().BeEmpty();
    }

    [Fact]
    public void ToPermissionClaims_UserWithDuplicatePermissionsAcrossRoles_ReturnsDistinctClaims()
    {
        // Arrange
        var role1 = new Role("role1", permissions: [Permissions.ApiRead, Permissions.ApiWrite]);
        var role2 = new Role("role2", permissions: [Permissions.ApiRead, Permissions.TokenCreate]);
        var user = new User("testuser", [role1, role2], new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        // Act
        var claims = user.ToPermissionClaims();

        // Assert
        claims.Should().HaveCount(3);
        claims.Select(c => c.Value).Should().BeEquivalentTo([Permissions.ApiRead, Permissions.ApiWrite, Permissions.TokenCreate]);
    }
}
