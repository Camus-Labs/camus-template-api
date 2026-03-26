using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class AuthenticateUserCommandTests
{
    private const string ValidUsername = "testuser";
    private const string ValidPassword = "securepassword";

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var username = ValidUsername;
        var password = ValidPassword;

        // Act
        var command = new AuthenticateUserCommand(username, password);

        // Assert
        command.Username.Should().Be(username);
        command.Password.Should().Be(password);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        // Act
        var act = () => new AuthenticateUserCommand(username!, ValidPassword);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("username");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidPassword_ThrowsArgumentException(string? password)
    {
        // Arrange
        // Act
        var act = () => new AuthenticateUserCommand(ValidUsername, password!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("password");
    }
}
