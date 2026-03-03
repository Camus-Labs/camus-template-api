using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Auth;

/// <summary>
/// Standard permission names used throughout the application for fine-grained authorization.
/// These constants define what actions users can perform based on their roles.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Permissions
{
    /// <summary>
    /// The claim type used for permission claims in JWT tokens.
    /// </summary>
    public const string ClaimType = "permission";

    // API Access Permissions
    
    /// <summary>
    /// Permission to read API resources and data.
    /// </summary>
    public const string ApiRead = "api.read";
    
    /// <summary>
    /// Permission to write or modify API resources and data.
    /// </summary>
    public const string ApiWrite = "api.write";
    
    /// <summary>
    /// Permission to create access tokens.
    /// </summary>
    public const string TokenCreate = "token.create";

    /// <summary>
    /// Gets all defined permissions.
    /// </summary>
    /// <returns>A list of all permission constants.</returns>
    public static List<string> GetAll()
    {
        return new List<string>
        {
            ApiRead,
            ApiWrite,
            TokenCreate
        };
    }
}
