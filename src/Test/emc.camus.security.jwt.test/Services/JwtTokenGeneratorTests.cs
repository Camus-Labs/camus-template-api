using System.Security.Claims;
using emc.camus.application.Auth;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace adapter.security.jwt.test.Services;

/// <summary>
/// Unit tests for JwtTokenGenerator to verify token generation logic.
/// </summary>
public class JwtTokenGeneratorTests
{
    private readonly Mock<ILogger<JwtTokenGenerator>> _mockLogger;
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenGeneratorTests()
    {
        _mockLogger = new Mock<ILogger<JwtTokenGenerator>>();
        _jwtSettings = new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 60
        };

        // Create test signing credentials
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-secret-key-that-is-long-enough-for-hmac-256"));
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GenerateToken_WithValidSubject_ShouldReturnToken()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act
        var result = generator.GenerateToken(subject);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresOn.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_WithValidSubject_ShouldSetCorrectExpiration()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        // Act
        var result = generator.GenerateToken(subject);

        // Assert
        result.ExpiresOn.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_ShouldIncludeStandardClaims()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act
        var result = generator.GenerateToken(subject);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == subject);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == subject);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectIssuerAndAudience()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act
        var result = generator.GenerateToken(subject);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        
        token.Issuer.Should().Be(_jwtSettings.Issuer);
        token.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Fact]
    public void GenerateToken_WithAdditionalClaims_ShouldIncludeThem()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";
        var additionalClaims = new[]
        {
            new Claim("role", "admin"),
            new Claim("department", "IT")
        };

        // Act
        var result = generator.GenerateToken(subject, additionalClaims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        
        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
        token.Claims.Should().Contain(c => c.Type == "department" && c.Value == "IT");
    }

    [Fact]
    public void GenerateToken_WithNullAdditionalClaims_ShouldNotThrow()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act & Assert
        var act = () => generator.GenerateToken(subject, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_WithEmptyAdditionalClaims_ShouldNotThrow()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act & Assert
        var act = () => generator.GenerateToken(subject, Array.Empty<Claim>());
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_MultipleCallsWithSameSubject_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act
        var result1 = generator.GenerateToken(subject);
        var result2 = generator.GenerateToken(subject);

        // Assert
        result1.Token.Should().NotBe(result2.Token);
    }

    [Fact]
    public void GenerateToken_ShouldGenerateUniqueJti()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act
        var result1 = generator.GenerateToken(subject);
        var result2 = generator.GenerateToken(subject);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token1 = handler.ReadJwtToken(result1.Token);
        var token2 = handler.ReadJwtToken(result2.Token);
        
        var jti1 = token1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = token2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        
        jti1.Should().NotBe(jti2);
    }

    [Theory]
    [InlineData("user1")]
    [InlineData("admin@example.com")]
    [InlineData("test-user-123")]
    public void GenerateToken_WithVariousSubjects_ShouldGenerateValidTokens(string subject)
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var result = generator.GenerateToken(subject);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == subject);
    }

    [Fact]
    public void GenerateToken_ReturnedToken_ShouldBeValidJwt()
    {
        // Arrange
        var generator = CreateGenerator();
        var subject = "testuser";

        // Act
        var result = generator.GenerateToken(subject);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(result.Token).Should().BeTrue();
    }

    private JwtTokenGenerator CreateGenerator()
    {
        return new JwtTokenGenerator(
            _jwtSettings,
            _signingCredentials,
            _mockLogger.Object);
    }
}
