using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
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

    /// <inheritdoc/>
    public AuthToken GenerateToken(Guid userId, string username, IEnumerable<Claim>? additionalClaims = null)
    {
        var jti = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return GenerateToken(userId, username, jti, expiresOn, additionalClaims);
    }

    /// <inheritdoc/>
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
