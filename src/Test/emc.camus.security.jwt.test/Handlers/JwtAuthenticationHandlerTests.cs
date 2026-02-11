using System.Security.Claims;
using emc.camus.application.Generic;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Handlers;
using emc.camus.security.jwt.Metrics;
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
    private readonly JwtMetrics _metrics;

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
        _metrics = new JwtMetrics("test-service");
    }

    [Fact]
    public void AddJwtBearerWithDefaults_ShouldRegisterJwtBearer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_jwtSettings);
        services.AddSingleton(_rsaKey);
        services.AddSingleton(_mockLoggerFactory.Object);
        services.AddSingleton(_metrics);
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
        services.AddSingleton(_metrics);
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
    public async Task OnAuthenticationFailed_TokenExpired_ShouldStoreException()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        var context = CreateAuthenticationFailedContext(new SecurityTokenExpiredException("Token expired"));

        // Act
        await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert - Verify exception is stored for OnChallenge to use
        context.HttpContext.Items.Should().ContainKey("AuthException");
        context.HttpContext.Items["AuthException"].Should().BeOfType<SecurityTokenExpiredException>();
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
            .WithMessage("*Unauthorized access*")
            .And.Data[ErrorCodes.ErrorCodeKey].Should().NotBeNull();
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
            .WithMessage("*Unauthorized access*")
            .And.Data[ErrorCodes.ErrorCodeKey].Should().NotBeNull();
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
            .WithMessage("*You do not have permission to access this resource*")
            .And.Data[ErrorCodes.ErrorCodeKey].Should().NotBeNull();
    }

    [Fact]
    public async Task OnTokenValidated_WithUnknownUser_ShouldNotThrow()
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

        // Act & Assert - OnTokenValidated should not throw, it's just for tracking
        var act = async () => await jwtOptions.Events.OnTokenValidated(context);
        await act.Should().NotThrowAsync();
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
            .WithMessage("*permission*")
            .And.Data[ErrorCodes.ErrorCodeKey].Should().NotBeNull();
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
            .WithMessage("*permission*")
            .And.Data[ErrorCodes.ErrorCodeKey].Should().NotBeNull();
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
            .WithMessage("*permission*")
            .And.Data[ErrorCodes.ErrorCodeKey].Should().NotBeNull();
    }

    private ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_jwtSettings);
        services.AddSingleton(_rsaKey);
        services.AddSingleton(_mockLoggerFactory.Object);
        services.AddSingleton(_metrics);
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
