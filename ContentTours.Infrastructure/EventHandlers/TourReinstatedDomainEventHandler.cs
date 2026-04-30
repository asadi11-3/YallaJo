using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourReinstatedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourReinstatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourReinstatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourReinstatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourReinstatedIntegrationEvent(
                TourId:          evt.TourId,
                CreatedByUserId: evt.CreatedByUserId,
                ReinstatedAt:    evt.ReinstatedAt)));

        logger.LogInformation(
            "TourReinstatedDomainEvent: tour {TourId} reinstated at {ReinstatedAt}.",
            evt.TourId, evt.ReinstatedAt);

        return Task.CompletedTask;
    }
}
