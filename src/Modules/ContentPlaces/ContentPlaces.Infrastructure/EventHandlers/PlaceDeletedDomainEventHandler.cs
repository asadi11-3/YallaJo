using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class PlaceDeletedDomainEventHandler(
    ContentPlacesDbContext dbContext,
    ILogger<PlaceDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PlaceDeletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<PlaceDeletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(new PlaceDeletedIntegrationEvent(evt.PlaceId)));

        logger.LogInformation("PlaceDeletedDomainEvent: Queued outbox event for place {PlaceId}.", evt.PlaceId);

        return Task.CompletedTask;
    }
}
