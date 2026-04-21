using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Generic outbox processor typed to a specific DbContext.
    /// Each module registers its own instance as <see cref="IOutboxProcessor"/>:
    ///   services.AddScoped&lt;IOutboxProcessor, OutboxProcessor&lt;MyDbContext&gt;&gt;();
    ///
    /// The <see cref="CompositeOutboxProcessor"/> hosted service resolves all
    /// registered processors and runs them in a single background loop.
    ///
    /// Locking: messages are claimed (LockedUntil set) before processing to prevent
    /// double-delivery in multi-instance deployments. The lock expires after 5 minutes,
    /// allowing another instance to retry if the current one crashes mid-processing.
    ///
    /// Resilience:
    ///   • Each <see cref="INotificationHandler{T}"/> is invoked individually with
    ///     its OWN try/catch so one poison handler cannot block other consumers
    ///     of the same event. MediatR's default ForeachAwaitPublisher short-
    ///     circuits on the first exception — that behavior was unacceptable
    ///     here because (a) multiple modules subscribe to the same event and
    ///     (b) one failing subscriber would starve every later subscriber of
    ///     the retry.
    ///   • A message is marked Processed only when EVERY handler succeeded.
    ///     Any handler failure marks the message Failed (RetryCount++) so the
    ///     outbox retries the whole event; successful handlers short-circuit
    ///     on retry via their own per-module inbox store.
    ///   • MaxRetryCount defaults to 10 (was 3). Messages at/over that ceiling
    ///     remain in the table as a queryable dead-letter inbox for ops.
    /// </summary>
    public sealed class OutboxProcessor<TContext>(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor<TContext>> logger) : IOutboxProcessor
        where TContext : DbContext
    {
        internal const int MaxRetryCount = 10;
        private const int BatchSize = 20;
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

        public async Task ProcessOutboxMessagesAsync(CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            var now = DateTime.UtcNow;

            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null
                         && m.RetryCount < MaxRetryCount
                         && (m.LockedUntil == null || m.LockedUntil < now))
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (messages.Count == 0) return;

            var lockUntil = now.Add(LockDuration);
            foreach (var msg in messages)
                msg.Lock(lockUntil);

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation(
                "Processing {Count} outbox messages for {Context}",
                messages.Count, typeof(TContext).Name);

            foreach (var message in messages)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var eventType = Type.GetType(message.Type);
                    if (eventType is null)
                    {
                        logger.LogWarning("Unknown event type: {Type}", message.Type);
                        message.MarkAsFailed($"Unknown event type: {message.Type}");
                        continue;
                    }

                    var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType) as IIntegrationEvent;
                    if (integrationEvent is null)
                    {
                        message.MarkAsFailed("Deserialization returned null");
                        continue;
                    }

                    var notificationType = typeof(IntegrationEventNotification<>).MakeGenericType(eventType);
                    var notification = (INotification)Activator.CreateInstance(notificationType, message.Id, integrationEvent)!;

                    // Resolve handlers for the closed generic type directly and
                    // invoke each one in isolation. This replaces mediator.Publish
                    // so a single failing handler cannot short-circuit the rest.
                    var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
                    var handlers = scope.ServiceProvider.GetServices(handlerType)
                        .Where(h => h is not null)
                        .ToList();

                    if (handlers.Count == 0)
                    {
                        logger.LogInformation(
                            "No handlers registered for {EventType} (message {MessageId}). Marking as processed.",
                            eventType.Name, message.Id);
                        message.MarkAsProcessed();
                        continue;
                    }

                    var handlerFailures = new List<string>();
                    foreach (var handler in handlers)
                    {
                        if (ct.IsCancellationRequested) break;

                        try
                        {
                            var task = (Task?)handlerType
                                .GetMethod("Handle")!
                                .Invoke(handler, new object[] { notification, ct });
                            if (task is not null) await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception handlerEx)
                        {
                            // Unwrap TargetInvocationException from reflected invocation.
                            var actual = handlerEx is System.Reflection.TargetInvocationException tie && tie.InnerException is not null
                                ? tie.InnerException
                                : handlerEx;

                            logger.LogError(actual,
                                "Handler {Handler} failed for outbox message {MessageId} ({EventType}). Other handlers will still run.",
                                handler!.GetType().FullName, message.Id, eventType.Name);

                            handlerFailures.Add(
                                $"{handler.GetType().Name}: {actual.GetType().Name} {actual.Message}");
                        }
                    }

                    if (handlerFailures.Count > 0)
                    {
                        var aggregate = string.Join(" | ", handlerFailures);
                        if (aggregate.Length > 4000) aggregate = aggregate[..4000];
                        message.MarkAsFailed(aggregate);
                    }
                    else
                    {
                        message.MarkAsProcessed();
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                    message.MarkAsFailed(ex.Message);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
