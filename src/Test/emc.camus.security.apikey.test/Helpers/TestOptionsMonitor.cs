using Microsoft.Extensions.Options;

namespace emc.camus.security.apikey.test.Helpers;

internal sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
    public TestOptionsMonitor(TOptions currentValue)
    {
        CurrentValue = currentValue;
    }

    public TOptions CurrentValue { get; }

    public TOptions Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
}
