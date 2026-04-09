using FluentAssertions;
using emc.camus.cache.inmemory.Configurations;

namespace emc.camus.cache.inmemory.test.Configurations;

public class InMemoryCacheSettingsTests
{
    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new InMemoryCacheSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_InvalidSubSection_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new InMemoryCacheSettings
        {
            TokenRevocationCache = new() { SyncIntervalSeconds = 0 }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SyncIntervalSeconds*");
    }

}
