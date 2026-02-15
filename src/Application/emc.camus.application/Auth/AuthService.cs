using System.Security.Claims;
using emc.camus.application.Common;

namespace emc.camus.application.Auth;

/// <summary>
/// Provides authentication services including credential validation and token generation.
/// Validates credentials via user repository and generates tokens for authenticated users.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository for user credential validation.</param>
    /// <param name="tokenGenerator">Generator for creating authentication tokens.</param>
    public AuthService(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    /// <summary>
    /// Authenticates user credentials and generates a token.
    /// </summary>
    /// <param name="command">The authentication command containing username and password.</param>
    /// <returns>Authentication result with token, expiration, and user information.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid.</exception>
    public virtual async Task<AuthenticateUserResult> AuthenticateAsync(AuthenticateUserCommand command)
    {
        // Validate credentials via repository (could be in-memory, database, LDAP, etc.)
        // Repository throws UnauthorizedAccessException with error codes if validation fails
        var user = await _userRepository.ValidateCredentialsAsync(command.Username, command.Password);

        // Generate token with user's roles
        var roleClaims = user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)).ToList();
        var tokenCommand = new GenerateTokenCommand(user.Username, roleClaims);
        var tokenResult = _tokenGenerator.GenerateToken(tokenCommand);

        // Return immutable result
        return new AuthenticateUserResult(
            tokenResult.Token,
            tokenResult.ExpiresOn
        );
    }
}
