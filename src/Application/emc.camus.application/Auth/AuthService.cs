using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using emc.camus.application.Common;
using emc.camus.application.Observability;
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
    private readonly ITokenRevocationCache _tokenRevocationCache;
    private readonly IUserContext _userContext;
    private readonly IActivitySourceWrapper _activitySource;
    private readonly IConnectionFactory? _connectionFactory;
    private readonly IGeneratedTokenRepository? _generatedTokenRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository for user credential validation.</param>
    /// <param name="tokenGenerator">Generator for creating authentication tokens.</param>
    /// <param name="auditRepository">Repository for logging audit events.</param>
    /// <param name="tokenRevocationCache">Cache for tracking revoked token JTIs.</param>
    /// <param name="userContext">Context for accessing current authenticated user information.</param>
    /// <param name="activitySource">Activity source for distributed tracing telemetry.</param>
    /// <param name="connectionFactory">Optional: Factory for creating database connections (for transactional operations).</param>
    /// <param name="generatedTokenRepository">Optional: Repository for managing generated tokens (requires connectionFactory).</param>
    public AuthService(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IActionAuditRepository auditRepository,
        ITokenRevocationCache tokenRevocationCache,
        IUserContext userContext,
        IActivitySourceWrapper activitySource,
        IConnectionFactory? connectionFactory = null,
        IGeneratedTokenRepository? generatedTokenRepository = null)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(tokenGenerator);
        ArgumentNullException.ThrowIfNull(auditRepository);
        ArgumentNullException.ThrowIfNull(tokenRevocationCache);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(activitySource);

        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _connectionFactory = connectionFactory;
        _auditRepository = auditRepository;
        _generatedTokenRepository = generatedTokenRepository;
        _tokenRevocationCache = tokenRevocationCache;
        _userContext = userContext;
        _activitySource = activitySource;
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

                    // Generate token with user's permissions
                    var claims = user.GetPermissions()
                        .Select(p => new Claim(Permissions.ClaimType, p))
                        .ToList();
                    token = _tokenGenerator.GenerateToken(user.Id, user.Username, claims);

                    // Log successful login audit
                    await _auditRepository.LogSystemActionAsync(
                        connection,
                        user.Id,
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

                // Generate token with user's permissions
                var claims = user.GetPermissions()
                    .Select(p => new Claim(Permissions.ClaimType, p))
                    .ToList();
                token = _tokenGenerator.GenerateToken(user.Id, user.Username, claims);
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
    /// /// Generates a custom token with specified permissions and expiration for an authenticated user.
    /// Validates and restricts permissions to those possessed by the current user.
    /// Stores the generated token metadata in the database for audit and tracking.
    /// </summary>
    /// <param name="command">The generate token command with suffix, expiration, and permissions.</param>
    /// <returns>Token generation result with token details and permissions.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails (invalid suffix, expiration, or permissions).</exception>
    /// <exception cref="InvalidOperationException">Thrown when user context is unavailable or token generation fails.</exception>
    public virtual async Task<GenerateTokenResult> GenerateTokenAsync(GenerateTokenCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Extract current user information from context
        var currentUserId = _userContext.GetCurrentUserId()
            ?? throw new InvalidOperationException("User ID is not available. Ensure the user is authenticated.");
        var currentUsername = _userContext.GetCurrentUsername()
            ?? throw new InvalidOperationException("Username is not available. Ensure the user is authenticated.");
        var currentUserPermissions = _userContext.GetCurrentPermissions();

        var activity = Activity.Current;

        try
        {
            _activitySource.SetExecutionTags(activity, new Dictionary<string, object?>
            {
                { "requestor_username", currentUsername },
                { "requestor_user_id", currentUserId },
                { "requestor_permissions", string.Join(",", currentUserPermissions) }
            });

            // Validate username suffix
            ValidateUsernameSuffix(command.UsernameSuffix);

            // Validate expiration date
            ValidateExpirationDate(command.ExpiresOn);

            // Validate permissions are subset of current user's permissions
            ValidatePermissions(command.Permissions, currentUserPermissions);

            // Construct token username with suffix
            var tokenUsername = $"{currentUsername}-{command.UsernameSuffix}";

            // Generate JTI for token tracking
            var jti = Guid.NewGuid();

            AuthToken token;

            // Build additional claims for the custom token
            var additionalClaims = command.Permissions
                .Select(p => new Claim(Permissions.ClaimType, p))
                .ToList();

            // Construct domain entity for the generated token
            var generatedToken = new GeneratedToken(
                jti,
                currentUserId,
                currentUsername,
                tokenUsername,
                command.Permissions,
                command.ExpiresOn);

            // Generate token with custom JTI and expiration
            token = _tokenGenerator.GenerateToken(
                currentUserId,
                tokenUsername,
                jti,
                command.ExpiresOn,
                additionalClaims);

            // If connection factory and token repository are available, use transactional approach
            if (_connectionFactory != null && _generatedTokenRepository != null)
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Store generated token (entity-centric: repo owns lifecycle defaults)
                    await _generatedTokenRepository.CreateAsync(connection, generatedToken);

                    // Log token generation audit
                    await _auditRepository.LogSystemActionAsync(
                        connection,
                        currentUserId,
                        currentUsername,
                        "token.generate.success",
                        $"Generated token for '{tokenUsername}' with permissions: {string.Join(", ", command.Permissions)}. Expires: {command.ExpiresOn:yyyy-MM-dd HH:mm:ss} UTC");

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

            // Return result
            return new GenerateTokenResult(
                token.Token,
                token.ExpiresOn,
                currentUserId,
                currentUsername,
                tokenUsername);
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            // Let validation exceptions bubble up
            throw;
        }
        catch (Exception ex)
        {
            // Wrap infrastructure failures with business context
            throw new InvalidOperationException(
                $"Token generation failed due to a system error. User: {_userContext.GetCurrentUsername()}", ex);
        }
    }

    /// <summary>
    /// Retrieves a paged list of generated tokens created by the currently authenticated user.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="filter">Optional filter criteria for the query.</param>
    /// <returns>A paged result of token summaries for the current user.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user context is unavailable or database operations fail.</exception>
    public virtual async Task<PagedResult<GeneratedTokenSummaryView>> GetGeneratedTokensAsync(PaginationParams pagination, GeneratedTokenFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        var currentUserId = _userContext.GetCurrentUserId()
            ?? throw new InvalidOperationException("User ID is not available. Ensure the user is authenticated.");

        try
        {
            if (_connectionFactory == null || _generatedTokenRepository == null)
            {
                throw new InvalidOperationException("Token retrieval requires a database connection and token repository.");
            }

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var pagedTokens = await _generatedTokenRepository.GetPagedByCreatorUserIdAsync(connection, currentUserId, pagination, filter);

            var items = pagedTokens.Items.Select(ToSummaryView).ToList();

            return new PagedResult<GeneratedTokenSummaryView>(items, pagedTokens.TotalCount, pagedTokens.Page, pagedTokens.PageSize);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to retrieve generated tokens due to a system error.", ex);
        }
    }

    /// <summary>
    /// Revokes a generated token by its JTI. Only the creator of the token can revoke it.
    /// Uses the entity-centric pattern: load → mutate → save.
    /// Updates the in-memory revocation cache so the token is immediately rejected.
    /// </summary>
    /// <param name="jti">The JWT ID of the token to revoke.</param>
    /// <exception cref="InvalidOperationException">Thrown when user context is unavailable or database operations fail.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the token is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not the creator of the token.</exception>
    public virtual async Task<GeneratedTokenSummaryView> RevokeTokenAsync(Guid jti)
    {
        var currentUserId = _userContext.GetCurrentUserId()
            ?? throw new InvalidOperationException("User ID is not available. Ensure the user is authenticated.");
        var currentUsername = _userContext.GetCurrentUsername()
            ?? throw new InvalidOperationException("Username is not available. Ensure the user is authenticated.");

        if (_connectionFactory == null || _generatedTokenRepository == null)
        {
            throw new InvalidOperationException("Token revocation requires a database connection and token repository.");
        }

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Load entity from persistence
                var generatedToken = await _generatedTokenRepository.GetByJtiAsync(connection, jti)
                    ?? throw new KeyNotFoundException($"Generated token with JTI '{jti}' not found.");

                // Verify the current user is the creator
                if (generatedToken.CreatorUserId != currentUserId)
                {
                    throw new UnauthorizedAccessException("You can only revoke tokens that you created.");
                }

                // Mutate domain entity (enforces invariant: can only revoke once)
                generatedToken.Revoke();

                // Save mutated entity
                await _generatedTokenRepository.SaveAsync(connection, generatedToken);

                // Update in-memory revocation cache
                _tokenRevocationCache.Revoke(jti, generatedToken.ExpiresOn);

                // Audit log
                await _auditRepository.LogSystemActionAsync(
                    connection,
                    currentUserId,
                    currentUsername,
                    "token.revoke.success",
                    $"Revoked token '{generatedToken.TokenUsername}' (JTI: {jti}).");

                transaction.Commit();

                return ToSummaryView(generatedToken);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex) when (ex is KeyNotFoundException or UnauthorizedAccessException or InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Token revocation failed due to a system error.", ex);
        }
    }

    /// <summary>
    /// Maps a GeneratedToken domain entity to a GeneratedTokenSummaryView.
    /// </summary>

    /// <summary>    /// Initializes the user repository to load users and roles.
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

    /// <summary>
    /// Validates the username suffix format.
    /// </summary>
    private static void ValidateUsernameSuffix(string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
        {
            throw new ArgumentException("Username suffix cannot be empty.", nameof(suffix));
        }

        if (suffix.Length > 20)
        {
            throw new ArgumentException("Username suffix cannot exceed 20 characters.", nameof(suffix));
        }

        // Allow only alphanumeric characters, dots, hyphens, and underscores
        var validPattern = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._-]+$");
        if (!validPattern.IsMatch(suffix))
        {
            throw new ArgumentException(
                "Username suffix can only contain alphanumeric characters, dots (.), hyphens (-), and underscores (_).",
                nameof(suffix));
        }
    }

    /// <summary>
    /// Validates the expiration date is within acceptable range.
    /// </summary>
    private static void ValidateExpirationDate(DateTime expiresOn)
    {
        var now = DateTime.UtcNow;
        var minExpiration = now.AddHours(1);
        var maxExpiration = now.AddYears(1);

        if (expiresOn < minExpiration)
        {
            throw new ArgumentException(
                $"Expiration date must be at least 1 hour from now (minimum: {minExpiration:yyyy-MM-dd HH:mm:ss} UTC).",
                nameof(expiresOn));
        }

        if (expiresOn > maxExpiration)
        {
            throw new ArgumentException(
                $"Expiration date cannot exceed 1 year from now (maximum: {maxExpiration:yyyy-MM-dd HH:mm:ss} UTC).",
                nameof(expiresOn));
        }
    }

    /// <summary>
    /// Validates that requested permissions are a subset of current user's permissions
    /// and are all valid system permissions.
    /// </summary>
    private static void ValidatePermissions(List<string> requestedPermissions, List<string> currentUserPermissions)
    {
        if (requestedPermissions == null || requestedPermissions.Count == 0)
        {
            throw new ArgumentException("At least one permission must be specified.", nameof(requestedPermissions));
        }

        // Validate all requested permissions are valid system permissions
        var validPermissions = Permissions.GetAll();
        var invalidPermissions = requestedPermissions.Where(p => !validPermissions.Contains(p)).ToList();

        if (invalidPermissions.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid permissions: {string.Join(", ", invalidPermissions)}. Valid permissions are: {string.Join(", ", validPermissions)}.",
                nameof(requestedPermissions));
        }

        // Validate all requested permissions are subset of current user's permissions
        var unauthorizedPermissions = requestedPermissions.Where(p => !currentUserPermissions.Contains(p)).ToList();

        if (unauthorizedPermissions.Count > 0)
        {
            throw new ArgumentException(
                $"Cannot grant permissions you don't have: {string.Join(", ", unauthorizedPermissions)}.",
                nameof(requestedPermissions));
        }
    }
    private static GeneratedTokenSummaryView ToSummaryView(GeneratedToken token)
    {
        return new GeneratedTokenSummaryView(
            token.Jti,
            token.TokenUsername,
            token.Permissions,
            token.ExpiresOn,
            token.CreatedAt,
            token.IsRevoked,
            token.RevokedAt,
            token.IsValid()
        );
    }
}
