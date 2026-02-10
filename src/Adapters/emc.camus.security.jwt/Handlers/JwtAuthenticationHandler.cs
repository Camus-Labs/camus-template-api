using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.security.jwt.Configurations;
using emc.camus.application.Generic;
using System.Diagnostics.CodeAnalysis;

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
                .Configure<JwtSettings, RsaSecurityKey, ILoggerFactory>((options, jwtSettings, rsaKey, loggerFactory) =>
                {
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
                            var (errorCode, message) = GetAuthenticationError(context.Exception);
                            LogAuthenticationFailure(logger, context.Exception, errorCode, context.Request.Path);
                            // Store error details in HttpContext.Items for OnChallenge to use
                            StoreAuthenticationError(context.HttpContext, errorCode, message, context.Exception);
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            // Skip default challenge response - we'll throw to use our error handler
                            context.HandleResponse();
                            // Retrieve error details stored by OnAuthenticationFailed, if not present is because authentication was not provided at all
                            var (errorCode, errorMessage, originalException) = RetrieveAuthenticationError(context.HttpContext);
                            
                            if (errorCode == ErrorCodes.AuthenticationRequired)
                            {
                                logger.LogWarning("JWT authentication required for request to {Path}", context.Request.Path);
                            }
                            
                            throw CreateUnauthorizedException(errorCode, errorMessage, originalException);
                        },
                        OnForbidden = context =>
                        {
                            var userName = context.Principal?.Identity?.Name ?? "Unknown";
                            logger.LogWarning("JWT authorization forbidden for user {User} accessing {Path}", userName, context.Request.Path);
                            throw CreateForbiddenException(userName);
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

        private static (string ErrorCode, string Message) GetAuthenticationError(Exception exception)
        {
            return exception switch
            {
                SecurityTokenExpiredException => (ErrorCodes.Jwt.TokenExpired, "JWT token has expired"),
                SecurityTokenInvalidSignatureException => (ErrorCodes.Jwt.InvalidSignature, "JWT token signature is invalid"),
                SecurityTokenInvalidIssuerException => (ErrorCodes.Jwt.InvalidIssuer, "JWT token issuer is invalid"),
                SecurityTokenInvalidAudienceException => (ErrorCodes.Jwt.InvalidAudience, "JWT token audience is invalid"),
                _ => (ErrorCodes.Jwt.InvalidToken, $"JWT token validation failed: {exception.Message}")
            };
        }

        private static void LogAuthenticationFailure(ILogger logger, Exception exception, string errorCode, string path)
        {
            var logMessage = errorCode switch
            {
                var code when code == ErrorCodes.Jwt.TokenExpired => "JWT token expired for request to {Path}",
                var code when code == ErrorCodes.Jwt.InvalidSignature => "JWT token signature validation failed for request to {Path}",
                var code when code == ErrorCodes.Jwt.InvalidIssuer => "JWT token issuer validation failed for request to {Path}",
                var code when code == ErrorCodes.Jwt.InvalidAudience => "JWT token audience validation failed for request to {Path}",
                _ => "JWT authentication failed for request to {Path}"
            };

            if (errorCode == ErrorCodes.Jwt.InvalidToken)
            {
                logger.LogWarning(exception, logMessage, path);
            }
            else
            {
                logger.LogWarning(logMessage, path);
            }
        }

        private static void StoreAuthenticationError(Microsoft.AspNetCore.Http.HttpContext httpContext, string errorCode, string message, Exception exception)
        {
            httpContext.Items["AuthErrorCode"] = errorCode;
            httpContext.Items["AuthErrorMessage"] = message;
            httpContext.Items["AuthException"] = exception;
        }

        private static (string ErrorCode, string ErrorMessage, Exception? OriginalException) RetrieveAuthenticationError(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var errorCode = httpContext.Items["AuthErrorCode"] as string ?? ErrorCodes.AuthenticationRequired;
            var errorMessage = httpContext.Items["AuthErrorMessage"] as string 
                ?? "JWT authentication is required to access this resource";
            var originalException = httpContext.Items["AuthException"] as Exception;
            
            return (errorCode, errorMessage, originalException);
        }

        private static UnauthorizedAccessException CreateUnauthorizedException(string errorCode, string errorMessage, Exception? originalException)
        {
            var unauthorizedException = new UnauthorizedAccessException(errorMessage, originalException);
            unauthorizedException.Data["ErrorCode"] = errorCode;
            return unauthorizedException;
        }

        private static InvalidOperationException CreateForbiddenException(string userName)
        {
            var forbiddenException = new InvalidOperationException($"User {userName} does not have permission to access this resource");
            forbiddenException.Data["ErrorCode"] = ErrorCodes.Forbidden;
            forbiddenException.Data["UserName"] = userName;
            return forbiddenException;
        }
    }
}
