using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using ContentTours.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class PlaceTourCountUpdatedIntegrationEventHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesInboxStore inboxStore,
    ILogger<PlaceTourCountUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceTourCountUpdatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PlaceTourCountUpdatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // ── Idempotency check (outbox may retry) ──────────────────────────────
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "PlaceTourCountUpdated: Message {MessageId} already processed; skipping.",
                notification.MessageId);
            return;
        }

        var evt = notification.Event;

        var place = await placeRepository.GetByIdAsync(evt.PlaceId, ct, asNoTracking: false);

        if (place is null)
        {
            // Place may have been deleted — not an error, just mark processed and move on.
            logger.LogWarning(
                "PlaceTourCountUpdated: Place {PlaceId} not found; skipping count update.",
                evt.PlaceId);

            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        place.UpdateTourCount(evt.ActiveTourCount);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "PlaceTourCountUpdated: Place {PlaceId} TourCount set to {Count}",
            evt.PlaceId, evt.ActiveTourCount);
    }
}
