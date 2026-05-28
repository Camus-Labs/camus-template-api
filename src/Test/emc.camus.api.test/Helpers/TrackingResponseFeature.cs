using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace emc.camus.api.test.Helpers;

public sealed class TrackingResponseFeature : IHttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _startingCallbacks;

    public int StatusCode { get; set; }
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; }
    public Stream Body { get; set; }
    public bool HasStarted { get; private set; }

    public TrackingResponseFeature()
    {
        _startingCallbacks = new List<(Func<object, Task> Callback, object State)>();
        StatusCode = 200;
        Headers = new HeaderDictionary();
        Body = Stream.Null;
    }

    public void OnCompleted(Func<object, Task> callback, object state) { }

    public void OnStarting(Func<object, Task> callback, object state)
        => _startingCallbacks.Add((callback, state));

    public async Task FireOnStartingAsync()
    {
        HasStarted = true;
        for (var i = _startingCallbacks.Count - 1; i >= 0; i--)
            await _startingCallbacks[i].Callback(_startingCallbacks[i].State);
    }
}
