using ContentPlaces.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

internal sealed class ContentPlacesPlaceCreatedHandler(
    IPlaceSnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<ContentPlacesPlaceCreatedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<PlaceCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PlaceCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var snapshot = await snapshotRepository.GetByPlaceIdAsync(evt.PlaceId, ct);
        if (snapshot is null)
            await snapshotRepository.UpsertAsync(PlaceSnapshot.Create(evt.PlaceId, evt.Name, evt.Slug, nowUtc), ct);
        else
            snapshot.UpsertCreated(evt.Name, evt.Slug, nowUtc);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Place snapshot upserted for place {PlaceId}", evt.PlaceId);
    }
}
