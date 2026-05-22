using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events.BusinessEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class BusinessDeletedDomainEventHandler(
    IContentPlacesOutboxWriter outboxWriter,
    ILogger<BusinessDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessDeletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BusinessDeletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        outboxWriter.Enqueue(new BusinessDeletedIntegrationEvent(
            evt.BusinessId,
            evt.OccurredOn));

        logger.LogInformation(
            "BusinessDeletedDomainEvent: queued outbox for Business {BusinessId}",
            evt.BusinessId);

        return Task.CompletedTask;
    }
}
