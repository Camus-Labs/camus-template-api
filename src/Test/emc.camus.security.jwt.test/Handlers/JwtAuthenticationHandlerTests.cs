using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using emc.camus.application.Generic;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Handlers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace emc.camus.security.jwt.test.Handlers;

/// <summary>
/// Unit tests for JwtAuthenticationHandler to verify event handlers and authentication logic.
/// Tests focus on security-critical behavior: token validation, error handling, and logging.
/// </summary>
public class JwtAuthenticationHandlerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly JwtSettings _jwtSettings;
    private readonly RsaSecurityKey _rsaKey;
    private readonly SigningCredentials _signingCredentials;

    public JwtAuthenticationHandlerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _jwtSettings = new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 60
        };

        // Create test RSA key
        var rsa = System.Security.Cryptography.RSA.Create(2048);
        _rsaKey = new RsaSecurityKey(rsa);
        _signingCredentials = new SigningCredentials(_rsaKey, SecurityAlgorithms.RsaSha256);
    }

    [Fact]
    public void AddJwtBearerWithDefaults_ShouldRegisterJwtBearer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_jwtSettings);
        services.AddSingleton(_rsaKey);
        services.AddSingleton(_mockLoggerFactory.Object);
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder().Build();
        var authBuilder = services.AddAuthentication();

        // Act
        var result = authBuilder.AddJwtBearerWithDefaults(services, configuration);

        // Assert
        result.Should().NotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddJwtBearerWithDefaults_ShouldConfigureTokenValidationParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_jwtSettings);
        services.AddSingleton(_rsaKey);
        services.AddSingleton(_mockLoggerFactory.Object);
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder().Build();
        var authBuilder = services.AddAuthentication();

        // Act
        authBuilder.AddJwtBearerWithDefaults(services, configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidIssuer.Should().Be(_jwtSettings.Issuer);
        jwtOptions.TokenValidationParameters.ValidAudience.Should().Be(_jwtSettings.Audience);
        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().Be(_rsaKey);
        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task OnAuthenticationFailed_TokenExpired_ShouldSetCorrectErrorCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenExpiredException("Token expired"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        context.HttpContext.Items["AuthErrorCode"].Should().Be(ErrorCodes.Jwt.TokenExpired);
        context.HttpContext.Items["AuthErrorMessage"].Should().Be("JWT token has expired");
        context.HttpContext.Items["AuthException"].Should().BeOfType<SecurityTokenExpiredException>();
    }

    [Fact]
    public async Task OnAuthenticationFailed_TokenExpired_ShouldLogWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenExpiredException("Token expired"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT token expired")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthenticationFailed_InvalidSignature_ShouldSetCorrectErrorCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenInvalidSignatureException("Invalid signature"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        context.HttpContext.Items["AuthErrorCode"].Should().Be(ErrorCodes.Jwt.InvalidSignature);
        context.HttpContext.Items["AuthErrorMessage"].Should().Be("JWT token signature is invalid");
    }

    [Fact]
    public async Task OnAuthenticationFailed_InvalidIssuer_ShouldSetCorrectErrorCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenInvalidIssuerException("Invalid issuer"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        context.HttpContext.Items["AuthErrorCode"].Should().Be(ErrorCodes.Jwt.InvalidIssuer);
        context.HttpContext.Items["AuthErrorMessage"].Should().Be("JWT token issuer is invalid");
    }

    [Fact]
    public async Task OnAuthenticationFailed_InvalidAudience_ShouldSetCorrectErrorCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenInvalidAudienceException("Invalid audience"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        context.HttpContext.Items["AuthErrorCode"].Should().Be(ErrorCodes.Jwt.InvalidAudience);
        context.HttpContext.Items["AuthErrorMessage"].Should().Be("JWT token audience is invalid");
    }

    [Fact]
    public async Task OnAuthenticationFailed_GenericError_ShouldSetInvalidTokenErrorCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenException("Generic token error"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        context.HttpContext.Items["AuthErrorCode"].Should().Be(ErrorCodes.Jwt.InvalidToken);
        ((string)context.HttpContext.Items["AuthErrorMessage"]!).Should().Contain("JWT token validation failed");
    }

    [Fact]
    public void OnChallenge_WithStoredError_ShouldThrowUnauthorizedWithErrorCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["AuthErrorCode"] = ErrorCodes.Jwt.TokenExpired;
        httpContext.Items["AuthErrorMessage"] = "Token has expired";
        httpContext.Items["AuthException"] = new SecurityTokenExpiredException();

        var context = new JwtBearerChallengeContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions(),
            new AuthenticationProperties());

        // Act
        var act = () => jwtOptions.Events.OnChallenge(context).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Token has expired")
            .And.Data["ErrorCode"].Should().Be(ErrorCodes.Jwt.TokenExpired);
    }

    [Fact]
    public void OnChallenge_WithoutStoredError_ShouldThrowAuthenticationRequired()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";

        var context = new JwtBearerChallengeContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions(),
            new AuthenticationProperties());

        // Act
        var act = () => jwtOptions.Events.OnChallenge(context).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("JWT authentication is required to access this resource")
            .And.Data["ErrorCode"].Should().Be(ErrorCodes.AuthenticationRequired);
    }

    [Fact]
    public void OnChallenge_WithoutStoredError_ShouldLogWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";

        var context = new JwtBearerChallengeContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions(),
            new AuthenticationProperties());

        // Act
        try { jwtOptions.Events.OnChallenge(context); } catch { }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT authentication required")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnForbidden_ShouldThrowInvalidOperationWithForbiddenCode()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "Bearer"));

        var context = new ForbiddenContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        var act = () => jwtOptions.Events.OnForbidden(context).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not have permission*")
            .And.Data["ErrorCode"].Should().Be(ErrorCodes.Forbidden);
    }

    [Fact]
    public void OnForbidden_ShouldLogWarningWithUserName()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/admin";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "Bearer"));

        var context = new ForbiddenContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        try { jwtOptions.Events.OnForbidden(context); } catch { }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT authorization forbidden") && v.ToString()!.Contains("testuser")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnTokenValidated_ShouldLogInformationWithUserName()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "Bearer"));

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        await jwtOptions.Events.OnTokenValidated(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT token validated") && v.ToString()!.Contains("testuser")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnTokenValidated_WithUnknownUser_ShouldLogUnknown()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        await jwtOptions.Events.OnTokenValidated(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthenticationFailed_InvalidSignature_ShouldLogWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenInvalidSignatureException("Invalid signature"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT token signature validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthenticationFailed_InvalidIssuer_ShouldLogWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenInvalidIssuerException("Invalid issuer"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT token issuer validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthenticationFailed_InvalidAudience_ShouldLogWarning()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenInvalidAudienceException("Invalid audience"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT token audience validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthenticationFailed_GenericError_ShouldLogWarningWithException()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenException("Generic token error"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT authentication failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnForbidden_WithUnknownUser_ShouldThrowInvalidOperationWithUnknown()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var context = new ForbiddenContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        var act = () => jwtOptions.Events.OnForbidden(context).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*")
            .And.Data["ErrorCode"].Should().Be(ErrorCodes.Forbidden);
    }

    [Fact]
    public void OnForbidden_WithNullPrincipal_ShouldThrowInvalidOperationWithUnknown()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();

        var context = new ForbiddenContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = null
        };

        // Act
        var act = () => jwtOptions.Events.OnForbidden(context).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*")
            .And.Data["ErrorCode"].Should().Be(ErrorCodes.Forbidden);
    }

    [Fact]
    public void OnForbidden_WithNullIdentity_ShouldThrowInvalidOperationWithUnknown()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        // ClaimsPrincipal with no identities - Identity property will be null
        var principal = new ClaimsPrincipal();

        var context = new ForbiddenContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        var act = () => jwtOptions.Events.OnForbidden(context).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown*")
            .And.Data["ErrorCode"].Should().Be(ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task OnTokenValidated_WithNullPrincipal_ShouldLogUnknown()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = null
        };

        // Act
        await jwtOptions.Events.OnTokenValidated(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnTokenValidated_WithNullIdentity_ShouldLogUnknown()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext();
        // ClaimsPrincipal with no identities - Identity property will be null
        var principal = new ClaimsPrincipal();

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = principal
        };

        // Act
        await jwtOptions.Events.OnTokenValidated(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_jwtSettings);
        services.AddSingleton(_rsaKey);
        services.AddSingleton(_mockLoggerFactory.Object);
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder().Build();
        var authBuilder = services.AddAuthentication();
        authBuilder.AddJwtBearerWithDefaults(services, configuration);

        return services;
    }

    private AuthenticationFailedContext CreateAuthenticationFailedContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";

        return new AuthenticationFailedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Exception = exception
        };
    }
}
