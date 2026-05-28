using Microsoft.AspNetCore.Mvc;
using emc.camus.api.Controllers;

namespace emc.camus.api.test.Helpers;

public sealed class TestableController : ApiControllerBase
{
    public TestableController(TimeProvider timeProvider) : base(timeProvider) { }

    public IActionResult CallSuccess<T>(T data, string message) => Success(data, message);
    public IActionResult CallCreated<T>(T data, string message, string? actionName = null, object? routeValues = null)
        => Created(data, message, actionName, routeValues);
    public IActionResult CallAccepted<T>(T data, string message) => Accepted(data, message);
    public IActionResult CallNoContentSuccess() => NoContentSuccess();
}
