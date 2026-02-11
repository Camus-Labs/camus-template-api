using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using emc.camus.application.Auth;
using emc.camus.security.jwt.Configurations;

namespace emc.camus.security.jwt.Services;

/// <summary>
/// Provides JWT token generation functionality using RSA signing.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;
    private readonly ILogger<JwtTokenGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenGenerator"/> class.
    /// </summary>
    /// <param name="jwtSettings">JWT configuration settings.</param>
    /// <param name="signingCredentials">Signing credentials for token generation.</param>
    /// <param name="logger">Logger for token generation events.</param>
    public JwtTokenGenerator(
        JwtSettings jwtSettings,
        SigningCredentials signingCredentials,
        ILogger<JwtTokenGenerator> logger)
    {
        _jwtSettings = jwtSettings;
        _signingCredentials = signingCredentials;
        _logger = logger;
    }

    /// <inheritdoc/>
    public JwtTokenResult GenerateToken(string subject, IEnumerable<Claim>? additionalClaims = null)
    {
        _logger.LogDebug("Generating JWT token for subject: {Subject}", subject);

        // Build claims list
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.UniqueName, subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add additional claims if provided
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
            _logger.LogDebug("Added {Count} additional claims to token", additionalClaims.Count());
        }

        var expiresOn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var jwtToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresOn,
            signingCredentials: _signingCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        _logger.LogInformation("JWT token generated successfully for subject: {Subject}, expires: {ExpiresOn}", 
            subject, expiresOn);

        return new JwtTokenResult
        {
            Token = token,
            ExpiresOn = expiresOn
        };
    }
}
