using FluentAssertions;
using emc.camus.cache.inmemory.Configurations;

namespace emc.camus.cache.inmemory.test.Configurations;

public class TokenRevocationCacheSettingsTests
{
    [Fact]
    public void Validate_DefaultSettings_DoesNotThrowAndHasExpectedDefaults()
    {
        // Arrange
        var settings = new TokenRevocationCacheSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
        settings.SyncEnabled.Should().BeTrue();
        settings.SyncIntervalSeconds.Should().Be(300);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    [InlineData(86401)]
    public void Validate_InvalidSyncIntervalSeconds_ThrowsInvalidOperationException(int syncIntervalSeconds)
    {
        // Arrange
        var settings = new TokenRevocationCacheSettings { SyncIntervalSeconds = syncIntervalSeconds };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SyncIntervalSeconds*");
    }

}
