using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

/// <summary>
/// Single hosted service that processes outbox messages for ALL modules in sequence.
/// Replaces per-module OutboxProcessor&lt;TContext&gt; hosted-service registrations with
/// one background loop that resolves every <see cref="IOutboxProcessor"/> from DI.
///
/// <b>Adaptive polling</b>: when any processor returns > 0 messages processed, the loop
/// immediately re-polls after 200 ms (drain mode). When all processors return 0 (idle),
/// it backs off to the standard 10-second interval. This reduces end-to-end delivery
/// latency under load without increasing DB queries at rest.
/// </summary>
public sealed class CompositeOutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<CompositeOutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan IdleInterval  = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DrainInterval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Composite Outbox Processor started");

        while (!ct.IsCancellationRequested)
        {
            var processedAny = false;

            try
            {
                using var scope = serviceProvider.CreateScope();
                var processors = scope.ServiceProvider.GetServices<IOutboxProcessor>();

                foreach (var processor in processors)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        var count = await processor.ProcessOutboxMessagesAsync(ct);
                        if (count > 0) processedAny = true;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Outbox processing failed for {Processor}",
                            processor.GetType().Name);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in composite outbox processing loop");
            }

            // Adaptive delay: drain quickly while messages are flowing, back off when idle.
            var delay = processedAny ? DrainInterval : IdleInterval;

            try { await Task.Delay(delay, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("Composite Outbox Processor stopped");
    }
}
