using System.Data;
using System.Security.Claims;
using emc.camus.application.Common;
using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Provides authentication services including credential validation and token generation.
/// Validates credentials via user repository and generates tokens for authenticated users.
/// Manages database transactions and audit logging for authentication operations when available.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IActionAuditRepository _auditRepository;
    private readonly IConnectionFactory? _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository for user credential validation.</param>
    /// <param name="tokenGenerator">Generator for creating authentication tokens.</param>
    /// <param name="connectionFactory">Optional: Factory for creating database connections (for transactional operations).</param>
    /// <param name="auditRepository">Optional: Repository for logging audit events.</param>
    public AuthService(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IActionAuditRepository auditRepository,
        IConnectionFactory? connectionFactory = null)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(tokenGenerator);
        ArgumentNullException.ThrowIfNull(auditRepository);

        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _connectionFactory = connectionFactory;
        _auditRepository = auditRepository;
    }

    /// <summary>
    /// Authenticates user credentials and generates a token.
    /// When IConnectionFactory is available, manages transaction and audit logging.
    /// Otherwise, performs simple credential validation.
    /// </summary>
    /// <param name="command">The authentication command containing username and password.</param>
    /// <returns>Authentication result with token, expiration, and user information.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when token generation or database operations fail.</exception>
    public virtual async Task<AuthenticateUserResult> AuthenticateAsync(AuthenticateUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            User user;
            AuthToken token;

            // If connection factory is available, use transactional approach
            if (_connectionFactory != null)
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Validate credentials via repository (UnauthorizedAccessException bubbles up)
                    user = await _userRepository.ValidateCredentialsAsync(connection, command.Username, command.Password);

                    // Update last login timestamp
                    await _userRepository.UpdateLastLoginAsync(connection, user.Id);

                    // Generate token with user's roles
                    var roleClaims = user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)).ToList();
                    token = _tokenGenerator.GenerateToken(user.Id, user.Username, roleClaims);

                    // Log successful login audit
                    await _auditRepository.LogSystemActionAsync(
                        connection,
                        Guid.Parse(user.Id),
                        user.Username,
                        "user.login.success",
                        $"Successful login. Token expires: {token.ExpiresOn:yyyy-MM-dd HH:mm:ss} UTC");

                    // Commit transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    // Rollback on infrastructure failures
                    transaction.Rollback();
                    throw;
                }
            }
            else
            {
                // Simple validation without transaction (e.g., InMemory provider)
                user = await _userRepository.ValidateCredentialsAsync(null!, command.Username, command.Password);

                // Generate token with user's roles
                var roleClaims = user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)).ToList();
                token = _tokenGenerator.GenerateToken(user.Id, user.Username, roleClaims);
            }

            // Return immutable result
            return new AuthenticateUserResult(
                token.Token,
                token.ExpiresOn
            );
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or KeyNotFoundException)
        {
            // Let domain exceptions bubble up with their original context
            throw;
        }
        catch (Exception ex)
        {
            // Wrap infrastructure failures with business context
            throw new InvalidOperationException(
                $"Authentication failed due to a system error. Username: {command.Username}", ex);
        }
    }

    /// <summary>
    /// Initializes the user repository to load users and roles.
    /// Should be called during application startup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public virtual void Initialize()
    {
        try
        {
            _userRepository.Initialize();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize authentication service. Ensure the database is accessible.", ex);
        }
    }
}
