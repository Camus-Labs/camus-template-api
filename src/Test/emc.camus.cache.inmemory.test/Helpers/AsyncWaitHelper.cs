namespace emc.camus.cache.inmemory.test.Helpers;

internal static class AsyncWaitHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Yield();
        }
    }
}
