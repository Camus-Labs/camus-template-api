using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using emc.camus.application.Secrets;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.security.apikey.Configurations;

namespace emc.camus.security.apikey.Handlers
{
    /// <summary>
    /// Authentication handler for validating API Key requests using a secret provider.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ISecretProvider _secretProvider;
        private readonly ApiKeySettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">The options monitor for authentication scheme options.</param>
        /// <param name="logger">The logger factory.</param>
        /// <param name="encoder">The URL encoder.</param>
        /// <param name="secretProvider">The secret provider for retrieving API keys.</param>
        /// <param name="settings">The API Key settings from configuration.</param>
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISecretProvider secretProvider,
            ApiKeySettings settings)
            : base(options, logger, encoder)
        {
            _secretProvider = secretProvider;
            _settings = settings;
        }

        /// <summary>
        /// Handles authentication for API Key requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticateResult"/> indicating success or failure.</returns>
        /// <remarks>
        /// Throws <see cref="UnauthorizedAccessException"/> on authentication failures which are caught
        /// by the authentication middleware and converted to 401 responses with appropriate error codes.
        /// Error codes are determined by ErrorCodeResolver based on exception type and message.
        /// </remarks>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Headers.ApiKey, out var apiKeyHeaderValues))
            {
                throw new UnauthorizedAccessException("Authentication is required to access this resource. API Key header is missing.");
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            var configuredApiKey = _secretProvider.GetSecret(_settings.ApiKeySecretName);

            if (string.IsNullOrWhiteSpace(providedApiKey) || providedApiKey != configuredApiKey)
            {
                throw new UnauthorizedAccessException("The provided credentials are invalid. API Key does not match the configured value.");
            }

            var claims = new[] { new Claim(ClaimTypes.Name, ApiKeySettings.DefaultUsername) };
            var identity = new ClaimsIdentity(claims, AuthenticationSchemes.ApiKey);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationSchemes.ApiKey);
            
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
