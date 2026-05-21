namespace emc.camus.cache.inmemory.test.Helpers;

internal static class AsyncWaitHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static async Task WaitUntilAsync(Func<bool> condition, TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        var deadline = provider.GetUtcNow() + DefaultTimeout;
        while (!condition() && provider.GetUtcNow() < deadline)
        {
            await Task.Yield();
        }
    }
}
