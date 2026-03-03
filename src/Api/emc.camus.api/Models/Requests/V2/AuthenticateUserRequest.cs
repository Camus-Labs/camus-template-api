namespace emc.camus.api.Models.Requests.V2;

/// <summary>
/// Request model for user authentication.
/// </summary>
public class AuthenticateUserRequest
{
    /// <summary>
    /// The username for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
