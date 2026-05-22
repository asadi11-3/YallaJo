using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourGuideRegisteredDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourGuideRegisteredDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourGuideRegisteredDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourGuideRegisteredDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourGuideRegisteredIntegrationEvent(
                TourGuideId: evt.TourGuideId,
                UserId:      evt.UserId)));

        logger.LogInformation(
            "TourGuideRegisteredDomainEvent: guide profile {TourGuideId} registered for user {UserId}.",
            evt.TourGuideId, evt.UserId);

        return Task.CompletedTask;
    }
}
