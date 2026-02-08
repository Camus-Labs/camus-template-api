using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace emc.camus.observability.otel.Logging
{
    /// <summary>
    /// Hosted service to ensure Serilog logs are flushed on application shutdown, preventing log loss.
    /// </summary>
    public sealed class SerilogFlushHostedService : IHostedService
    {
        /// <summary>
        /// No-op on start; required by IHostedService.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Flushes and closes Serilog on application stop to ensure all logs are written.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.CloseAndFlush();
            return Task.CompletedTask;
        }
    }
}