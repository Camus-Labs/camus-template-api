using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using emc.camus.observability.otel.Configurations;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry tracing provider.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Sets resource attributes for the tracing provider.
        /// </summary>
        /// <param name="builder">The tracing provider builder.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured tracing provider builder.</returns>
        public static TracerProviderBuilder UseResourceAttributes(
            this TracerProviderBuilder builder, 
            string serviceName, 
            string serviceVersion, 
            string instanceId,
            string environmentName)
        {
            builder.SetResourceBuilder(ResourceFactory.Create(serviceName, serviceVersion, instanceId, environmentName));
            return builder;
        }
        
        /// <summary>
        /// Adds ASP.NET Core instrumentation to the tracing provider, including enrichment for authentication and routing.
        /// </summary>
        /// <param name="builder">The tracing provider builder.</param>
        /// <returns>The configured tracing provider builder.</returns>
        public static TracerProviderBuilder AddAspNetCoreInstrumentationWithEnrichment(
            this TracerProviderBuilder builder)
        {
            return builder.AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = EnrichWithHttpRequest;
                options.EnrichWithHttpResponse = EnrichWithHttpResponse;
            });
        }

        /// <summary>
        /// Enriches activity with HTTP request information including authentication details.
        /// </summary>
        private static void EnrichWithHttpRequest(Activity activity, HttpRequest request)
        {
            var isAuthenticated = request.HttpContext.User?.Identity?.IsAuthenticated ?? false;
            activity.SetTag("enduser.authenticated", isAuthenticated);

            var endUser = request.HttpContext.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(endUser))
            {
                activity.SetTag("enduser.id", endUser);
            }
        }

        /// <summary>
        /// Enriches activity with HTTP response information including routing details.
        /// </summary>
        private static void EnrichWithHttpResponse(Activity activity, HttpResponse response)
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
        }
        
        /// <summary>
        /// Configures the tracing exporter for OpenTelemetry using Camus conventions.
        /// Supported exporters: OTLP (default), Console.
        /// </summary>
        /// <param name="builder">The tracing provider builder.</param>
        /// <param name="settings">OpenTelemetry configuration settings.</param>
        /// <returns>The configured tracing provider builder.</returns>
        public static TracerProviderBuilder ConfigureTracingExporter(
            this TracerProviderBuilder builder, 
            OpenTelemetrySettings settings)
        {
            var selectedExporter = settings.Tracing.Exporter;

            if (IsOtlpExporter(selectedExporter))
            {
                ConfigureOtlpExporter(builder, settings);
            }
            else if (IsConsoleExporter(selectedExporter))
            {
                builder.AddConsoleExporter();
            }

            return builder;
        }

        private static bool IsOtlpExporter(string exporter) 
            => string.Equals(exporter, "otlp", StringComparison.OrdinalIgnoreCase);

        private static bool IsConsoleExporter(string exporter) 
            => string.Equals(exporter, "console", StringComparison.OrdinalIgnoreCase);

        private static void ConfigureOtlpExporter(TracerProviderBuilder builder, OpenTelemetrySettings settings)
        {
            builder.AddOtlpExporter(options =>
            {
                var endpoint = settings.Tracing.OtlpEndpoint;
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    options.Endpoint = new Uri(endpoint);
                }
                // Use OTLP over gRPC (port 4317)
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }
    }
}