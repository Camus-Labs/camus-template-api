using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using emc.camus.api.Filters;
using emc.camus.application.Common;

namespace emc.camus.api.test.Filters;

public class IdempotencyKeyValidationFilterTests
{
    private readonly IdempotencyKeyValidationFilter _filter = new();

    private static ActionExecutingContext CreateActionExecutingContext(
        HttpContext httpContext,
        bool hasAttribute = true,
        string policyName = "default")
    {
        var actionDescriptor = new ActionDescriptor();

        if (hasAttribute)
        {
            actionDescriptor.FilterDescriptors = new List<FilterDescriptor>();
            actionDescriptor.EndpointMetadata = new List<object>
            {
                new RequireIdempotencyKeyAttribute(policyName)
            };
        }
        else
        {
            actionDescriptor.EndpointMetadata = new List<object>();
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor, new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    // --- AC-01: Missing Idempotency-Key header throws ArgumentException ---

    [Fact]
    public void OnActionExecuting_MissingIdempotencyKeyHeader_ThrowsArgumentExceptionWithMissingMessage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var context = CreateActionExecutingContext(httpContext);

        // Act
        var act = () => _filter.OnActionExecuting(context);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Idempotency-Key*missing*");
    }

    // --- AC-02: Empty/whitespace/over-length Idempotency-Key throws ArgumentException ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [MemberData(nameof(OverLengthIdempotencyKey))]
    public void OnActionExecuting_InvalidIdempotencyKey_ThrowsArgumentExceptionWithInvalidMessage(string headerValue)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Headers.IdempotencyKey] = headerValue;
        var context = CreateActionExecutingContext(httpContext);

        // Act
        var act = () => _filter.OnActionExecuting(context);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Idempotency-Key*invalid*");
    }

    public static IEnumerable<object[]> OverLengthIdempotencyKey()
    {
        yield return new object[] { new string('a', 257) };
    }

    // --- AC-03: Valid Idempotency-Key passes validation ---

    [Theory]
    [InlineData("valid-key-123")]
    [InlineData("x")]
    [MemberData(nameof(MaxLengthIdempotencyKey))]
    public void OnActionExecuting_ValidIdempotencyKey_DoesNotThrow(string headerValue)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Headers.IdempotencyKey] = headerValue;
        var context = CreateActionExecutingContext(httpContext);

        // Act
        var act = () => _filter.OnActionExecuting(context);

        // Assert
        act.Should().NotThrow();
        context.Result.Should().BeNull();
    }

    public static IEnumerable<object[]> MaxLengthIdempotencyKey()
    {
        yield return new object[] { new string('a', 256) };
    }

    // --- AC-04: Endpoints without attribute are unaffected ---

    [Fact]
    public void OnActionExecuting_NoRequireIdempotencyKeyAttribute_DoesNotThrow()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var context = CreateActionExecutingContext(httpContext, hasAttribute: false);

        // Act
        var act = () => _filter.OnActionExecuting(context);

        // Assert
        act.Should().NotThrow();
        context.Result.Should().BeNull();
    }

    // --- AC-05: OnActionExecuted completes without error ---

    [Fact]
    public void OnActionExecuted_Always_DoesNotThrow()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object());

        // Act
        var act = () => _filter.OnActionExecuted(context);

        // Assert
        act.Should().NotThrow();
    }
}
