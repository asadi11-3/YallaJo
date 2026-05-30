using ContentTours.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

internal sealed class ContentToursTourDeletedHandler(
    ITourSnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<ContentToursTourDeletedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourDeletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var snapshot = await snapshotRepository.GetByTourIdAsync(evt.TourId, ct);
        if (snapshot is null)
            snapshot = TourSnapshot.Create(evt.TourId, string.Empty, string.Empty, evt.CreatedByUserId, evt.PlaceId, timeProvider.GetUtcNow().UtcDateTime);

        snapshot.MarkDeleted(evt.CreatedByUserId, evt.PlaceId, evt.DeletedAt);
        await snapshotRepository.UpsertAsync(snapshot, ct);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Tour snapshot marked deleted for tour {TourId}", evt.TourId);
    }
}
