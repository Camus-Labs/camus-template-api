namespace emc.camus.security.jwt.Configurations;

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The issuer of the JWT token (usually your API's URL).
    /// </summary>
    public string Issuer { get; set; } = "https://auth.camus.com/";

    /// <summary>
    /// The audience for the JWT token (usually your client app's URL).
    /// </summary>
    public string Audience { get; set; } = "https://app.camus.com/";

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
