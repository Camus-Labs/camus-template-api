using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Requests.V2;
using emc.camus.api.Models.Responses.V2;
using emc.camus.application.Auth;

namespace emc.camus.api.Mapping.V2;

/// <summary>
/// Extension methods for mapping between API V2 DTOs and Application layer Commands/Results/Views.
/// </summary>
public static class AuthMappingExtensions
{
    /// <summary>
    /// Converts a LoginRequest to an AuthenticateUserCommand.
    /// </summary>
    /// <param name="request">The login request from the API.</param>
    /// <returns>An authentication command for the application layer.</returns>
    public static AuthenticateUserCommand ToCommand(this AuthenticateUserRequest request)
    {
        return new AuthenticateUserCommand(
            request.Username,
            request.Password
        );
    }

    /// <summary>
    /// Converts an AuthenticateUserResult to an AuthenticateUserResponse.
    /// </summary>
    /// <param name="result">The authentication result from the application layer.</param>
    /// <returns>An AuthenticateUserResponse DTO for the API layer.</returns>
    public static AuthenticateUserResponse ToResponse(this AuthenticateUserResult result)
    {
        return new AuthenticateUserResponse
        {
            Token = result.Token,
            ExpiresOn = result.ExpiresOn
        };
    }

    /// <summary>
    /// Converts a GenerateTokenRequest to a GenerateTokenCommand.
    /// </summary>
    /// <param name="request">The generate token request from the API.</param>
    /// <returns>A generate token command for the application layer.</returns>
    public static GenerateTokenCommand ToCommand(this GenerateTokenRequest request)
    {
        return new GenerateTokenCommand(
            request.UsernameSuffix,
            request.ExpiresOn,
            request.Permissions
        );
    }

    /// <summary>
    /// Converts a GenerateTokenResult to a GenerateTokenResponse.
    /// </summary>
    /// <param name="result">The generate token result from the application layer.</param>
    /// <returns>A GenerateTokenResponse DTO for the API layer.</returns>
    public static GenerateTokenResponse ToResponse(this GenerateTokenResult result)
    {
        return new GenerateTokenResponse
        {
            Token = result.Token,
            ExpiresOn = result.ExpiresOn,
            TokenUsername = result.TokenUsername
        };
    }

    /// <summary>
    /// Converts a GeneratedTokenSummaryView to a GeneratedTokenSummaryDto.
    /// </summary>
    /// <param name="view">The token summary view from the application layer.</param>
    /// <returns>A GeneratedTokenSummaryDto for the API layer.</returns>
    public static GeneratedTokenSummaryDto ToDto(this GeneratedTokenSummaryView view)
    {
        return new GeneratedTokenSummaryDto
        {
            Jti = view.Jti,
            TokenUsername = view.TokenUsername,
            Permissions = view.Permissions,
            ExpiresOn = view.ExpiresOn,
            CreatedAt = view.CreatedAt,
            IsRevoked = view.IsRevoked,
            RevokedAt = view.RevokedAt,
            IsValid = view.IsValid
        };
    }

    /// <summary>
    /// Converts a GetGeneratedTokensQuery (API layer) to a GeneratedTokenFilter (Application layer).
    /// </summary>
    /// <param name="query">The query from the API request.</param>
    /// <returns>A filter for the application layer.</returns>
    public static GeneratedTokenFilter ToFilter(this GetGeneratedTokensQuery query)
    {
        return new GeneratedTokenFilter(
            ExcludeRevoked: query.ExcludeRevoked,
            ExcludeExpired: query.ExcludeExpired
        );
    }

    /// <summary>
    /// Creates a <see cref="RevokeTokenCommand"/> from a JTI.
    /// </summary>
    /// <param name="jti">The JWT ID from the route.</param>
    /// <returns>A revoke token command for the application layer.</returns>
    public static RevokeTokenCommand ToRevokeTokenCommand(Guid jti)
    {
        return new RevokeTokenCommand(jti);
    }
}
