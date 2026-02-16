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
}
