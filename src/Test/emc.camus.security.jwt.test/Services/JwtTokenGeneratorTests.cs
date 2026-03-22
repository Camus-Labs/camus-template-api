using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Services;

namespace emc.camus.security.jwt.test.Services;

public class JwtTokenGeneratorTests
{
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ValidJti = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidUsername = "testuser";

    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenGeneratorTests()
    {
        _jwtSettings = new JwtSettings
        {
            Issuer = "https://test-issuer.com/",
            Audience = "https://test-audience.com/",
            ExpirationMinutes = 30
        };

        var rsa = RSA.Create(2048);
        var rsaKey = new RsaSecurityKey(rsa);
        _signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
    }

    private JwtTokenGenerator CreateGenerator() =>
        new JwtTokenGenerator(_jwtSettings, _signingCredentials);

    // --- Constructor ---

    [Fact]
    public void Constructor_NullJwtSettings_ThrowsArgumentNullException()
    {
        // Arrange
        JwtSettings nullSettings = null!;

        // Act
        var act = () => new JwtTokenGenerator(nullSettings, _signingCredentials);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("jwtSettings");
    }

    [Fact]
    public void Constructor_NullSigningCredentials_ThrowsArgumentNullException()
    {
        // Arrange
        SigningCredentials nullCredentials = null!;

        // Act
        var act = () => new JwtTokenGenerator(_jwtSettings, nullCredentials);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("signingCredentials");
    }

    // --- GenerateToken (default JTI overload) ---

    [Fact]
    public void GenerateToken_EmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateToken(Guid.Empty, ValidUsername);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("userId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_NullOrWhiteSpaceUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateToken(ValidUserId, username!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("username");
    }

    [Fact]
    public void GenerateToken_ValidInputs_ReturnsTokenWithExpectedProperties()
    {
        // Arrange
        var generator = CreateGenerator();
        var handler = new JwtSecurityTokenHandler();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername);
        var token = handler.ReadJwtToken(result.Token);

        // Assert
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresOn.Should().BeAfter(beforeGeneration);
        token.Issuer.Should().Be(_jwtSettings.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(_jwtSettings.Audience);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == ValidUserId.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_WithAdditionalClaims_TokenContainsAdditionalClaims()
    {
        // Arrange
        var generator = CreateGenerator();
        var handler = new JwtSecurityTokenHandler();
        var additionalClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("custom-claim", "custom-value")
        };

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername, additionalClaims);
        var token = handler.ReadJwtToken(result.Token);

        // Assert
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        token.Claims.Should().Contain(c => c.Type == "custom-claim" && c.Value == "custom-value");
    }

    [Fact]
    public void GenerateToken_WithNullAdditionalClaims_DoesNotThrow()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, null);

        // Assert
        act.Should().NotThrow();
    }

    // --- GenerateToken (explicit JTI overload) ---

    [Fact]
    public void GenerateToken_WithJti_EmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var generator = CreateGenerator();
        var expiresOn = DateTime.UtcNow.AddMinutes(30);

        // Act
        var act = () => generator.GenerateToken(Guid.Empty, ValidUsername, ValidJti, expiresOn);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("userId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_WithJti_NullOrWhiteSpaceUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var generator = CreateGenerator();
        var expiresOn = DateTime.UtcNow.AddMinutes(30);

        // Act
        var act = () => generator.GenerateToken(ValidUserId, username!, ValidJti, expiresOn);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("username");
    }

    [Fact]
    public void GenerateToken_WithJti_EmptyJti_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var generator = CreateGenerator();
        var expiresOn = DateTime.UtcNow.AddMinutes(30);

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, Guid.Empty, expiresOn);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("jti");
    }

    [Fact]
    public void GenerateToken_WithJti_DefaultExpiresOn_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, default);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("expiresOn");
    }

    [Fact]
    public void GenerateToken_WithJti_ValidInputs_ReturnsTokenWithExpectedProperties()
    {
        // Arrange
        var generator = CreateGenerator();
        var handler = new JwtSecurityTokenHandler();
        var expiresOn = DateTime.UtcNow.AddMinutes(30);

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, expiresOn);
        var token = handler.ReadJwtToken(result.Token);

        // Assert
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresOn.Should().BeCloseTo(expiresOn, TimeSpan.FromSeconds(1));
        token.Issuer.Should().Be(_jwtSettings.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(_jwtSettings.Audience);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti && c.Value == ValidJti.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == ValidUserId.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_WithJti_WithAdditionalClaims_TokenContainsAdditionalClaims()
    {
        // Arrange
        var generator = CreateGenerator();
        var handler = new JwtSecurityTokenHandler();
        var expiresOn = DateTime.UtcNow.AddMinutes(30);
        var additionalClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("custom-claim", "custom-value")
        };

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, expiresOn, additionalClaims);
        var token = handler.ReadJwtToken(result.Token);

        // Assert
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        token.Claims.Should().Contain(c => c.Type == "custom-claim" && c.Value == "custom-value");
    }

    [Fact]
    public void GenerateToken_WithJti_NullAdditionalClaims_DoesNotThrow()
    {
        // Arrange
        var generator = CreateGenerator();
        var expiresOn = DateTime.UtcNow.AddMinutes(30);

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, expiresOn, null);

        // Assert
        act.Should().NotThrow();
    }
}
