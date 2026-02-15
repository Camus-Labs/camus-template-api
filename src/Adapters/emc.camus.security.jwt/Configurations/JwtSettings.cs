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

    private const int MinExpirationMinutes = 1;
    private const int MaxExpirationMinutes = 43200;
    private const int MaxIssuerLength = 200;
    private const int MaxAudienceLength = 200;
    private const int MaxRsaPrivateKeySecretNameLength = 50;
    
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
    /// Secret name for the RSA private key PEM used in JWT token signing.
    /// </summary>
    public string RsaPrivateKeySecretName { get; set; } = "RsaPrivateKeyPem";

    /// <summary>
    /// Validates the JWT settings configuration.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateIssuer();
        ValidateAudience();
        ValidateExpirationMinutes();
        ValidateRsaPrivateKeySecretName();
    }

    private void ValidateIssuer()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new ArgumentException("Issuer cannot be null or empty.", nameof(Issuer));
        }

        if (Issuer.Length > MaxIssuerLength)
        {
            throw new ArgumentException($"Issuer must not exceed {MaxIssuerLength} characters. Current length: {Issuer.Length}", nameof(Issuer));
        }

        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Issuer must be a valid absolute URL: '{Issuer}'", nameof(Issuer));
        }
    }

    private void ValidateAudience()
    {
        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new ArgumentException("Audience cannot be null or empty.", nameof(Audience));
        }

        if (Audience.Length > MaxAudienceLength)
        {
            throw new ArgumentException($"Audience must not exceed {MaxAudienceLength} characters. Current length: {Audience.Length}", nameof(Audience));
        }

        if (!Uri.TryCreate(Audience, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Audience must be a valid absolute URL: '{Audience}'", nameof(Audience));
        }
    }

    private void ValidateExpirationMinutes()
    {
        if (ExpirationMinutes < MinExpirationMinutes || ExpirationMinutes > MaxExpirationMinutes)
        {
            throw new ArgumentException($"ExpirationMinutes must be between {MinExpirationMinutes} and {MaxExpirationMinutes} (30 days). Current value: {ExpirationMinutes}", nameof(ExpirationMinutes));
        }
    }

    private void ValidateRsaPrivateKeySecretName()
    {
        if (string.IsNullOrWhiteSpace(RsaPrivateKeySecretName))
        {
            throw new ArgumentException("RsaPrivateKeySecretName cannot be null or empty.", nameof(RsaPrivateKeySecretName));
        }

        if (RsaPrivateKeySecretName.Length > MaxRsaPrivateKeySecretNameLength)
        {
            throw new ArgumentException($"RsaPrivateKeySecretName must not exceed {MaxRsaPrivateKeySecretNameLength} characters. Current length: {RsaPrivateKeySecretName.Length}", nameof(RsaPrivateKeySecretName));
        }
    }
}

