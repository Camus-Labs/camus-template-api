using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace emc.camus.observability.otel.test.Helpers;

/// <summary>
/// Fake <see cref="IHttpResponseFeature"/> that captures OnStarting callbacks for test assertions.
/// </summary>
internal sealed class TestResponseFeature : IHttpResponseFeature
{
    private Func<object, Task>? _callback;
    private object? _state;

    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = Stream.Null;
    public bool HasStarted { get; private set; }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        _callback = callback;
        _state = state;
    }

    public void OnCompleted(Func<object, Task> callback, object state) { }

    public async Task FireOnStartingAsync()
    {
        if (_callback != null)
        {
            HasStarted = true;
            await _callback(_state!);
        }
    }
}
