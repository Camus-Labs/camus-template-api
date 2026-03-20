namespace emc.camus.security.jwt.Configurations;

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
internal class JwtSettings
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
    private const string DefaultIssuer = "https://auth.camus.com/";
    private const string DefaultAudience = "https://app.camus.com/";
    private const int DefaultExpirationMinutes = 60;
    private const string DefaultRsaPrivateKeySecretName = "RsaPrivateKeyPem";

    /// <summary>
    /// The issuer of the JWT token (usually your API's URL).
    /// </summary>
    public string Issuer { get; set; } = DefaultIssuer;

    /// <summary>
    /// The audience for the JWT token (usually your client app's URL).
    /// </summary>
    public string Audience { get; set; } = DefaultAudience;

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = DefaultExpirationMinutes;

    /// <summary>
    /// Secret name for the RSA private key PEM used in JWT token signing.
    /// </summary>
    public string RsaPrivateKeySecretName { get; set; } = DefaultRsaPrivateKeySecretName;

    /// <summary>
    /// Validates the JWT settings configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
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
            throw new InvalidOperationException("Issuer cannot be null or empty.");
        }

        if (Issuer.Length > MaxIssuerLength)
        {
            throw new InvalidOperationException($"Issuer must not exceed {MaxIssuerLength} characters. Current length: {Issuer.Length}");
        }

        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Issuer must be a valid absolute URL: '{Issuer}'");
        }
    }

    private void ValidateAudience()
    {
        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Audience cannot be null or empty.");
        }

        if (Audience.Length > MaxAudienceLength)
        {
            throw new InvalidOperationException($"Audience must not exceed {MaxAudienceLength} characters. Current length: {Audience.Length}");
        }

        if (!Uri.TryCreate(Audience, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Audience must be a valid absolute URL: '{Audience}'");
        }
    }

    private void ValidateExpirationMinutes()
    {
        if (ExpirationMinutes < MinExpirationMinutes || ExpirationMinutes > MaxExpirationMinutes)
        {
            throw new InvalidOperationException($"ExpirationMinutes must be between {MinExpirationMinutes} and {MaxExpirationMinutes} (30 days). Current value: {ExpirationMinutes}");
        }
    }

    private void ValidateRsaPrivateKeySecretName()
    {
        if (string.IsNullOrWhiteSpace(RsaPrivateKeySecretName))
        {
            throw new InvalidOperationException("RsaPrivateKeySecretName cannot be null or empty.");
        }

        if (RsaPrivateKeySecretName.Length > MaxRsaPrivateKeySecretNameLength)
        {
            throw new InvalidOperationException($"RsaPrivateKeySecretName must not exceed {MaxRsaPrivateKeySecretNameLength} characters. Current length: {RsaPrivateKeySecretName.Length}");
        }
    }
}

