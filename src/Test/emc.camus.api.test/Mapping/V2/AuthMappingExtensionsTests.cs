using FluentAssertions;
using emc.camus.api.Mapping.V2;
using emc.camus.api.Models.Requests.V2;
using emc.camus.application.Auth;

namespace emc.camus.api.test.Mapping.V2;

public class AuthMappingExtensionsTests
{
    private static readonly DateTime FixedExpiration = new(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime FixedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FixedJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FixedUserId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // --- ToCommand (AuthenticateUserRequest) ---

    [Fact]
    public void ToCommand_AuthenticateUserRequest_MapsUsernameAndPassword()
    {
        // Arrange
        var request = new AuthenticateUserRequest
        {
            Username = "testuser",
            Password = "securepass"
        };

        // Act
        var command = request.ToCommand();

        // Assert
        command.Username.Should().Be("testuser");
        command.Password.Should().Be("securepass");
    }

    // --- ToResponse (AuthenticateUserResult) ---

    [Fact]
    public void ToResponse_AuthenticateUserResult_MapsTokenAndExpiration()
    {
        // Arrange
        var result = new AuthenticateUserResult("jwt-token-value", FixedExpiration);

        // Act
        var response = result.ToResponse();

        // Assert
        response.Token.Should().Be("jwt-token-value");
        response.ExpiresOn.Should().Be(FixedExpiration);
    }

    // --- ToCommand (GenerateTokenRequest) ---

    [Fact]
    public void ToCommand_GenerateTokenRequest_MapsAllProperties()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            UsernameSuffix = "ci-deploy",
            ExpiresOn = FixedExpiration,
            Permissions = new List<string> { "api.read", "api.write" }
        };

        // Act
        var command = request.ToCommand();

        // Assert
        command.UsernameSuffix.Should().Be("ci-deploy");
        command.ExpiresOn.Should().Be(FixedExpiration);
        command.Permissions.Should().BeEquivalentTo(new List<string> { "api.read", "api.write" });
    }

    // --- ToResponse (GenerateTokenResult) ---

    [Fact]
    public void ToResponse_GenerateTokenResult_MapsAllProperties()
    {
        // Arrange
        var result = new GenerateTokenResult(
            "generated-token",
            FixedExpiration,
            FixedUserId,
            "adminuser",
            "adminuser-ci-deploy");

        // Act
        var response = result.ToResponse();

        // Assert
        response.Token.Should().Be("generated-token");
        response.ExpiresOn.Should().Be(FixedExpiration);
        response.TokenUsername.Should().Be("adminuser-ci-deploy");
    }

    // --- ToDto (GeneratedTokenSummaryView) ---

    [Fact]
    public void ToDto_GeneratedTokenSummaryView_MapsAllProperties()
    {
        // Arrange
        var revokedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var view = new GeneratedTokenSummaryView(
            jti: FixedJti,
            tokenUsername: "admin-token1",
            permissions: new List<string> { "api.read" },
            expiresOn: FixedExpiration,
            createdAt: FixedCreatedAt,
            isRevoked: true,
            revokedAt: revokedAt,
            isValid: false);

        // Act
        var dto = view.ToDto();

        // Assert
        dto.Jti.Should().Be(FixedJti);
        dto.TokenUsername.Should().Be("admin-token1");
        dto.Permissions.Should().BeEquivalentTo(new List<string> { "api.read" });
        dto.ExpiresOn.Should().Be(FixedExpiration);
        dto.CreatedAt.Should().Be(FixedCreatedAt);
        dto.IsRevoked.Should().BeTrue();
        dto.RevokedAt.Should().Be(revokedAt);
        dto.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ToDto_ActiveToken_MapsIsValidTrue()
    {
        // Arrange
        var view = new GeneratedTokenSummaryView(
            jti: FixedJti,
            tokenUsername: "admin-token1",
            permissions: new List<string> { "api.read" },
            expiresOn: FixedExpiration,
            createdAt: FixedCreatedAt,
            isRevoked: false,
            revokedAt: null,
            isValid: true);

        // Act
        var dto = view.ToDto();

        // Assert
        dto.IsRevoked.Should().BeFalse();
        dto.RevokedAt.Should().BeNull();
        dto.IsValid.Should().BeTrue();
    }

    // --- ToFilter (GetGeneratedTokensQuery) ---

    [Fact]
    public void ToFilter_GetGeneratedTokensQuery_MapsFilterProperties()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery
        {
            ExcludeRevoked = true,
            ExcludeExpired = true
        };

        // Act
        var filter = query.ToFilter();

        // Assert
        filter.ExcludeRevoked.Should().BeTrue();
        filter.ExcludeExpired.Should().BeTrue();
    }

    [Fact]
    public void ToFilter_DefaultQuery_MapsDefaultFilterValues()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery();

        // Act
        var filter = query.ToFilter();

        // Assert
        filter.ExcludeRevoked.Should().BeFalse();
        filter.ExcludeExpired.Should().BeFalse();
    }

    // --- ToRevokeTokenCommand ---

    [Fact]
    public void ToRevokeTokenCommand_ValidJti_CreatesCommand()
    {
        // Arrange
        var jti = FixedJti;

        // Act
        var command = emc.camus.api.Mapping.V2.AuthMappingExtensions.ToRevokeTokenCommand(jti);

        // Assert
        command.Jti.Should().Be(FixedJti);
    }
}
