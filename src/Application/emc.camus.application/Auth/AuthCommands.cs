using System.Security.Claims;

namespace emc.camus.application.Auth;

/// <summary>
/// Command to authenticate a user with username and password.
/// </summary>
/// <param name="Username">The username for authentication.</param>
/// <param name="Password">The password for authentication.</param>
public record AuthenticateUserCommand(string Username, string Password);