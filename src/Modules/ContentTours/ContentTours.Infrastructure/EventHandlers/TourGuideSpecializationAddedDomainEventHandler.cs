using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourGuideSpecializationAddedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourGuideSpecializationAddedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourGuideSpecializationAddedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourGuideSpecializationAddedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourGuideSpecializationAddedIntegrationEvent(
                TourGuideId:      evt.TourGuideId,
                UserId:           evt.UserId,
                SpecializationId: evt.SpecializationId)));

        logger.LogInformation(
            "TourGuideSpecializationAddedDomainEvent: specialization {SpecializationId} added to guide profile {TourGuideId}.",
            evt.SpecializationId, evt.TourGuideId);

        return Task.CompletedTask;
    }
}
