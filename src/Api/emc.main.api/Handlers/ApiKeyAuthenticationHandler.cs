using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using emc.camus.application.Secrets;
    
namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// Authentication handler for validating API Key requests using a secret provider.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>
        /// The authentication scheme name for API Key authentication.
        /// </summary>
        public const string SchemeName = "ApiKey";
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
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key header not found."));
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            var configuredApiKey = _secretProvider.GetSecret("XApiKey");

            if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != configuredApiKey)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}