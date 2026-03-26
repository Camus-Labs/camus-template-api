using FluentAssertions;
using emc.camus.application.Configurations;

namespace emc.camus.application.test.Configurations;

public class DataPersistenceSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_DefaultProvider_DoesNotThrow()
    {
        // Arrange
        var settings = new DataPersistenceSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(PersistenceProvider.InMemory)]
    [InlineData(PersistenceProvider.PostgreSQL)]
    public void Validate_ValidProvider_DoesNotThrow(PersistenceProvider provider)
    {
        // Arrange
        var settings = new DataPersistenceSettings { Provider = provider };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_InvalidProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new DataPersistenceSettings { Provider = (PersistenceProvider)999 };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid persistence provider*");
    }

}
