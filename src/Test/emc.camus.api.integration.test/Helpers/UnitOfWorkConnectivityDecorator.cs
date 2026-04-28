using emc.camus.application.Common;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Decorator around a real <see cref="IUnitOfWork"/> that intercepts
/// <see cref="CheckConnectivityAsync"/> to simulate database outages.
/// All other operations delegate to the inner implementation unchanged.
/// </summary>
public sealed class UnitOfWorkConnectivityDecorator : IUnitOfWork
{
    private readonly IUnitOfWork _inner;

    /// <summary>
    /// When <c>true</c>, <see cref="CheckConnectivityAsync"/> throws to simulate an unreachable database.
    /// Static because the decorator is scoped (new instance per request) while the flag must survive across requests.
    /// </summary>
    public static bool SimulateConnectivityFailure { get; set; }

    public UnitOfWorkConnectivityDecorator(IUnitOfWork inner)
    {
        _inner = inner;
    }

    public Task BeginTransactionAsync(CancellationToken ct = default)
        => _inner.BeginTransactionAsync(ct);

    public Task CommitAsync(CancellationToken ct = default)
        => _inner.CommitAsync(ct);

    public Task RollbackAsync()
        => _inner.RollbackAsync();

    public Task CheckConnectivityAsync(CancellationToken ct = default)
    {
        return SimulateConnectivityFailure
            ? Task.FromException(new InvalidOperationException("Simulated database connectivity failure"))
            : _inner.CheckConnectivityAsync(ct);
    }
}
