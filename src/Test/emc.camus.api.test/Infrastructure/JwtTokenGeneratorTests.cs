using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;
using emc.camus.api.Configurations;
using emc.camus.api.Infrastructure;

namespace emc.camus.api.test.Infrastructure;

public class JwtTokenGeneratorTests
{
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ValidJti = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset FixedUtcNow = new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiresOn = FixedUtcNow.AddYears(1).UtcDateTime;
    private static readonly RSA SharedRsa = RSA.Create(2048);
    private static readonly SigningCredentials SharedSigningCredentials = new(
        new RsaSecurityKey(SharedRsa), SecurityAlgorithms.RsaSha256);
    private const string ValidUsername = "testuser";
    private const string TestRoleValue = "Admin";
    private const string CustomClaimType = "custom-claim";
    private const string CustomClaimValue = "custom-value";
    private static readonly JwtSecurityTokenHandler TokenHandler = new();
    private static readonly Claim[] AdditionalClaims =
    [
        new(ClaimTypes.Role, TestRoleValue),
        new(CustomClaimType, CustomClaimValue)
    ];

    private readonly JwtSettings _jwtSettings;
    private readonly FakeTimeProvider _timeProvider;

    public JwtTokenGeneratorTests()
    {
        _jwtSettings = new JwtSettings
        {
            Issuer = "https://test-issuer.com/",
            Audience = "https://test-audience.com/",
            ExpirationMinutes = 60
        };

        _timeProvider = new FakeTimeProvider(FixedUtcNow);
    }

    private JwtTokenGenerator CreateGenerator() =>
        new JwtTokenGenerator(_jwtSettings, SharedSigningCredentials, _timeProvider);

    // --- Constructor ---

    [Fact]
    public void Constructor_NullJwtSettings_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _ = new JwtTokenGenerator(null!, SharedSigningCredentials, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("jwtSettings");
    }

    [Fact]
    public void Constructor_NullSigningCredentials_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _ = new JwtTokenGenerator(_jwtSettings, null!, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("signingCredentials");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _ = new JwtTokenGenerator(_jwtSettings, SharedSigningCredentials, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("timeProvider");
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
        var expectedExpiresOn = _timeProvider.GetUtcNow().DateTime.AddMinutes(_jwtSettings.ExpirationMinutes);

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername);
        var token = TokenHandler.ReadJwtToken(result.Token);

        // Assert
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresOn.Should().Be(expectedExpiresOn);
        token.Issuer.Should().Be(_jwtSettings.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(_jwtSettings.Audience);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == ValidUserId.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_WithAdditionalClaims_TokenContainsAdditionalClaims()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername, AdditionalClaims);
        var token = TokenHandler.ReadJwtToken(result.Token);

        // Assert
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == TestRoleValue);
        token.Claims.Should().Contain(c => c.Type == CustomClaimType && c.Value == CustomClaimValue);
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

        // Act
        var act = () => generator.GenerateToken(Guid.Empty, ValidUsername, ValidJti, ValidExpiresOn);

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

        // Act
        var act = () => generator.GenerateToken(ValidUserId, username!, ValidJti, ValidExpiresOn);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("username");
    }

    [Fact]
    public void GenerateToken_WithJti_EmptyJti_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, Guid.Empty, ValidExpiresOn);

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

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, ValidExpiresOn);
        var token = TokenHandler.ReadJwtToken(result.Token);

        // Assert
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresOn.Should().Be(ValidExpiresOn);
        token.Issuer.Should().Be(_jwtSettings.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(_jwtSettings.Audience);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti && c.Value == ValidJti.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == ValidUserId.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == ValidUsername);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_WithJti_WithAdditionalClaims_TokenContainsAdditionalClaims()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var result = generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, ValidExpiresOn, AdditionalClaims);
        var token = TokenHandler.ReadJwtToken(result.Token);

        // Assert
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == TestRoleValue);
        token.Claims.Should().Contain(c => c.Type == CustomClaimType && c.Value == CustomClaimValue);
    }

    [Fact]
    public void GenerateToken_WithJti_NullAdditionalClaims_DoesNotThrow()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, ValidExpiresOn, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_SigningFailure_ThrowsJwtTokenGenerationException()
    {
        // Arrange — symmetric key paired with RSA algorithm causes signing failure
        var shortKey = new SymmetricSecurityKey(new byte[16]);
        var invalidCredentials = new SigningCredentials(shortKey, SecurityAlgorithms.RsaSha256);

        var generator = new JwtTokenGenerator(_jwtSettings, invalidCredentials, _timeProvider);

        // Act
        var act = () => generator.GenerateToken(ValidUserId, ValidUsername, ValidJti, ValidExpiresOn);

        // Assert
        act.Should().Throw<JwtTokenGenerationException>()
            .WithMessage("Failed to generate JWT token.")
            .Which.InnerException.Should().NotBeNull();
    }
}
