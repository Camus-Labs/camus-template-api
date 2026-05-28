using emc.camus.application.ApiInfo;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Stub <see cref="IApiInfoService"/> that delays indefinitely until cancellation.
/// Used by timeout integration tests to force the request timeout policy to fire.
/// Exposes <see cref="CancellationReceived"/> so tests can verify that cancellation propagated to the service layer.
/// </summary>
public sealed class SlowApiInfoService : IApiInfoService
{
    private readonly TaskCompletionSource _cancelled;

    public SlowApiInfoService()
    {
        _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Completes when the cancellation token fires inside <see cref="GetByVersionAsync"/>.
    /// Tests can await this to confirm server-side cancellation propagated through the pipeline.
    /// </summary>
    public Task CancellationReceived => _cancelled.Task;

    /// <summary>
    /// Delays indefinitely until the cancellation token is triggered by the request timeout policy.
    /// </summary>
    public async Task<ApiInfoDetailView> GetByVersionAsync(ApiInfoFilter filter, CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _cancelled.TrySetResult();
            throw;
        }

        // Unreachable — Task.Delay throws OperationCanceledException when ct fires
        return new ApiInfoDetailView("0.0", "timeout", Array.Empty<string>());
    }
}
