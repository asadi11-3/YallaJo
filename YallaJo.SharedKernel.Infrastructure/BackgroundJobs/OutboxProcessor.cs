using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Generic outbox processor typed to a specific DbContext.
    /// Each module that publishes integration events registers its own instance:
    ///   services.AddHostedService&lt;OutboxProcessor&lt;AccountsDbContext&gt;&gt;();
    ///
    /// Locking: messages are claimed (LockedUntil set) before processing to prevent
    /// double-delivery in multi-instance deployments. The lock expires after 5 minutes,
    /// allowing another instance to retry if the current one crashes mid-processing.
    /// </summary>
    public sealed class OutboxProcessor<TContext>(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor<TContext>> logger) : BackgroundService, IOutboxProcessor
        where TContext : DbContext
    {
        private const int MaxRetryCount = 3;
        private const int BatchSize = 20;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            logger.LogInformation("Outbox Processor for {Context} started", typeof(TContext).Name);

            while (!ct.IsCancellationRequested)
            {
                try { await ProcessOutboxMessagesAsync(ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in outbox processing loop for {Context}", typeof(TContext).Name);
                }

                try { await Task.Delay(Interval, ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            }

            logger.LogInformation("Outbox Processor for {Context} stopped", typeof(TContext).Name);
        }

        public async Task ProcessOutboxMessagesAsync(CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var now = DateTime.UtcNow;

            // Select only unprocessed, unlocked (or expired-lock) messages
            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null
                         && m.RetryCount < MaxRetryCount
                         && (m.LockedUntil == null || m.LockedUntil < now))
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (messages.Count == 0) return;

            // Claim messages by setting a lock so concurrent processors skip them
            var lockUntil = now.Add(LockDuration);
            foreach (var msg in messages)
                msg.Lock(lockUntil);

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("Processing {Count} outbox messages for {Context}", messages.Count, typeof(TContext).Name);

            foreach (var message in messages)
            {
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
                    // Pass the OutboxMessage.Id as MessageId so handlers can record it in their inbox
                    var notification = Activator.CreateInstance(notificationType, message.Id, integrationEvent)!;
                    await mediator.Publish((INotification)notification, ct);

                    message.MarkAsProcessed();
                    logger.LogDebug("Processed outbox message {Id}", message.Id);
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
