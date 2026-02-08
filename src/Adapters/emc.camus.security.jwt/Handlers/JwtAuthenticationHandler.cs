using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.security.jwt.Configurations;

namespace emc.camus.security.jwt.Handlers
{
    /// <summary>
    /// Provides extension methods for configuring JWT Bearer authentication.
    /// </summary>
    public static class JwtAuthenticationHandler
    {
        /// <summary>
        /// Adds JWT Bearer authentication with default configuration including event handlers for logging.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="services">The service collection for dependency resolution.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated authentication builder for fluent configuration.</returns>
        public static AuthenticationBuilder AddJwtBearerWithDefaults(
            this AuthenticationBuilder builder,
            IServiceCollection services,
            IConfiguration configuration)
        {
            builder.AddJwtBearer(options =>
            {
                // Initial configuration - will be overridden by AddOptions below
            });

            // Configure JWT Bearer Options with dependency injection
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>, RsaSecurityKey, ILoggerFactory>((options, jwtSettingsOptions, rsaKey, loggerFactory) =>
                {
                    var jwtSettings = jwtSettingsOptions.Value;
                    var logger = loggerFactory.CreateLogger("JwtAuthenticationHandler");
                    
                    // Token Validation Parameters
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = rsaKey,
                        ClockSkew = TimeSpan.Zero
                    };

                    // JWT Bearer Events for logging and error handling
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var exception = context.Exception;
                            
                            // Detect specific token validation failures for better client error handling
                            string errorCode;
                            string message;

                            if (exception is SecurityTokenExpiredException)
                            {
                                errorCode = "token_expired";
                                message = "JWT token has expired";
                                logger.LogWarning("JWT token expired for request to {Path}", context.Request.Path);
                            }
                            else if (exception is SecurityTokenInvalidSignatureException)
                            {
                                errorCode = "invalid_signature";
                                message = "JWT token signature is invalid";
                                logger.LogWarning("JWT token signature validation failed for request to {Path}", context.Request.Path);
                            }
                            else if (exception is SecurityTokenInvalidIssuerException)
                            {
                                errorCode = "invalid_issuer";
                                message = "JWT token issuer is invalid";
                                logger.LogWarning("JWT token issuer validation failed for request to {Path}", context.Request.Path);
                            }
                            else if (exception is SecurityTokenInvalidAudienceException)
                            {
                                errorCode = "invalid_audience";
                                message = "JWT token audience is invalid";
                                logger.LogWarning("JWT token audience validation failed for request to {Path}", context.Request.Path);
                            }
                            else
                            {
                                errorCode = "invalid_token";
                                message = $"JWT token validation failed: {exception.Message}";
                                logger.LogWarning(exception, "JWT authentication failed for request to {Path}", context.Request.Path);
                            }

                            // Store error details in HttpContext.Items for OnChallenge to use
                            context.HttpContext.Items["AuthErrorCode"] = errorCode;
                            context.HttpContext.Items["AuthErrorMessage"] = message;
                            context.HttpContext.Items["AuthException"] = exception;
                            
                            // Don't throw here - let OnChallenge handle it to avoid double-invocation
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            // Skip default challenge response - we'll throw to use our error handler
                            context.HandleResponse();
                            
                            // Retrieve error details stored by OnAuthenticationFailed, if not present is because authentication was not provided at all
                            var errorCode = context.HttpContext.Items["AuthErrorCode"] as string ?? "authentication_required";
                            var errorMessage = context.HttpContext.Items["AuthErrorMessage"] as string 
                                ?? "JWT authentication is required to access this resource";
                            var originalException = context.HttpContext.Items["AuthException"] as Exception;
                            
                            // Log only if no token was provided (not already logged in OnAuthenticationFailed)
                            if (errorCode == "authentication_required")
                            {
                                logger.LogWarning("JWT authentication required for request to {Path}", context.Request.Path);
                            }
                            
                            // Create exception with error code for error handler
                            var unauthorizedException = new UnauthorizedAccessException(errorMessage, originalException);
                            unauthorizedException.Data["ErrorCode"] = errorCode;
                            throw unauthorizedException;
                        },
                        OnForbidden = context =>
                        {
                            // Called when authentication succeeded but authorization failed (insufficient permissions)
                            var userName = context.Principal?.Identity?.Name ?? "Unknown";
                            logger.LogWarning("JWT authorization forbidden for user {User} accessing {Path}", userName, context.Request.Path);
                            
                            var forbiddenException = new InvalidOperationException($"User {userName} does not have permission to access this resource");
                            forbiddenException.Data["ErrorCode"] = "forbidden";
                            forbiddenException.Data["UserName"] = userName;
                            throw forbiddenException;
                        },
                        OnTokenValidated = context =>
                        {
                            logger.LogInformation("JWT token validated for user: {User}", 
                                context.Principal?.Identity?.Name ?? "Unknown");
                            return Task.CompletedTask;
                        }
                    };
                });

            return builder;
        }
    }
}
