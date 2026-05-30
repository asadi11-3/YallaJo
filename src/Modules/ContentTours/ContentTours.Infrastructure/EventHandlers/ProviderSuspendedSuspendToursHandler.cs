using Accounts.Contracts.IntegrationEvents;
using ContentTours.Application.Interfaces;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Infrastructure.EventHandlers;

/// <summary>
/// Suspends all active tours belonging to a provider when that provider is suspended.
///
/// <para>
/// Uses batch processing (200 rows at a time) to avoid materializing the entire set
/// of tours in memory when a provider has a large catalogue.
/// </para>
/// </summary>
public sealed class ProviderSuspendedSuspendToursHandler(
    ContentToursDbContext dbContext,
    IContentToursUnitOfWork unitOfWork,
    IContentToursInboxStore inboxStore,
    ILogger<ProviderSuspendedSuspendToursHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderSuspendedIntegrationEvent>>
{
    private const int BatchSize = 200;

    public async Task Handle(
        IntegrationEventNotification<ProviderSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogDebug(
                "ContentTours: Message {MessageId} (ProviderSuspended {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var suspensionReason = $"Provider account suspended: {evt.Reason}";
        var totalSuspended = 0;

        // Batch-suspend: query unsuspended tours for this provider and suspend in chunks.
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await dbContext.Tours
                .Where(t => t.CreatedByUserId == evt.UserId && t.SuspendedAt == null)
                .OrderBy(t => t.Id)
                .Take(BatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var tour in batch)
            {
                tour.Suspend(suspensionReason);
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            totalSuspended += batch.Count;

            logger.LogDebug(
                "ContentTours: suspended batch of {Count} tours for provider UserId={UserId}.",
                batch.Count, evt.UserId);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ContentTours: Suspended {Total} tours for ProviderSuspended UserId={UserId} ApplicationId={ApplicationId}.",
            totalSuspended, evt.UserId, evt.ApplicationId);
    }
}
