using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using emc.camus.application.Secrets;

namespace emc.camus.security.components.Handlers
{
    /// <summary>
    /// Authentication handler for validating API Key requests using a secret provider.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string ApiKeyHeaderName = "X-Api-Key";
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">The options monitor for authentication scheme options.</param>
        /// <param name="logger">The logger factory.</param>
        /// <param name="encoder">The URL encoder.</param>
        /// <param name="secretProvider">The secret provider for retrieving API keys.</param>
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISecretProvider secretProvider)
            : base(options, logger, encoder)
        {
            _secretProvider = secretProvider;
        }

        /// <summary>
        /// Handles authentication for API Key requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticateResult"/> indicating success or failure.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when API Key is missing or invalid.</exception>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            {
                Logger.LogWarning("API Key authentication failed: Header '{HeaderName}' not found", ApiKeyHeaderName);
                throw new UnauthorizedAccessException("API Key header not found.");
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            var configuredApiKey = _secretProvider.GetSecret("XApiKey");

            if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != configuredApiKey)
            {
                Logger.LogWarning("API Key authentication failed: Invalid API Key provided");
                throw new UnauthorizedAccessException("Invalid API Key.");
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
            var identity = new ClaimsIdentity(claims, CamusAuthenticationSchemes.ApiKey);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, CamusAuthenticationSchemes.ApiKey);

            Logger.LogInformation("API Key authentication successful for user: {User}", "ApiKeyUser");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
