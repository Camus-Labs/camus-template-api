using emc.camus.api.Configurations;
using emc.camus.api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Test-only controller providing endpoints with and without <see cref="RequireIdempotencyKeyAttribute"/>
/// for integration testing the idempotency key validation filter through the full HTTP pipeline.
/// </summary>
[ApiController]
[Route("api/v1/test/idempotency")]
public class IdempotencyTestController : ControllerBase
{
    [HttpPost("decorated")]
    [RequireIdempotencyKey(IdempotencyPolicies.Default)]
    public IActionResult DecoratedEndpoint() => Ok(new { status = "ok" });

    [HttpPost("undecorated")]
    public IActionResult UndecoratedEndpoint() => Ok(new { status = "ok" });
}
