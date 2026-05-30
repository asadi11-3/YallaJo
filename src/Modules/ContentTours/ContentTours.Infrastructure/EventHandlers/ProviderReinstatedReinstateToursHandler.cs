using Accounts.Contracts.IntegrationEvents;
using ContentTours.Application.Interfaces;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Infrastructure.EventHandlers;

/// <summary>
/// Reinstates all suspended tours belonging to a provider when that provider is reinstated.
///
/// <para>
/// Only reinstates tours that were suspended (SuspendedAt != null); tours in other
/// non-active states are left unchanged.
/// </para>
/// </summary>
public sealed class ProviderReinstatedReinstateToursHandler(
    ContentToursDbContext dbContext,
    IContentToursUnitOfWork unitOfWork,
    IContentToursInboxStore inboxStore,
    ILogger<ProviderReinstatedReinstateToursHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderReinstatedIntegrationEvent>>
{
    private const int BatchSize = 200;

    public async Task Handle(
        IntegrationEventNotification<ProviderReinstatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogDebug(
                "ContentTours: Message {MessageId} (ProviderReinstated {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var totalReinstated = 0;

        // Batch-reinstate: query suspended tours for this provider and reinstate in chunks.
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await dbContext.Tours
                .Where(t => t.CreatedByUserId == evt.UserId && t.SuspendedAt != null)
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
                tour.Reinstate();
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            totalReinstated += batch.Count;

            logger.LogDebug(
                "ContentTours: reinstated batch of {Count} tours for provider UserId={UserId}.",
                batch.Count, evt.UserId);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ContentTours: Reinstated {Total} tours for ProviderReinstated UserId={UserId} ApplicationId={ApplicationId}.",
            totalReinstated, evt.UserId, evt.ApplicationId);
    }
}
