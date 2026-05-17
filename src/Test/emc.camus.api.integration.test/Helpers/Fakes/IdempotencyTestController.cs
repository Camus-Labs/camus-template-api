using emc.camus.api.Configurations;
using emc.camus.api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Test-only controller providing endpoints with and without <see cref="RequireIdempotencyKeyAttribute"/>
/// for integration testing the idempotency key validation filter and response caching filter
/// through the full HTTP pipeline.
/// </summary>
[ApiController]
[Route("api/v1/test/idempotency")]
public class IdempotencyTestController : ControllerBase
{
    [HttpPost("undecorated")]
    public IActionResult UndecoratedEndpoint() => Ok(new { status = "ok" });

    [HttpPost("decorated-with-body")]
    [Authorize]
    [RequireIdempotencyKey(IdempotencyPolicies.Default)]
    public IActionResult DecoratedWithBodyEndpoint([FromBody] IdempotencyTestPayload payload)
        => Ok(new { status = "ok", value = payload.Value });

    [HttpPost("decorated-long-term")]
    [Authorize]
    [RequireIdempotencyKey(IdempotencyPolicies.LongTerm)]
    public IActionResult DecoratedLongTermEndpoint([FromBody] IdempotencyTestPayload payload)
        => Ok(new { status = "ok", value = payload.Value });
}

/// <summary>
/// Request payload for idempotency response caching integration tests.
/// </summary>
public class IdempotencyTestPayload
{
    public string Value { get; set; } = string.Empty;
}
