using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

/// <summary>
/// Single hosted service that processes outbox messages for ALL modules in sequence.
/// Replaces per-module OutboxProcessor&lt;TContext&gt; hosted-service registrations with
/// one background loop that resolves every <see cref="IOutboxProcessor"/> from DI.
/// </summary>
public sealed class CompositeOutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<CompositeOutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Composite Outbox Processor started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var processors = scope.ServiceProvider.GetServices<IOutboxProcessor>();

                foreach (var processor in processors)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        await processor.ProcessOutboxMessagesAsync(ct);
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

            try { await Task.Delay(Interval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("Composite Outbox Processor stopped");
    }
}
