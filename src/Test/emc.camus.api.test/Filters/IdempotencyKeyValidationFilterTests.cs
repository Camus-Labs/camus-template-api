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
    private static readonly List<FilterDescriptor> EmptyFilterDescriptors = [];
    private static readonly List<object> EmptyMetadata = [];
    private static readonly List<IFilterMetadata> EmptyFilters = [];
    private static readonly Dictionary<string, object?> EmptyArguments = [];

    private readonly IdempotencyKeyValidationFilter _filter;

    public IdempotencyKeyValidationFilterTests()
    {
        _filter = new IdempotencyKeyValidationFilter();
    }

    private static ActionExecutingContext CreateActionExecutingContext(
        HttpContext httpContext,
        bool hasAttribute = true,
        string policyName = "default")
    {
        var actionDescriptor = new ActionDescriptor();

        if (hasAttribute)
        {
            actionDescriptor.FilterDescriptors = EmptyFilterDescriptors;
            actionDescriptor.EndpointMetadata = new List<object>
            {
                new RequireIdempotencyKeyAttribute(policyName)
            };
        }
        else
        {
            actionDescriptor.EndpointMetadata = EmptyMetadata;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor, new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            EmptyFilters,
            EmptyArguments,
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

    // --- AC-02: Empty/whitespace Idempotency-Key throws ArgumentException ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OnActionExecuting_EmptyOrWhitespaceIdempotencyKey_ThrowsArgumentException(string headerValue)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Headers.IdempotencyKey] = headerValue;
        var context = CreateActionExecutingContext(httpContext);

        // Act
        var act = () => _filter.OnActionExecuting(context);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*empty string or composed entirely of whitespace*");
    }

    // --- AC-02b: Over-length Idempotency-Key throws ArgumentException ---

    [Fact]
    public void OnActionExecuting_OverLengthIdempotencyKey_ThrowsArgumentExceptionWithLengthMessage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Headers.IdempotencyKey] = new string('a', 257);
        var context = CreateActionExecutingContext(httpContext);

        // Act
        var act = () => _filter.OnActionExecuting(context);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum length*");
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

    public static readonly TheoryData<string> MaxLengthIdempotencyKey = new()
    {
        new string('a', 256)
    };

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
            EmptyFilters,
            new object());

        // Act
        var act = () => _filter.OnActionExecuted(context);

        // Assert
        act.Should().NotThrow();
    }
}
