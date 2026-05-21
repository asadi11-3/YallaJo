using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourGuideUpdatedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourGuideUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourGuideUpdatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourGuideUpdatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourGuideUpdatedIntegrationEvent(
                TourGuideId: evt.TourGuideId,
                UserId:      evt.UserId)));

        logger.LogInformation(
            "TourGuideUpdatedDomainEvent: guide profile {TourGuideId} updated for user {UserId}.",
            evt.TourGuideId, evt.UserId);

        return Task.CompletedTask;
    }
}
