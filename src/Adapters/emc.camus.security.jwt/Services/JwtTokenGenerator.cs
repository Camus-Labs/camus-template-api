using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using emc.camus.application.Auth;
using emc.camus.domain.Auth;
using emc.camus.security.jwt.Configurations;

namespace emc.camus.security.jwt.Services;

/// <summary>
/// Provides JWT token generation functionality using RSA signing.
/// </summary>
public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenGenerator"/> class.
    /// </summary>
    /// <param name="jwtSettings">JWT configuration settings.</param>
    /// <param name="signingCredentials">Signing credentials for token generation.</param>
    public JwtTokenGenerator(
        JwtSettings jwtSettings,
        SigningCredentials signingCredentials)
    {
        ArgumentNullException.ThrowIfNull(jwtSettings);
        ArgumentNullException.ThrowIfNull(signingCredentials);

        _jwtSettings = jwtSettings;
        _signingCredentials = signingCredentials;
    }

    /// <summary>
    /// Generates an authentication token with a default JTI and configured expiration.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <param name="username">The username for the user.</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>An <see cref="AuthToken"/> containing the generated token and expiration information.</returns>
    public AuthToken GenerateToken(Guid userId, string username, IEnumerable<Claim>? additionalClaims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var jti = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return GenerateToken(userId, username, jti, expiresOn, additionalClaims);
    }

    /// <summary>
    /// Generates an authentication token with a specific JTI, custom expiration, and specified claims.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <param name="username">The username for the user.</param>
    /// <param name="jti">The JWT ID for unique token identification.</param>
    /// <param name="expiresOn">The custom expiration date and time (UTC).</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>An <see cref="AuthToken"/> containing the generated token and expiration information.</returns>
    public AuthToken GenerateToken(Guid userId, string username, Guid jti, DateTime expiresOn, IEnumerable<Claim>? additionalClaims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        
        // Build claims list
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // For HttpUserContext.GetCurrentUserId()
            new Claim(ClaimTypes.Name, username),         // For HttpUserContext.GetCurrentUsername() via Identity.Name
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        // Add additional claims if provided
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var jwtToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresOn,
            signingCredentials: _signingCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return new AuthToken(token, expiresOn);
    }
}
