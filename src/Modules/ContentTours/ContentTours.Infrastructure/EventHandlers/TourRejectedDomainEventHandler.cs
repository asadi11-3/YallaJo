using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourRejectedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourRejectedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourRejectedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourRejectedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourRejectedIntegrationEvent(
                TourId:           evt.TourId,
                CreatedByUserId:  evt.CreatedByUserId,
                RejectedByUserId: evt.RejectedByUserId,
                Reason:           evt.Reason,
                RejectedAt:       evt.RejectedAt)));

        // Reason intentionally omitted from the Information log line (privacy).
        logger.LogInformation(
            "TourRejectedDomainEvent: tour {TourId} rejected by {RejectedByUserId} at {RejectedAt}.",
            evt.TourId, evt.RejectedByUserId, evt.RejectedAt);

        return Task.CompletedTask;
    }
}
