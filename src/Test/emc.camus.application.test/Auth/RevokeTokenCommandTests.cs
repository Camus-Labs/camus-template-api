using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class RevokeTokenCommandTests
{
    private static readonly Guid ValidJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidJti_SetsProperty()
    {
        // Arrange
        var jti = ValidJti;

        // Act
        var command = new RevokeTokenCommand(jti);

        // Assert
        command.Jti.Should().Be(jti);
    }

    [Fact]
    public void Constructor_EmptyJti_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyJti = Guid.Empty;

        // Act
        var act = () => new RevokeTokenCommand(emptyJti);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("jti");
    }
}
