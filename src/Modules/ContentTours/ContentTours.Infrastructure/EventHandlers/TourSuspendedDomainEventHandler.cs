using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourSuspendedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourSuspendedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourSuspendedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourSuspendedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourSuspendedIntegrationEvent(
                TourId:          evt.TourId,
                CreatedByUserId: evt.CreatedByUserId,
                Reason:          evt.Reason,
                SuspendedAt:     evt.SuspendedAt)));

        // Reason intentionally omitted from the Information log line (privacy).
        logger.LogInformation(
            "TourSuspendedDomainEvent: tour {TourId} suspended at {SuspendedAt}.",
            evt.TourId, evt.SuspendedAt);

        return Task.CompletedTask;
    }
}
