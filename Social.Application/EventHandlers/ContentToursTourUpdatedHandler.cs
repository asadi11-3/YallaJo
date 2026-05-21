using ContentTours.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

internal sealed class ContentToursTourUpdatedHandler(
    ITourSnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<ContentToursTourUpdatedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<TourUpdatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourUpdatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var snapshot = await snapshotRepository.GetByTourIdAsync(evt.TourId, ct);
        if (snapshot is null)
            await snapshotRepository.UpsertAsync(TourSnapshot.Create(evt.TourId, string.Empty, string.Empty, Guid.Empty, null, timeProvider.GetUtcNow().UtcDateTime), ct);
        else
            snapshot.MarkUpdated(timeProvider.GetUtcNow().UtcDateTime);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Tour snapshot touched for tour {TourId}", evt.TourId);
    }
}
