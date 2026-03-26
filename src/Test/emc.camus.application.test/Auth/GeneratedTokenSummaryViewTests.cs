using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class GeneratedTokenSummaryViewTests
{
    private static readonly Guid ValidJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidTokenUsername = "admin-token1";
    private static readonly IReadOnlyList<string> ValidPermissions = new List<string> { "api.read", "api.write" };
    private static readonly DateTime ValidExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime ValidCreatedAt = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var jti = ValidJti;
        var tokenUsername = ValidTokenUsername;
        var permissions = ValidPermissions;
        var expiresOn = ValidExpiration;
        var createdAt = ValidCreatedAt;
        var isRevoked = false;
        DateTime? revokedAt = null;
        var isValid = true;

        // Act
        var view = new GeneratedTokenSummaryView(jti, tokenUsername, permissions, expiresOn, createdAt, isRevoked, revokedAt, isValid);

        // Assert
        view.Jti.Should().Be(jti);
        view.TokenUsername.Should().Be(tokenUsername);
        view.Permissions.Should().BeEquivalentTo(permissions);
        view.ExpiresOn.Should().Be(expiresOn);
        view.CreatedAt.Should().Be(createdAt);
        view.IsRevoked.Should().BeFalse();
        view.RevokedAt.Should().BeNull();
        view.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyJti_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyJti = Guid.Empty;

        // Act
        var act = () => new GeneratedTokenSummaryView(emptyJti, ValidTokenUsername, ValidPermissions, ValidExpiration, ValidCreatedAt, false, null, true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("jti");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTokenUsername_ThrowsArgumentException(string? tokenUsername)
    {
        // Arrange
        // Act
        var act = () => new GeneratedTokenSummaryView(ValidJti, tokenUsername!, ValidPermissions, ValidExpiration, ValidCreatedAt, false, null, true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("tokenUsername");
    }

    [Fact]
    public void Constructor_NullPermissions_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new GeneratedTokenSummaryView(ValidJti, ValidTokenUsername, null!, ValidExpiration, ValidCreatedAt, false, null, true);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("permissions");
    }

    [Fact]
    public void Constructor_DefaultExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var defaultDate = default(DateTime);

        // Act
        var act = () => new GeneratedTokenSummaryView(ValidJti, ValidTokenUsername, ValidPermissions, defaultDate, ValidCreatedAt, false, null, true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("expiresOn");
    }

    [Fact]
    public void Constructor_DefaultCreatedAt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var defaultDate = default(DateTime);

        // Act
        var act = () => new GeneratedTokenSummaryView(ValidJti, ValidTokenUsername, ValidPermissions, ValidExpiration, defaultDate, false, null, true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("createdAt");
    }
}
