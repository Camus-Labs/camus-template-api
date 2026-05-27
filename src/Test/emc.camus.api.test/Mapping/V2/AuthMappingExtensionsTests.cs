using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using emc.camus.api.Mapping.V2;
using emc.camus.api.Models.Requests.V2;
using emc.camus.application.Auth;

namespace emc.camus.api.test.Mapping.V2;

public class AuthMappingExtensionsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiresOn = FixedNow.UtcDateTime.AddYears(1).AddDays(-1);
    private static readonly DateTime ValidCreatedAt = FixedNow.UtcDateTime;
    private static readonly List<string> PermissionsReadWrite = ["api.read", "api.write"];
    private static readonly List<string> PermissionsRead = ["api.read"];
    private static readonly Guid TestJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
        command.Username.Should().Be(request.Username);
        command.Password.Should().Be(request.Password);
    }

    // --- ToResponse (AuthenticateUserResult) ---

    [Fact]
    public void ToResponse_AuthenticateUserResult_MapsTokenAndExpiration()
    {
        // Arrange
        var result = new AuthenticateUserResult("jwt-token-value", ValidExpiresOn);

        // Act
        var response = result.ToResponse();

        // Assert
        response.Token.Should().Be(result.Token);
        response.ExpiresOn.Should().Be(result.ExpiresOn);
    }

    // --- ToCommand (GenerateTokenRequest) ---

    [Fact]
    public void ToCommand_GenerateTokenRequest_MapsAllProperties()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            UsernameSuffix = "ci-deploy",
            ExpiresOn = ValidExpiresOn,
            Permissions = PermissionsReadWrite
        };

        // Act
        var command = request.ToCommand();

        // Assert
        command.UsernameSuffix.Should().Be(request.UsernameSuffix);
        command.ExpiresOn.Should().Be(request.ExpiresOn);
        command.Permissions.Should().BeEquivalentTo(request.Permissions);
    }

    // --- ToResponse (GenerateTokenResult) ---

    [Fact]
    public void ToResponse_GenerateTokenResult_MapsAllProperties()
    {
        // Arrange
        var result = new GenerateTokenResult(
            "generated-token",
            ValidExpiresOn,
            "adminuser-ci-deploy");

        // Act
        var response = result.ToResponse();

        // Assert
        response.Token.Should().Be(result.Token);
        response.ExpiresOn.Should().Be(result.ExpiresOn);
        response.TokenUsername.Should().Be(result.TokenUsername);
    }

    // --- ToDto (GeneratedTokenSummaryView) ---

    [Fact]
    public void ToDto_GeneratedTokenSummaryView_MapsAllProperties()
    {
        // Arrange
        var revokedAt = FixedNow.UtcDateTime.AddMonths(6);
        var view = new GeneratedTokenSummaryView(
            jti: TestJti,
            tokenUsername: "admin-token1",
            permissions: PermissionsRead,
            expiresOn: ValidExpiresOn,
            createdAt: ValidCreatedAt,
            isRevoked: true,
            revokedAt: revokedAt,
            isValid: false);

        // Act
        var dto = view.ToDto();

        // Assert
        dto.Jti.Should().Be(view.Jti);
        dto.TokenUsername.Should().Be(view.TokenUsername);
        dto.Permissions.Should().BeEquivalentTo(view.Permissions);
        dto.ExpiresOn.Should().Be(view.ExpiresOn);
        dto.CreatedAt.Should().Be(view.CreatedAt);
        dto.IsRevoked.Should().BeTrue();
        dto.RevokedAt.Should().Be(revokedAt);
        dto.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ToDto_ActiveToken_MapsIsValidTrue()
    {
        // Arrange
        var view = new GeneratedTokenSummaryView(
            jti: TestJti,
            tokenUsername: "admin-token1",
            permissions: PermissionsRead,
            expiresOn: ValidExpiresOn,
            createdAt: ValidCreatedAt,
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

    // --- ToSortParams (GetGeneratedTokensQuery) ---

    [Fact]
    public void ToSortParams_BothNull_ReturnsInstanceWithNoSorting()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery();

        // Act
        var result = query.ToSortParams();

        // Assert
        result.Should().NotBeNull();
        result.Field.Should().BeNull();
    }

    [Theory]
    [InlineData("createdAt", "desc", GeneratedTokenSortField.CreatedAt, emc.camus.application.Common.SortDirection.Desc)]
    [InlineData("expiresOn", "asc", GeneratedTokenSortField.ExpiresOn, emc.camus.application.Common.SortDirection.Asc)]
    [InlineData("tokenUsername", "asc", GeneratedTokenSortField.TokenUsername, emc.camus.application.Common.SortDirection.Asc)]
    [InlineData("revokedAt", "desc", GeneratedTokenSortField.RevokedAt, emc.camus.application.Common.SortDirection.Desc)]
    public void ToSortParams_ValidFieldAndDirection_ReturnsMappedSortParams(
        string sortBy, string sortDirection, GeneratedTokenSortField expectedField, emc.camus.application.Common.SortDirection expectedDirection)
    {
        // Arrange
        var query = new GetGeneratedTokensQuery
        {
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        // Act
        var result = query.ToSortParams();

        // Assert
        result.Should().NotBeNull();
        result.Field.Should().Be(expectedField);
        result.Direction.Should().Be(expectedDirection);
    }

    // --- ToRevokeTokenCommand ---

    [Fact]
    public void ToRevokeTokenCommand_ValidJti_CreatesCommand()
    {
        // Arrange
        var jti = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var command = emc.camus.api.Mapping.V2.AuthMappingExtensions.ToRevokeTokenCommand(jti);

        // Assert
        command.Jti.Should().Be(jti);
    }
}
