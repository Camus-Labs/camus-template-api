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
            Permissions = view.Permissions.ToList(),
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
            excludeRevoked: query.ExcludeRevoked,
            excludeExpired: query.ExcludeExpired
        );
    }

    /// <summary>
    /// Converts sort query parameters to application-layer sort params.
    /// Returns null when no sort parameters are specified.
    /// </summary>
    /// <param name="query">The query from the API request.</param>
    /// <returns>Sort parameters for the application layer, or null if not specified.</returns>
    /// <exception cref="ArgumentException">Thrown when only one of sortBy/sortDirection is provided or when an invalid value is specified.</exception>
    /// <remarks>
    /// Validation lives here rather than in <see cref="GeneratedTokenSortParams"/> because the constructor
    /// accepts typed enums. String-to-enum coercion and the cross-property both-or-neither check are
    /// API-shape concerns that cannot move to the application-layer record.
    /// </remarks>
    public static GeneratedTokenSortParams? ToSortParams(this GetGeneratedTokensQuery query)
    {
        if (query.SortBy is null && query.SortDirection is null)
        {
            return null;
        }

        if (query.SortBy is null || query.SortDirection is null)
        {
            throw new ArgumentException("Both sortBy and sortDirection must be provided together.");
        }

        if (!Enum.TryParse<GeneratedTokenSortField>(query.SortBy, ignoreCase: true, out var field))
        {
            throw new ArgumentException($"Invalid value for sortBy: '{query.SortBy}'. Allowed values: tokenUsername, expiresOn, createdAt, revokedAt.");
        }

        if (!Enum.TryParse<emc.camus.application.Common.SortDirection>(query.SortDirection, ignoreCase: true, out var direction))
        {
            throw new ArgumentException($"Invalid value for sortDirection: '{query.SortDirection}'. Allowed values: asc, desc.");
        }

        return new GeneratedTokenSortParams(field, direction);
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
