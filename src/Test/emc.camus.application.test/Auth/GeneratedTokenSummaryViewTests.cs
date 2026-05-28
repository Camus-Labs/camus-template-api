using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class GeneratedTokenSummaryViewTests
{
    private static readonly Guid ValidJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidTokenUsername = "admin-token1";
    private static readonly IReadOnlyList<string> ValidPermissions = new List<string> { Permissions.ApiRead, Permissions.ApiWrite };
    private static readonly DateTimeOffset ReferenceTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiration = ReferenceTime.UtcDateTime.AddYears(1);
    private static readonly DateTime ValidCreatedAt = ReferenceTime.UtcDateTime.AddYears(-2);

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Act
        var view = new GeneratedTokenSummaryView(ValidJti, ValidTokenUsername, ValidPermissions, ValidExpiration, ValidCreatedAt, false, null, true);

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
