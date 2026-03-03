using emc.camus.api.Models.Dtos;
using emc.camus.api.Models.Requests;
using emc.camus.api.Models.Responses;
using emc.camus.application.Auth;

namespace emc.camus.api.Mapping;

/// <summary>
/// Extension methods for mapping between API DTOs and Application layer Commands/Results.
/// </summary>
public static class AuthMappingExtensions
{
    /// <summary>
    /// Converts a LoginRequest to an AuthenticateUserCommand.
    /// </summary>
    /// <param name="request">The login request from the API.</param>
    /// <returns>An authentication command for the application layer.</returns>
    /// <exception cref="ArgumentException">Thrown when username or password is null, empty, or whitespace.</exception>
    public static AuthenticateUserCommand ToCommand(this AuthenticateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Username is required and cannot be empty or whitespace.", nameof(request.Username));
        }
        
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required and cannot be empty or whitespace.", nameof(request.Password));
        }
        
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
        ArgumentNullException.ThrowIfNull(result);
        
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
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static GenerateTokenCommand ToCommand(this GenerateTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (string.IsNullOrWhiteSpace(request.UsernameSuffix))
        {
            throw new ArgumentException("Username suffix is required and cannot be empty or whitespace.", nameof(request.UsernameSuffix));
        }

        if (request.Permissions == null || request.Permissions.Count == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(request.Permissions));
        }
        
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
        ArgumentNullException.ThrowIfNull(result);
        
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
        ArgumentNullException.ThrowIfNull(view);

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
        ArgumentNullException.ThrowIfNull(query);
        return new GeneratedTokenFilter(
            ExcludeRevoked: query.ExcludeRevoked,
            ExcludeExpired: query.ExcludeExpired
        );
    }
}
