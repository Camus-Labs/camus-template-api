using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using emc.camus.application.Auth;
using emc.camus.domain.Auth;
using Microsoft.IdentityModel.Tokens;
using emc.camus.api.Configurations;
using emc.camus.api.Exceptions;

namespace emc.camus.api.Utilities;

/// <summary>
/// Provides JWT token generation functionality using RSA signing.
/// </summary>
public sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenGenerator"/> class.
    /// </summary>
    /// <param name="jwtSettings">JWT configuration settings.</param>
    /// <param name="signingCredentials">Signing credentials for token generation.</param>
    /// <param name="timeProvider">Time provider for token expiration and issued-at calculations.</param>
    public JwtTokenGenerator(
        JwtSettings jwtSettings,
        SigningCredentials signingCredentials,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(jwtSettings);
        ArgumentNullException.ThrowIfNull(signingCredentials);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _jwtSettings = jwtSettings;
        _signingCredentials = signingCredentials;
        _timeProvider = timeProvider;
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
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var jti = Guid.NewGuid();
        var expiresOn = _timeProvider.GetUtcNow().DateTime.AddMinutes(_jwtSettings.ExpirationMinutes);

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
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(expiresOn, default);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        try
        {
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
        catch (Exception ex)
        {
            throw new JwtTokenGenerationException("Failed to generate JWT token.", ex);
        }
    }
}
