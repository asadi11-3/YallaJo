using ContentPlaces.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

internal sealed class ContentPlacesPlaceDeletedHandler(
    IPlaceSnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<ContentPlacesPlaceDeletedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<PlaceDeletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PlaceDeletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var snapshot = await snapshotRepository.GetByPlaceIdAsync(evt.PlaceId, ct);
        if (snapshot is null)
            snapshot = PlaceSnapshot.Create(evt.PlaceId, string.Empty, string.Empty, nowUtc);

        snapshot.MarkDeleted(nowUtc);
        await snapshotRepository.UpsertAsync(snapshot, ct);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Place snapshot marked deleted for place {PlaceId}", evt.PlaceId);
    }
}
