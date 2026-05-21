namespace emc.camus.application.Common;

/// <summary>
/// Contract for services that require initialization during application startup.
/// Separates bootstrap lifecycle from request/response application service contracts.
/// </summary>
public interface IServiceInitializer
{
    /// <summary>
    /// Performs startup initialization (e.g., data loading, connectivity validation).
    /// Should be called once during application startup.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken ct = default);
}
