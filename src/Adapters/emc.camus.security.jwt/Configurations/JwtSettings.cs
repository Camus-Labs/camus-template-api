namespace emc.camus.security.jwt.Configurations;

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The configuration section name for JWT settings.
    /// </summary>
    public const string ConfigurationSectionName = "JwtSettings";

    /// <summary>
    /// Maximum allowed token expiration time in minutes (30 days).
    /// </summary>
    public const int MaxExpirationMinutes = 43200;
    
    /// <summary>
    /// Gets or sets the name of the secret key used to retrieve the RSA private key from the secret provider.
    /// Defaults to "RsaPrivateKeyPem".
    /// </summary>
    public string SecretKeyName { get; set; } = "RsaPrivateKeyPem";
    
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

    /// <summary>
    /// Validates the JWT settings configuration.
    /// Throws ArgumentException if any setting is invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKeyName))
            throw new ArgumentException("SecretKeyName cannot be null or empty", nameof(SecretKeyName));

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new ArgumentException("Issuer cannot be null or empty", nameof(Issuer));

        // Validate Issuer is a valid URL
        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out _))
            throw new ArgumentException($"Issuer must be a valid absolute URL: '{Issuer}'", nameof(Issuer));

        if (string.IsNullOrWhiteSpace(Audience))
            throw new ArgumentException("Audience cannot be null or empty", nameof(Audience));

        // Validate Audience is a valid URL
        if (!Uri.TryCreate(Audience, UriKind.Absolute, out _))
            throw new ArgumentException($"Audience must be a valid absolute URL: '{Audience}'", nameof(Audience));

        if (ExpirationMinutes <= 0 || ExpirationMinutes > MaxExpirationMinutes)
            throw new ArgumentException($"ExpirationMinutes must be between 1 and {MaxExpirationMinutes} (30 days)", nameof(ExpirationMinutes));
    }
}
