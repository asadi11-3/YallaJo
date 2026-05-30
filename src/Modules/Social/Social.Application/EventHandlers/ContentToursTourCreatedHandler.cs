using ContentTours.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

internal sealed class ContentToursTourCreatedHandler(
    ITourSnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<ContentToursTourCreatedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<TourCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var snapshot = await snapshotRepository.GetByTourIdAsync(evt.TourId, ct);
        if (snapshot is null)
            await snapshotRepository.UpsertAsync(TourSnapshot.Create(evt.TourId, evt.Name, evt.Slug, evt.CreatedByUserId, evt.PlaceId, nowUtc), ct);
        else
            snapshot.UpsertCreated(evt.Name, evt.Slug, evt.CreatedByUserId, evt.PlaceId, nowUtc);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Tour snapshot upserted for tour {TourId}", evt.TourId);
    }
}
