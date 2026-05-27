using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace emc.camus.api.test.Helpers;

public static class ResourceExecutionDelegateFactory
{
    private static readonly List<IFilterMetadata> EmptyFilters = [];

    public static ResourceExecutionDelegate CreateNextDelegate(IActionResult? actionResult = null)
    {
        var result = actionResult ?? new OkObjectResult(new { message = "created" });
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, EmptyFilters)
            {
                Result = result
            };
            return Task.FromResult(context);
        };
    }

    public static ResourceExecutionDelegate CreateNextDelegateWithNonObjectResult()
    {
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, EmptyFilters)
            {
                Result = new EmptyResult()
            };
            return Task.FromResult(context);
        };
    }

    public static (ResourceExecutionDelegate Delegate, Func<bool> WasCalled) CreateTrackingNextDelegate(IActionResult? actionResult = null)
    {
        var wasCalled = false;
        ResourceExecutionDelegate del = () =>
        {
            wasCalled = true;
            var result = actionResult ?? new OkObjectResult(new { message = "created" });
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, EmptyFilters)
            {
                Result = result
            };
            return Task.FromResult(context);
        };
        return (del, () => wasCalled);
    }

    public static ResourceExecutionDelegate CreateNextDelegateWithNullResult()
    {
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, EmptyFilters)
            {
                Result = null!
            };
            return Task.FromResult(context);
        };
    }

    public static ResourceExecutionDelegate CreateNextDelegateThatThrows()
    {
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, EmptyFilters)
            {
                Exception = new InvalidOperationException("action failed")
            };
            return Task.FromResult(context);
        };
    }
}
