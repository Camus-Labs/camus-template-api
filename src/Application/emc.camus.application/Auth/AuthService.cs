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
    private readonly IConnectionFactory? _connectionFactory;
    private readonly IActionAuditRepository? _auditRepository;

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
        IConnectionFactory? connectionFactory = null,
        IActionAuditRepository? auditRepository = null)
    {
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
    public virtual async Task<AuthenticateUserResult> AuthenticateAsync(AuthenticateUserCommand command)
    {
        User user;
        AuthToken token;

        // If connection factory and audit repository are available, use transactional approach with audit logging
        // (Both are registered together with database persistence)
        if (_connectionFactory != null && _auditRepository != null)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Validate credentials via repository
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
            catch
            {
                // Rollback transaction on any other failure
                transaction.Rollback();
                throw;
            }
        }
        else
        {
            // Simple validation without transaction (e.g., InMemory provider)
            user = await _userRepository.ValidateCredentialsAsync(command.Username, command.Password);
            
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

    /// <summary>
    /// Initializes the user repository to load users and roles.
    /// Should be called during application startup.
    /// </summary>
    public virtual void Initialize()
    {
        _userRepository.Initialize();
    }
}
