using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Auth;

/// <summary>
/// Defines authentication scheme names for the application.
/// </summary>
[ExcludeFromCodeCoverage]
public static class AuthenticationSchemes
{
    /// <summary>
    /// JWT Bearer authentication scheme name.
    /// </summary>
    public const string JwtBearer = "Bearer";

    /// <summary>
    /// API Key authentication scheme name.
    /// </summary>
    public const string ApiKey = "ApiKey";

    /// <summary>
    /// Gets all valid authentication scheme names.
    /// </summary>
    /// <returns>Array of all valid authentication scheme names.</returns>
    public static string[] GetAll()
    {
        return new[] { JwtBearer, ApiKey };
    }
}
