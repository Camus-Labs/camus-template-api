using Microsoft.AspNetCore.Http;

namespace emc.camus.api.test.Helpers;

internal sealed class NextDelegateStub
{
    public bool WasCalled { get; private set; }

    public Task Invoke(HttpContext context)
    {
        WasCalled = true;
        return Task.CompletedTask;
    }
}
