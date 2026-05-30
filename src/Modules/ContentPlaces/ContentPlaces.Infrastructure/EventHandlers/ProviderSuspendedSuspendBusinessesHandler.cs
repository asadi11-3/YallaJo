using Accounts.Contracts.IntegrationEvents;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Suspends all approved businesses belonging to a provider when that provider is suspended.
/// Uses batch processing (200 rows at a time) to avoid materializing the entire set.
/// </summary>
public sealed class ProviderSuspendedSuspendBusinessesHandler(
    ContentPlacesDbContext dbContext,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesInboxStore inboxStore,
    ILogger<ProviderSuspendedSuspendBusinessesHandler> logger)
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
                "ContentPlaces: Message {MessageId} (ProviderSuspended {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var totalSuspended = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await dbContext.Set<Domain.Entities.Business>()
                .Where(b => b.OwnerId == evt.UserId && b.Status == BusinessStatus.Approved)
                .OrderBy(b => b.Id)
                .Take(BatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            foreach (var business in batch)
            {
                business.Suspend(evt.Reason, Guid.Empty);
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            totalSuspended += batch.Count;

            logger.LogDebug(
                "ContentPlaces: suspended batch of {Count} businesses for provider UserId={UserId}.",
                batch.Count, evt.UserId);

            if (batch.Count < BatchSize)
                break;
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ContentPlaces: Suspended {Total} businesses for ProviderSuspended UserId={UserId} ApplicationId={ApplicationId}.",
            totalSuspended, evt.UserId, evt.ApplicationId);
    }
}
