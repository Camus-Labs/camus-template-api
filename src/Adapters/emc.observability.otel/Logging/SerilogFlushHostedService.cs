using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace emc.camus.observability.otel.Logging
{
    public sealed class SerilogFlushHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.CloseAndFlush();
            return Task.CompletedTask;
        }
    }
}