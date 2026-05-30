using ContentPlaces.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

internal sealed class ContentPlacesBusinessCreatedHandler(
    IBusinessSnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<ContentPlacesBusinessCreatedHandler> logger,
    TimeProvider timeProvider)
    : INotificationHandler<IntegrationEventNotification<BusinessCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<BusinessCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        var evt = notification.Event;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var snapshot = await snapshotRepository.GetByBusinessIdAsync(evt.BusinessId, ct);
        if (snapshot is null)
            await snapshotRepository.UpsertAsync(BusinessSnapshot.Create(evt.BusinessId, evt.Name, evt.Slug, evt.OwnerId, evt.PlaceId, nowUtc), ct);
        else
            snapshot.UpsertCreated(evt.Name, evt.Slug, evt.OwnerId, evt.PlaceId, nowUtc);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Business snapshot upserted for business {BusinessId}", evt.BusinessId);
    }
}
