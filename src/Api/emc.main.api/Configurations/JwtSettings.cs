namespace emc.camus.main.api.Configurations;

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The issuer of the JWT token (usually your API's URL).
    /// </summary>
    public string Issuer { get; set; } = "https://auth.camuslabs.com/";

    /// <summary>
    /// The audience for the JWT token (usually your client app's URL).
    /// </summary>
    public string Audience { get; set; } = "https://app.camuslabs.com/";

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Optional RSA private key PEM file path for RSA signing.
    /// </summary>
    public string? RsaPrivateKeyPem { get; set; }
}
