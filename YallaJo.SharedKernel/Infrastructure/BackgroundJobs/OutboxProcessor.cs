using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs
{
    public sealed class OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger) : BackgroundService, IOutboxProcessor
    {
        private const int MaxRetryCount = 3;
        private const int BatchSize = 20;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            logger.LogInformation("Outbox Processor started");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Graceful shutdown — do not log as error
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in outbox processing loop");
                }

                try
                {
                    await Task.Delay(Interval, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
            }

            logger.LogInformation("Outbox Processor stopped");
        }

        public async Task ProcessOutboxMessagesAsync(CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null)
                .Where(m => m.RetryCount < MaxRetryCount)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (messages.Count == 0)
                return;

            logger.LogInformation("Processing {Count} outbox messages", messages.Count);

            foreach (var message in messages)
            {
                try
                {
                    var eventType = Type.GetType(message.Type);

                    if (eventType is null)
                    {
                        logger.LogWarning("Unknown event type: {Type} for message {Id}", message.Type, message.Id);
                        message.MarkAsFailed($"Unknown event type: {message.Type}");
                        continue;
                    }

                    var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType) as IIntegrationEvent;

                    if (integrationEvent is null)
                    {
                        logger.LogWarning("Failed to deserialize event {Id} of type {Type}", message.Id, message.Type);
                        message.MarkAsFailed("Deserialization returned null");
                        continue;
                    }

                    await mediator.Publish(integrationEvent, ct);

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
