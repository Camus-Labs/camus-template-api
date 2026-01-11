using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace emc.camus.observability.otel.Telemetry
{
    public static class CamusResourceFactory
    {
        public static ResourceBuilder Create(
            string serviceName, 
            string serviceVersion, 
            string instanceId, 
            string environmentName)
        {
            return ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environmentName),
                    new KeyValuePair<string, object>("service.instance.id", instanceId)
                });
        }
    }
}
