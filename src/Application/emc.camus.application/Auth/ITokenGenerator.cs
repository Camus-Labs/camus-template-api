namespace emc.camus.application.Auth;

/// <summary>
/// Provides functionality for generating authentication tokens.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates an authentication token with the specified claims.
    /// </summary>
    /// <param name="command">The command containing subject and optional additional claims.</param>
    /// <returns>A <see cref="GenerateTokenResult"/> containing the token and expiration information.</returns>
    GenerateTokenResult GenerateToken(GenerateTokenCommand command);
}
