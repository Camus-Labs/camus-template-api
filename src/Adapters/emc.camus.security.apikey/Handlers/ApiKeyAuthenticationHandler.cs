using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using emc.camus.application.Secrets;
using emc.camus.application.Auth;
using emc.camus.application.Generic;
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
        /// <exception cref="UnauthorizedAccessException">Thrown when API Key is missing or invalid.</exception>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Headers.ApiKey, out var apiKeyHeaderValues))
            {
                Logger.LogWarning("API Key authentication failed: Header '{HeaderName}' not found", Headers.ApiKey);
                var missingHeaderException = new UnauthorizedAccessException("API Key header not found.");
                missingHeaderException.Data["ErrorCode"] = ErrorCodes.AuthenticationRequired;
                throw missingHeaderException;
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            var configuredApiKey = _secretProvider.GetSecret(_settings.SecretKeyName);

            if (string.IsNullOrWhiteSpace(providedApiKey) || providedApiKey != configuredApiKey)
            {
                Logger.LogWarning("API Key authentication failed: Invalid API Key provided");
                var invalidKeyException = new UnauthorizedAccessException("Invalid API Key.");
                invalidKeyException.Data["ErrorCode"] = ErrorCodes.InvalidCredentials;
                throw invalidKeyException;
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
            var identity = new ClaimsIdentity(claims, AuthenticationSchemes.ApiKey);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationSchemes.ApiKey);

            Logger.LogInformation("API Key authentication successful for user: {User}", "ApiKeyUser");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
