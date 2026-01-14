using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry tracing provider using Camus conventions.
    /// </summary>
    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Sets resource attributes for the tracing provider using Camus conventions.
        /// </summary>
        /// <param name="builder">The tracing provider builder.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured tracing provider builder.</returns>
        public static TracerProviderBuilder UseCamusResource(
            this TracerProviderBuilder builder, 
            string serviceName, 
            string serviceVersion, 
            string instanceId,
            string environmentName)
        {
            builder.SetResourceBuilder(CamusResourceFactory.Create(serviceName, serviceVersion, instanceId, environmentName));
            return builder;
        }
        
        /// <summary>
        /// Adds ASP.NET Core instrumentation to the tracing provider, including enrichment for authentication and routing.
        /// </summary>
        /// <param name="builder">The tracing provider builder.</param>
        /// <returns>The configured tracing provider builder.</returns>
        public static TracerProviderBuilder AddCamusAspNetCoreInstrumentation(
            this TracerProviderBuilder builder)
        {
            return builder.AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    var isAuthenticated = request.HttpContext.User?.Identity?.IsAuthenticated ?? false;
                    activity.SetTag("enduser.authenticated", isAuthenticated);

                    var endUser = request.HttpContext.User?.Identity?.Name;
                    if (!string.IsNullOrWhiteSpace(endUser))
                    {
                        activity.SetTag("enduser.id", endUser);
                    }
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    var routeData = response.HttpContext.GetRouteData();
                    var controller = routeData?.Values["controller"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(controller))
                    {
                        activity.SetTag("http.route.controller", controller);
                    }

                    var version = routeData?.Values["version"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        activity.SetTag("http.route.version", version);
                    }
                };
            });
        }
        
        public static TracerProviderBuilder ConfigureTracingExporter(
            this TracerProviderBuilder builder, 
            IConfiguration configuration)
        {
            var openTelemetryConfig = configuration.GetSection("OpenTelemetry");
            var selectedExporter = openTelemetryConfig["Tracing:Exporter"] ?? "none";

            switch (selectedExporter.ToLowerInvariant())
            {
                case "otlp":
                    builder.AddOtlpExporter(options =>
                    {
                        var endpoint = openTelemetryConfig["Tracing:OtlpEndpoint"];
                        if (!string.IsNullOrWhiteSpace(endpoint))
                        {
                            options.Endpoint = new Uri(endpoint);
                        }
                        // Use OTLP over gRPC (port 4317)
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                    break;

                case "console":
                    builder.AddConsoleExporter();
                    break;

                default:
                    // No exporter configured
                    break;
            }

            return builder;
        }
    }
}